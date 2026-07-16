using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public class VoxelGenerator : MonoBehaviour
{
    public ComputeShader NoiseCS;
    //private RenderTexture noiseRT;
    private ComputeBuffer noiseBuffer;
    private Texture2DArray noiseT2DArray;

    private float[] noiseArray1;
    private float[] noiseArray2;
    private float[] noiseArray3;


    [SerializeField]
    private Vector3Int worldSize = new Vector3Int(512,64,512);
    private WorldGenSettings worldSettings;
    private Voxel[,,] voxels;

    public float Scale = 0.02f;
    public int Octaves = 3;
    public float OctaveStrength = 0.3f;
    public float OctaveScale = 1.5f;


    public void Generate(Vector3Int worldVoxelSize, WorldGenSettings worldGenSettings)
    {
        worldSize = worldVoxelSize;
        worldSettings = worldGenSettings;

        voxels = new Voxel[worldSize.x, worldSize.y, worldSize.z];




        TerrainNoiseGPU(out noiseArray1, 0f);
        //TerrainNoiseGPU(out noiseArray2, 3f);
        //TerrainNoiseGPU(out noiseArray3, 9f);

        LoopXZ(TerrainNoise);
        LoopXYZ(AddGrass);
        LoopXYZ(CarveCaves);

    }


    public Voxel[,,] GetGeneratedVoxels()
    {
        return voxels;
    }

    #region GET/SET
    private bool SetVoxel(int x, int y, int z, Voxel voxel)
    {
        if (x < worldSize.x - 1 && y < worldSize.y - 1 && z < worldSize.z - 1 && x > 0 && y > 0 && z > 0)
        {
            voxels[x, y, z] = voxel;
            return true;
        }
        else
        {
            return false;
        }
    }
    private Voxel GetVoxel(int x, int y, int z)
    {
        if (x < worldSize.x - 1 && y < worldSize.y - 1 && z < worldSize.z - 1 && x > 0 && y > 0 && z > 0)
        {
            return voxels[x, y, z];
        }
        else
        {
            return new Voxel(BlockID.Invalid, 0, 0);
        }
    }
    #endregion

    #region LOOPS
    private void LoopXYZ(Action<int, int, int> loopFunction)
    {
        Vector3Int Size3D = new Vector3Int(worldSize.x, worldSize.y, worldSize.z);
        for (int x = 0; x < Size3D.x; x++) { 
            for (int z = 0; z < Size3D.z; z++) {
                for (int y = 0; y < Size3D.y; y++)
                {
                    loopFunction(x, y, z);
                }
            }
        }
    }
    private void LoopXZ(Action<int, int> loopFunction)
    {
        Vector2Int Size2D = new Vector2Int(worldSize.x, worldSize.z);
        for (int x = 0; x < Size2D.x; x++) {
            for (int z = 0; z < Size2D.y; z++)
            {
                loopFunction(x, z);
            }
        }
    }
    #endregion

    #region PASSES 

    [Button]
    private void TerrainNoiseGPU(out float[] noiseArray, float seed)
    {
        RenderTexture noiseRT = new RenderTexture(worldSize.x, worldSize.y, 32, RenderTextureFormat.RFloat);
        noiseRT.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        noiseRT.volumeDepth = worldSize.z;
        noiseRT.enableRandomWrite = true;

        noiseArray = new float[worldSize.x * worldSize.y * worldSize.z];
        noiseBuffer = new ComputeBuffer(worldSize.x * worldSize.y * worldSize.z, sizeof(float));

        int[] size = new int[3] { worldSize.x, worldSize.y, worldSize.z };
        int[] threads = new int[3] { 16, 1, 16 };
        int[] threadSize = new int[3] { size[0] / threads[0], size[1] / threads[1], size[2] / threads[2] };

        int kernel = NoiseCS.FindKernel("GenerateNoise");
        //NoiseCS.SetTexture(kernel, "Result", noiseRT);
        NoiseCS.SetBuffer(kernel, "ResultBuffer", noiseBuffer);
        NoiseCS.SetInts("WorldSize", size);
        NoiseCS.SetInts("ThreadCount", threads);
        NoiseCS.SetInts("ThreadSize", threadSize);
        NoiseCS.SetFloat("Seed", seed);
        NoiseCS.SetFloat ("Scale", Scale);
        NoiseCS.SetInt("Octaves", Octaves);
        NoiseCS.SetFloat("OctaveScale", OctaveScale);
        NoiseCS.SetFloat("OctaveStrength", OctaveStrength);

        //NoiseCS.SetVector(kernel, new Vector4(worldSize.x, worldSize.y, worldSize.z, 0f));
        NoiseCS.Dispatch(kernel, threads[0], threads[1], threads[2]);

        //AssetDatabase.CreateAsset(noiseRT, "Assets/Textures/Generated/Test_Tex3D.asset");
        noiseBuffer.GetData(noiseArray);
        //noiseT2DArray.CopyPixels(noiseRT);

        noiseRT.Release();
    }

    private float ReadNoiseTex(int x, int y, int z, ref float[] noise)
    {
        int xyzCoord = x + worldSize.x * y + worldSize.x * worldSize.y * z;
        return noise[xyzCoord];
    }

    private void TerrainNoise (int x, int z)
    {
        //float noise = Perlin.Fbm(x * worldSettings.NoiseScale, z * worldSettings.NoiseScale, worldSettings.NoiseOctaves);
        //float noise2 = Perlin.Fbm(x * worldSettings.NoiseScale * 0.2f, z * worldSettings.NoiseScale * 0.2f, worldSettings.NoiseOctaves);
        //float h = noise * worldSettings.HeightRange + worldSettings.HeightOffset;
        //h = h + (noise2 * worldSettings.HeightRange * 4);

        float h = ReadNoiseTex(x, 1, z, ref noiseArray1);
        float color_r = Mathf.Abs(h);
        float color_g = Mathf.Abs(ReadNoiseTex(x, 20, z, ref noiseArray1));
        float color_b = Mathf.Abs(ReadNoiseTex(x, 40, z, ref noiseArray1));
        //Debug.Log($"h={h}");
        h = h * worldSettings.HeightRange + worldSettings.HeightOffset;

        for (int y = 0; y < worldSize.y; y++)
        {
            UnityEngine.Random.InitState(x + y + z);
            int r = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 5.99f));
            OrthoNormal o1 = VoxelHelper.OrthoDirs[r];
            OrthoNormal o2 = VoxelHelper.OrthoDirs[(r + 2) % 5];

            float diff = h - y;
            if (diff < 0f)
                SetVoxel(x, y, z, new Voxel(BlockID.Air, 0, 0, BlockShape.Empty));
            else// if (diff < 4f)
                SetVoxel(x, y, z, new Voxel(new Color(color_r, color_g, color_b).NormalizeRGB()));
                //SetVoxel(x, y, z, new Voxel(BlockID.Dirt, 0, 0, BlockShape.Solid));
            //else if (diff < 8f)
            //{
            //    SetVoxel(x, y, z, new Voxel(BlockID.Rocky_Dirt, o1, o2));
            //}
            //else if (diff < 15f)
            //    SetVoxel(x, y, z, new Voxel(BlockID.Stone_Sandstone, o1, o2));
            //else if (diff < 22f)
            //    SetVoxel(x, y, z, new Voxel(BlockID.Stone_Limestone, 0, 0, BlockShape.Solid));
            //else if (diff < 30f)
            //    SetVoxel(x, y, z, new Voxel(BlockID.Stone_Dolomite, o1, o2));
            //else if (diff < 40f)
            //    SetVoxel(x, y, z, new Voxel(BlockID.Stone_Shale, o1, o2));
            //else if (diff < 55f)
            //    SetVoxel(x, y, z, new Voxel(BlockID.Stone_Slate, 0, 0, BlockShape.Solid));
            //else if (diff < 70f)
            //    SetVoxel(x, y, z, new Voxel(BlockID.Stone_Basalt, o1, o2));
        }
    }

    private void AddGrass(int x, int y, int z)
    {
        if (GetVoxel(x, y, z).BlockID == BlockID.Dirt && GetVoxel(x, y + 1, z).BlockID == BlockID.Air)
        {
            SetVoxel(x, y, z, new Voxel(BlockID.Grass));
            //SetVoxel(x, y-1, z, new Voxel(BlockID.Dirt));
            //SetVoxel(x, y-2, z, new Voxel(BlockID.Dirt));

        }
    }

    private void CarveCaves(int x, int y, int z) 
    {
        if (y < 2 || GetVoxel(x,y,z).BlockID == BlockID.Air) return;
        float noise = Perlin.Fbm(x * worldSettings.NoiseScale * 1.4f, y * worldSettings.NoiseScale *1.4f, z * worldSettings.NoiseScale * 1.4f, 2);
        if (noise < -0.1f)
        {
            SetVoxel(x, y, z, new Voxel(BlockID.Air));
        }

    }

    #endregion

}
