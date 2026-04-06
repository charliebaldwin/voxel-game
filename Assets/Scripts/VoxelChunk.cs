using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;
using static Perlin;
using static UnityEditor.PlayerSettings;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

public class VoxelChunk : MonoBehaviour
{
    public static bool DrawDebugs = false;

    public Vector3Int Size3D = new Vector3Int(16,32,16);
    private int bufferSizeMult = 24;
    ComputeBuffer cBuffer;
    ComputeBuffer vBuffer;
    ComputeBuffer nBuffer; 
    ComputeBuffer tBuffer;
    ComputeBuffer voxelBuffer;
    

    public int2 ChunkCoord;
    //public ChunkLoader ChunkLoader;
    //public RenderTexture voxelTex;
    //public RenderTexture testTex;

    public int[,,] voxelData = new int[1,1,1];
    private bool meshDirty = true;
    
    public Vector3 NoiseTranslate = Vector3.zero;
    public float NoiseScale = 0.1f;
    public float NoiseThreshold = 0.5f;

    public ComputeShader Compute;


    private MeshFilter meshFilter;
    private Mesh mesh;
    private MeshCollider meshCollider;

    private Vector3 lastPos = Vector3.zero;
    private float lastScale = 0f;
    private float lastThresh = 0f;
    private Vector3Int lastSize = Vector3Int.zero;

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
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        //voxelTex = new RenderTexture(voxelTex);

