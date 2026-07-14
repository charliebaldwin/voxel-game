using Sirenix.OdinInspector;
using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public class VoxelGenerator : MonoBehaviour
{
    public ComputeShader NoiseCS;
    private RenderTexture noiseRT;
    private float[] noiseArray;
    private ComputeBuffer noiseBuffer;
    private Texture2DArray noiseT2DArray;

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




        TerrainNoiseGPU();
        LoopXZ(TerrainNoise);
        LoopXYZ(AddGrass);

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
    private void TerrainNoiseGPU()
    {
        noiseRT = new RenderTexture(worldSize.x, worldSize.y, 32, RenderTextureFormat.RFloat);
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
        NoiseCS.SetFloat ("Scale", Scale);
        NoiseCS.SetInt("Octaves", Octaves);
        NoiseCS.SetFloat("OctaveScale", OctaveScale);
        NoiseCS.SetFloat("OctaveStrength", OctaveStrength);

        //NoiseCS.SetVector(kernel, new Vector4(worldSize.x, worldSize.y, worldSize.z, 0f));
        NoiseCS.Dispatch(kernel, threads[0], threads[1], threads[2]);

        //AssetDatabase.CreateAsset(noiseRT, "Assets/Textures/Generated/Test_Tex3D.asset");
        noiseBuffer.GetData(noiseArray);
        //noiseT2DArray.CopyPixels(noiseRT);

    }

    private float ReadNoiseTex(int x, int y, int z)
    {
        int xyzCoord = x + worldSize.x * y + worldSize.x * worldSize.y * z;
        return noiseArray[xyzCoord];
    }

    private void TerrainNoise (int x, int z)
    {
        //float noise = Perlin.Fbm(x * worldSettings.NoiseScale, z * worldSettings.NoiseScale, worldSettings.NoiseOctaves);
        //float noise2 = Perlin.Fbm(x * worldSettings.NoiseScale * 0.2f, z * worldSettings.NoiseScale * 0.2f, worldSettings.NoiseOctaves);
        //float h = noise * worldSettings.HeightRange + worldSettings.HeightOffset;
        //h = h + (noise2 * worldSettings.HeightRange * 4);

        float h = ReadNoiseTex(x, 1, z);
        //Debug.Log($"h={h}");
        h = h * worldSettings.HeightRange + worldSettings.HeightOffset;

        for (int y = 0; y < worldSize.y; y++)
        {
            if (y < h)
                SetVoxel(x, y, z, new Voxel(BlockID.Stone, 0, 0, BlockShape.Solid));
            else
                SetVoxel(x, y, z, new Voxel(BlockID.Air, 0, 0, BlockShape.Empty));
        }
    }

    private void AddGrass(int x, int y, int z)
    {
        if (GetVoxel(x, y, z).BlockID == BlockID.Stone && GetVoxel(x, y + 1, z).BlockID == BlockID.Air)
        {
            SetVoxel(x, y, z, new Voxel(BlockID.Grass));
            SetVoxel(x, y-1, z, new Voxel(BlockID.Dirt));
            SetVoxel(x, y-2, z, new Voxel(BlockID.Dirt));

        }
    }

    #endregion

}
