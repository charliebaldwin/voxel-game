using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;
using static Perlin;
using static UnityEditor.PlayerSettings;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

public class VoxelChunk : MonoBehaviour
{
    public static bool DrawDebugs = true;

    public Vector3Int Size3D = new Vector3Int(16,32,16);
    private int bufferSizeMult = 24;
    ComputeBuffer cBuffer;
    ComputeBuffer vBuffer;
    ComputeBuffer nBuffer; 
    ComputeBuffer tBuffer;
    ComputeBuffer iBuffer;
    ComputeBuffer indexBuffer;
    ComputeBuffer voxelBuffer;
    ComputeBuffer idBuffer;
    

    public int2 ChunkCoord;


    public int[,,] voxelData = new int[1,1,1];
    public VoxelData[,,] voxels = new VoxelData[1,1,1];
    private bool meshDirty = true;
    
    public Vector3 NoiseTranslate = Vector3.zero;
    public float NoiseScale = 0.1f;
    public float NoiseThreshold = 0.5f;

    public ComputeShader Compute;


    private MeshFilter meshFilter;
    private Mesh mesh;
    private MeshCollider meshCollider;


    private Vector3 tempOrigin = Vector3.zero;
    private Vector3 tempDirection = Vector3.forward;
    private List<Vector4> tempCubes = new List<Vector4>();

    private VoxelChunk adjacentChunkNX;
    private VoxelChunk adjacentChunkPX;
    private VoxelChunk adjacentChunkNZ;
    private VoxelChunk adjacentChunkPZ;

    private IEnumerator computeReadCoroutine;
    public float BufferReadDelay = 0.5f;


    private void Awake()
    {
        InitializeChunk();

    }


    private void Start()
    {
        //GenerateVoxels(Compute);
        //ComputeMesh(Compute);

    }

    public void InitializeChunk()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = new Mesh();
        meshCollider = GetComponent<MeshCollider>();

        //voxelBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, 3*sizeof(int));
        //voxelTex = new RenderTexture(voxelTex);