        lastPos = transform.position;
        lastScale = NoiseScale;
        lastThresh = NoiseThreshold;
        lastSize = Size3D;

    }


    private void Start()
    {
        GenerateVoxels(Compute);
        //ComputeMesh(Compute);

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
                Gizmos.DrawCube(new Vector3(v.x, v.y, v.z), Vector3.one);
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(tempOrigin, 0.5f);
        }

        //for (int x = 0; x < Size3D.x; x++)
        //{
        //    for (int z = 0; z < Size3D.z; z++)
        //    {
        //        if (threadTest[x, z] == 1)
        //        {
        //            Gizmos.DrawCube(transform.position + new Vector3(x, 0f, z), Vector3.one);
        //        }
        //    }
        //}
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                    if (voxelData[x,y,z] > 0)
                    {
                        switch (voxelData[x, y, z])
                        {
                            case 1:
                                Gizmos.color = Color.red; break;
                            case 2:
                                Gizmos.color = Color.green; break;
                            case 3:
                                Gizmos.color = Color.blue; break;
                        }

                      //  Gizmos.DrawCube(new Vector3(x, y,z) + transform.position, Vector3.one);
                    }

                }
            }
            
        }
    }

    private void Update()
    {
        Size3D.y = Mathf.Clamp(Size3D.y, 1, 128);

        //if (transform.position != lastPos || NoiseScale != lastScale || lastThresh != NoiseThreshold || lastSize != Size3D)
        //{
        //    lastPos = transform.position;
        //    lastScale = NoiseScale;
        //    lastThresh = NoiseThreshold;
        //    lastSize = Size3D;

        //    //VoxelNoise(Compute);
        //    ComputeMesh(Compute);

        //}
        

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
        voxelBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, sizeof(int));

        // Generate terrain shape (all stone)
        int kernel = compute.FindKernel("GenerateTerrain");
        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetVector("TranslateNoise", transform.position);
        compute.SetFloat("Scale", NoiseScale);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        // Add grass & dirt
        kernel = compute.FindKernel("SetTerrainBlocks");
        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        // Add ores
        //kernel = compute.FindKernel("AddOres");
        //compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        //compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        voxelBuffer.GetData(vData);
        voxelData = FlatTo3DArray(vData, Size3D);

    }

    private void ComputeMesh(ComputeShader compute)
    {
        int size3d = Size3D.x * Size3D.y * Size3D.z;

        vBuffer = new ComputeBuffer(bufferSizeMult * size3d, 3 * sizeof(float));
        nBuffer = new ComputeBuffer(bufferSizeMult * size3d, 3 * sizeof(float));
        cBuffer = new ComputeBuffer(bufferSizeMult * size3d, 4 * sizeof(float));
        tBuffer = new ComputeBuffer(bufferSizeMult * size3d, 2 * sizeof(float));

        voxelBuffer.SetData(ThreeDToFlatArray(voxelData, Size3D));

        int kernel = compute.FindKernel("ComputeMesh");
        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 1.0f));
        compute.SetBuffer(kernel, "Vertices", vBuffer);
        compute.SetBuffer(kernel, "Normals", nBuffer);
        compute.SetBuffer(kernel, "Colors", cBuffer);
        compute.SetBuffer(kernel, "TexCoords", tBuffer);

        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        //computeReadCoroutine = BufferReadTimer(BufferReadDelay);
        //StartCoroutine(computeReadCoroutine); 

        ReadBufferData();
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

        vBuffer.GetData(vData);
        nBuffer.GetData(nData);
        cBuffer.GetData(cData);
        tBuffer.GetData(tData);

        List<int> validIndices = GetValidIndices(vData);

        Vector3[] vDataTrimmed = new Vector3[validIndices.Count];
        Vector3[] nDataTrimmed = new Vector3[validIndices.Count];
        Color[] cDataTrimmed = new Color[validIndices.Count];
        Vector2[] tDataTrimmed = new Vector2[validIndices.Count];
        for (int i = 0; i < validIndices.Count; i++)
        {
            vDataTrimmed[i] = vData[validIndices[i]];
            nDataTrimmed[i] = nData[validIndices[i]];
            cDataTrimmed[i] = cData[validIndices[i]];
            tDataTrimmed[i] = tData[validIndices[i]];
        }
       
        meshFilter.mesh.Clear();
        meshFilter.mesh.vertices = vDataTrimmed;
        meshFilter.mesh.uv = tDataTrimmed;
        meshFilter.mesh.normals = nDataTrimmed;
        meshFilter.mesh.colors = cDataTrimmed;
        meshFilter.mesh.triangles = GenerateIndices(vDataTrimmed.Length);
        meshFilter.mesh.RecalculateBounds();

        meshCollider.sharedMesh = meshFilter.mesh;
    }

    private void BlockUpdate()
    {
        for (int x = 0; x < Size3D.x; x++) {  for(int y = 0; y < Size3D.y; y++) {  for(int z = 0; z < Size3D.z; z++) {

                    int voxelID = voxelData[x, y, z];
                    switch (voxelID)
                    {
                        case (1): // grass
                            if (y < Size3D.y - 1)
                            {
                                if (voxelData[x, y + 1, z] > 0)
                                {
                                    voxelData[x, y, z] = 2;
                                    meshDirty = true;
                                }
                            }
                            break;
                        case (2): // dirt
                            if (y < Size3D.y - 1)
                            {
                                if (voxelData[x, y + 1, z] == 0)
                                {
                                    // grow into dirt with random chance
                                    if (BlockRandomEvent(new int3(x, y, z), 0.0005f)) 
                                    {
                                        voxelData[x, y, z] = 1;
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


    private List<int> GetValidIndices(Vector3[] array)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null && array[i] != Vector3.zero)
            {
                result.Add(i);
            }
        }
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
    private int[,,] FlatTo3DArray(int[] flat, Vector3Int dimensions)
    {
        int[,,] result = new int[dimensions.x, dimensions.y, dimensions.z];

        for (int x = 0; x < dimensions.x; x++) {
            for (int y = 0; y < dimensions.y; y++) {
                for (int z = 0; z < dimensions.z; z++) {

                    result[x, y, z] = flat[x + dimensions.x * y + dimensions.x * dimensions.y * z];
                }
            }
        }
        return result;
    }
    private int[] ThreeDToFlatArray(int[,,] threeDarray, Vector3Int dimensions)
    {
        int[] result = new int[dimensions.x * dimensions.y * dimensions.z];
        for (int x = 0; x < dimensions.x; x++) {
            for (int y = 0; y < dimensions.y; y++) {
                for (int z = 0; z < dimensions.z; z++) {

                    result[x + dimensions.x * y + dimensions.x * dimensions.y * z] = threeDarray[x,y,z];
                }
            }
        }
        return result;
    }

    public VoxelHitData VoxelRaycast(Vector3 origin, Vector3 direction)
    {
        tempOrigin = origin;
        tempDirection = direction;
        tempCubes = new List<Vector4>();

        float stepDist = 0.05f;
        int stepCount = 300;

        VoxelHitData hitData = new VoxelHitData(false);

        Vector3 stepPos = origin;
        Vector3Int lastVoxPos = WorldPosToVoxel(stepPos + 0.5f * direction);

        for (int i = 0; i < stepCount; i++)
        {
            Vector3Int voxPos = WorldPosToVoxel(stepPos);
            stepPos = stepPos + direction * stepDist;
            Debug.Log($"({ChunkCoord.x},{ChunkCoord.y}) - i:{i}, worldVoxPos={voxPos + transform.position}, stepPos={stepPos}");

            // Debug.Log($"checking voxel {voxPos}");

            if (IsPosInGridBounds(voxPos, Size3D))
            {
                if (voxelData[voxPos.x, voxPos.y, voxPos.z] > 0)
                {
                    //return new Vector3(voxPos.x, voxPos.y, voxPos.z) + transform.position;
                    tempCubes.Add(new Vector4(voxPos.x, voxPos.y, voxPos.z, 1.0f) + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.0f));

                    //if (lastVoxPos - voxPos != Vector3.zero)
                    hitData.hitNormal = lastVoxPos - voxPos;

                    hitData.didHit = true;
                    hitData.hitPos = stepPos;
                    hitData.localVoxelPos = voxPos;
                    hitData.worldVoxelPos = voxPos + transform.position;
                    Debug.Log($"hit! at {hitData.worldVoxelPos}, normal={hitData.hitNormal}");
                    return hitData;

                }
                else
                {
                    tempCubes.Add(new Vector4(voxPos.x, voxPos.y, voxPos.z, 0.0f) + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.0f));
                }
            }
            else
            {
                // ray is outside bounds
                hitData.didHit = false;
                hitData.hitPos = stepPos;
                Debug.Log($"miss! at {hitData.worldVoxelPos}, normal={hitData.hitNormal}");
                return hitData;

            }
            stepPos = stepPos + direction * stepDist;
            hitData.hitPos = stepPos;
            lastVoxPos = voxPos;
            //return hitData;
        }
        return hitData;
    }

    public void BreakBlock(Vector3 worldPosition)
    {
        Vector3Int localPos = WorldPosToVoxel(worldPosition);
        voxelData[localPos.x, localPos.y, localPos.z] = 0;
        voxelBuffer.SetData(ThreeDToFlatArray(voxelData, Size3D));

        meshDirty = true;
    }
    public void PlaceBlock(Vector3 worldPosition, int blockType)
    {
        Vector3Int localPos = WorldPosToVoxel(worldPosition);

        if (voxelData[localPos.x, localPos.y, localPos.z] == 0)
        {
            voxelData[localPos.x, localPos.y, localPos.z] = blockType;
            voxelBuffer.SetData(ThreeDToFlatArray(voxelData, Size3D));

            meshDirty = true;
        }


    }

    public int LookupVoxel(Vector3 worldPos)
    {
        Vector3Int localPos = WorldPosToVoxel(worldPos);
        if (IsPosInGridBounds(localPos, Size3D))
        {
            return voxelData[localPos.x, localPos.y, localPos.z];
        } else {
            return 0;
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

   


}

public struct VoxelHitData
{
    public bool didHit;
    public int blockID;
    public Vector3Int localVoxelPos;
    public Vector3 worldVoxelPos;
    public Vector3 hitPos;
    public Vector3Int hitNormal;

    public VoxelHitData(bool didHit)
    {
        this.didHit = didHit;
        blockID = 0;
        localVoxelPos = Vector3Int.zero;
        worldVoxelPos = Vector3.zero;
        hitPos = Vector3.zero;
        hitNormal = Vector3Int.up;
    }
}