        GenerateVoxels(Compute);
        ComputeMesh(Compute);
    }

    private void OnDrawGizmos()
    {
        if (DrawDebugs)
        {
            Gizmos.color = Color.green;


            Gizmos.color = Color.white;
            Gizmos.DrawRay(tempOrigin, 100f * tempDirection);
            foreach (Vector4 v in tempCubes)
            {
                if (v.w == 1.0f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(new Vector3(v.x, v.y, v.z), Vector3.one);
                }
                else
                {
                    Gizmos.color = Color.white;
                }
                Gizmos.DrawSphere(new Vector3(v.x, v.y, v.z), 0.1f);
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(tempOrigin, 0.5f);
        }
    }

    private void FixedUpdate()
    {
        BlockUpdate();
    }
    private void LateUpdate()
    {
        //vBuffer.Release();
        //nBuffer.Release();
        //cBuffer.Release();
        if (meshDirty)
        {
            ComputeMesh(Compute);
            meshDirty = false;
        }
    }


    private void GenerateVoxels(ComputeShader compute)
    {

        int[] vData = new int[Size3D.x * Size3D.y * Size3D.z];
        idBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, sizeof(int));

        // Generate terrain shape (all stone)
        int kernel = compute.FindKernel("GenerateTerrain");
        compute.SetBuffer(kernel, "VoxelIDs", idBuffer);
        compute.SetVector("TranslateNoise", transform.position);
        compute.SetFloat("Scale", NoiseScale);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.Dispatch(kernel, 1, Size3D.y, 1);

        // Add grass & dirt
        kernel = compute.FindKernel("SetTerrainBlocks");
        compute.SetBuffer(kernel, "VoxelIDs", idBuffer);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.Dispatch(kernel, 1, Size3D.y, 1);

        // Add ores
        //kernel = compute.FindKernel("AddOres");
        //compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        //compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        idBuffer.GetData(vData);
        //voxelData = FlatTo3DArray(vData);

        voxels = IntTo3DVoxelData(vData);
        //voxelBuffer.SetData(VoxelDataToFlatArray(voxels));


    }

    private void ComputeMesh(ComputeShader compute)
    {
        int size3d = Size3D.x * Size3D.y * Size3D.z;

        vBuffer = new ComputeBuffer(bufferSizeMult * size3d, 3 * sizeof(float), ComputeBufferType.Counter);
        vBuffer.SetCounterValue(0);
        nBuffer = new ComputeBuffer(bufferSizeMult * size3d, 3 * sizeof(float));
        cBuffer = new ComputeBuffer(bufferSizeMult * size3d, 4 * sizeof(float));
        tBuffer = new ComputeBuffer(bufferSizeMult * size3d, 2 * sizeof(float));
        iBuffer = new ComputeBuffer(bufferSizeMult * size3d, 1 * sizeof(int));
        indexBuffer = new ComputeBuffer(bufferSizeMult * size3d, 1 * sizeof(int), ComputeBufferType.Counter);
        indexBuffer.SetCounterValue(0);

        voxelBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, sizeof(int) * 3);
        voxelBuffer.SetData(VoxelDataToFlatArray(voxels));

        int kernel = compute.FindKernel("ComputeMesh");
        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 1.0f));
        compute.SetBuffer(kernel, "Vertices", vBuffer);
        compute.SetBuffer(kernel, "Normals", nBuffer);
        compute.SetBuffer(kernel, "Colors", cBuffer);
        compute.SetBuffer(kernel, "TexCoords", tBuffer);
        compute.SetBuffer(kernel, "ValidIndices", iBuffer);
        compute.SetBuffer(kernel, "Indices", indexBuffer);

        compute.Dispatch(kernel, 1, Size3D.y, 1);

        computeReadCoroutine = BufferReadTimer(BufferReadDelay);
        StartCoroutine(computeReadCoroutine); 
        
        //ReadBufferData();
    }

    private IEnumerator BufferReadTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        ReadBufferData();
    }

    private void ReadBufferData()
    {
        int size3d = Size3D.x * Size3D.y * Size3D.z;

        Vector3[] vData = new Vector3[bufferSizeMult * size3d];
        Vector3[] nData = new Vector3[bufferSizeMult * size3d];
        Color[] cData = new Color[bufferSizeMult * size3d];
        Vector2[] tData = new Vector2[bufferSizeMult * size3d];
        int[] iData = new int[bufferSizeMult * size3d];
        int[] indexData = new int[bufferSizeMult * size3d];

        vBuffer.GetData(vData);
        nBuffer.GetData(nData);
        cBuffer.GetData(cData);
        tBuffer.GetData(tData);
        iBuffer.GetData(iData);
        indexBuffer.GetData(indexData);

        List<int> validIndices = GetValidIndices(iData);

        Vector3[] vDataTrimmed = new Vector3[validIndices.Count];
        Vector3[] nDataTrimmed = new Vector3[validIndices.Count];
        Color[] cDataTrimmed = new Color[validIndices.Count];
        Vector2[] tDataTrimmed = new Vector2[validIndices.Count];
        tempCubes = new List<Vector4>();
        for (int i = 0; i < validIndices.Count; i++)
        {
            Vector3 vert = vData[validIndices[i]];
            tempCubes.Add(new Vector4(vert.x, vert.y, vert.z));
            vDataTrimmed[i] = vData[validIndices[i]];
            nDataTrimmed[i] = nData[validIndices[i]];
            cDataTrimmed[i] = cData[validIndices[i]];
            tDataTrimmed[i] = tData[validIndices[i]];
        }
       
        meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh.vertices = vData;
        meshFilter.sharedMesh.uv = tData;
        meshFilter.sharedMesh.normals = nData;
        meshFilter.sharedMesh.colors = cData;
        meshFilter.sharedMesh.triangles = indexData; // GenerateIndices(vData.Length);
        meshFilter.sharedMesh.RecalculateBounds(); 

        //meshCollider.sharedMesh = meshFilter.sharedMesh;
    }

    private void BlockUpdate()
    {
        for (int x = 0; x < Size3D.x; x++) {  for(int y = 0; y < Size3D.y; y++) {  for(int z = 0; z < Size3D.z; z++) {

            int voxelID = voxels[x, y, z].ID;
            switch (voxelID)
            {
                case (1): // grass
                    if (y < Size3D.y - 1)
                    {
                        if (voxels[x, y + 1, z].ID> 0)
                        {
                            voxels[x, y, z].ID = 2;
                            meshDirty = true;
                        }
                    }
                    break;
                case (2): // dirt
                    if (y < Size3D.y - 1)
                    {
                        if (voxels[x, y + 1, z].ID== 0)
                        {
                            // grow into dirt with random chance
                            if (BlockRandomEvent(new int3(x, y, z), 0.0005f)) 
                            {
                                voxels[x, y, z].ID = 1;
                                meshDirty = true;
                            }
                        }
                    }
                    break;
            }
        } } }
    }
    private bool BlockRandomEvent(int3 pos, float probability)
    {
        int seed = (pos.x + Size3D.x * pos.y + Size3D.x * Size3D.y * pos.z) + (1000*ChunkCoord.x + 10000*ChunkCoord.y) + (Time.frameCount % 10000);
        Random.InitState(seed);
        return Random.Range(0f, 1f) < probability;
    }


    private List<int> GetValidIndices(int[] array)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != 0)// && array[i] != null )
            {
                result.Add(i);
            }
        }
        Debug.Log($"num vertices = {array.Length}");
        Debug.Log($"num indices = {result.Count}");
        return result;
    }
    private int[] GenerateIndices(int vertexCount)
    {
        int[] result = new int[(vertexCount / 4) * 6];
        for (int i=0; i < vertexCount/4 - 0; i++)
        {
            result[i * 6 + 0] = i * 4 + 0;
            result[i * 6 + 1] = i * 4 + 1;
            result[i * 6 + 2] = i * 4 + 2;
            result[i * 6 + 3] = i * 4 + 0;
            result[i * 6 + 4] = i * 4 + 2;
            result[i * 6 + 5] = i * 4 + 3;
        }
        return result;
    }
    private int[,,] FlatTo3DArray(int[] flat)
    {
        int[,,] result = new int[Size3D.x, Size3D.y, Size3D.z];

        for (int x = 0; x < Size3D.x; x++) {
            for (int y = 0; y < Size3D.y; y++) {
                for (int z = 0; z < Size3D.z; z++) {

                    result[x, y, z] = flat[x + Size3D.x * y + Size3D.x * Size3D.y * z];
                }
            }
        }
        return result;
    }
    private int[] ThreeDToFlatArray(int[,,] threeDarray)
    {
        int[] result = new int[Size3D.x * Size3D.y * Size3D.z];
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {

                    result[x + Size3D.x * y + Size3D.x * Size3D.y * z] = threeDarray[x, y, z];
                }
            }
        }
        return result;
    }

    private VoxelData[,,] IntTo3DVoxelData(int[] flat)
    {
        VoxelData[,,] result = new VoxelData[Size3D.x, Size3D.y, Size3D.z];

        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                    int id = flat[x + Size3D.x * y + Size3D.x * Size3D.y * z];
                    result[x, y, z] = new VoxelData(id, 0, 0);
                }
            }
        }
        return result;
    }
    private VoxelData[] VoxelDataToFlatArray(VoxelData[,,] threeDarray)
    {
        VoxelData[] result = new VoxelData[Size3D.x * Size3D.y * Size3D.z];
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                    result[x + Size3D.x * y + Size3D.x * Size3D.y * z] = threeDarray[x, y, z];
                }
            }
        }
        return result;
    }


    public void DamageBlock(Vector3 worldPosition, byte damage)
    {
        Vector3Int localPos = WorldPosToVoxel(worldPosition);
        voxels[localPos.x, localPos.y, localPos.z].Damage += damage;
        meshDirty = true;
        if (voxels[localPos.x, localPos.y, localPos.z].Damage >= 3)
        {
            BreakBlock(worldPosition);
        }

    }
    public void BreakBlock(Vector3 worldPosition)
    {
        Vector3Int localPos = WorldPosToVoxel(worldPosition);
        //voxelData[localPos.x, localPos.y, localPos.z] = 0;
        //voxelBuffer.SetData(ThreeDToFlatArray(voxelData));
        voxels[localPos.x, localPos.y, localPos.z] = new VoxelData(0,0,0);

        meshDirty = true;
    }
    public void PlaceBlock(Vector3 worldPosition, int blockType)
    {
        Vector3Int localPos = WorldPosToVoxel(worldPosition);

        //if (voxelData[localPos.x, localPos.y, localPos.z] == 0)
        //{
        //    voxelData[localPos.x, localPos.y, localPos.z] = blockType;
        //    voxelBuffer.SetData(ThreeDToFlatArray(voxelData));

        //    meshDirty = true;
        //}
        if (voxels[localPos.x, localPos.y, localPos.z].ID == 0)
        {
            voxels[localPos.x, localPos.y, localPos.z] = new VoxelData(blockType, 0, 0);
            //voxelBuffer.SetData(VoxelDataToFlatArray(voxels));
            meshDirty = true;
        }
    }

    public VoxelData LookupVoxel(Vector3 worldPos)
    {
        Vector3Int localPos = WorldPosToVoxel(worldPos);
        if (IsPosInGridBounds(localPos, Size3D))
        {
            return voxels[localPos.x,localPos.y,localPos.z];    
            //return voxelData[localPos.x, localPos.y, localPos.z];
        } else {
            return new VoxelData(-1,0,0);
        }
    }

    private Vector3Int WorldPosToVoxel(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        Vector3Int result = new Vector3Int(Mathf.RoundToInt(localPos.x), Mathf.RoundToInt(localPos.y), Mathf.RoundToInt(localPos.z));
        return result;
    }

    private bool IsPosInGridBounds(Vector3Int pos, Vector3Int size)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.z >= 0 && pos.x < size.x && pos.y < size.y && pos.z < size.z;
    }

    //private void OnApplicationQuit()
    //{
    //    SaveMesh();
    //}
    //public void SaveMesh()
    //{
    //    AssetDatabase.CreateAsset(meshFilter.sharedMesh, $"Assets/Cache/mesh_{ChunkCoord.x}_{ChunkCoord.y}.asset");
    //}

   


}


