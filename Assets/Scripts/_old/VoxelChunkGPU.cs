using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using static VoxelHelper;

public class VoxelChunkGPU : MonoBehaviour
{
    public Vector3Int Size3D = new Vector3Int(16, 32, 16);

    private MeshFilter meshFilter;
    private Mesh mesh;
    private MeshCollider meshCollider;

    public ComputeShader Compute;
    private int bufferSizeMult = 24;
    ComputeBuffer cBuffer;
    ComputeBuffer vBuffer;
    ComputeBuffer nBuffer;
    ComputeBuffer tBuffer;
    ComputeBuffer iBuffer;
    ComputeBuffer voxelBuffer;
    ComputeBuffer idBuffer;

    private IEnumerator computeReadCoroutine;
    public float BufferReadDelay = 0.5f;

    public Vector3 NoiseTranslate = Vector3.zero;
    public float NoiseScale = 0.1f;
    public float NoiseThreshold = 0.5f;

    public int[,,] voxelData = new int[1, 1, 1];
    public Voxel[,,] voxels = new Voxel[1, 1, 1];


    private void ComputeMesh(ComputeShader compute)
    {
        int size3d = Size3D.x * Size3D.y * Size3D.z;

        vBuffer = new ComputeBuffer(bufferSizeMult * size3d, 3 * sizeof(float));
        nBuffer = new ComputeBuffer(bufferSizeMult * size3d, 3 * sizeof(float));
        cBuffer = new ComputeBuffer(bufferSizeMult * size3d, 4 * sizeof(float));
        tBuffer = new ComputeBuffer(bufferSizeMult * size3d, 2 * sizeof(float));
        iBuffer = new ComputeBuffer(bufferSizeMult * size3d, 1 * sizeof(int));

        voxelBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, sizeof(int) * 3);
        voxelBuffer.SetData(VoxelDataToFlatArray(voxels, Size3D));

        int kernel = compute.FindKernel("ComputeMesh");
        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 1.0f));
        compute.SetBuffer(kernel, "Vertices", vBuffer);
        compute.SetBuffer(kernel, "Normals", nBuffer);
        compute.SetBuffer(kernel, "Colors", cBuffer);
        compute.SetBuffer(kernel, "TexCoords", tBuffer);
        compute.SetBuffer(kernel, "ValidIndices", iBuffer);

        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        computeReadCoroutine = BufferReadTimer(BufferReadDelay);
        StartCoroutine(computeReadCoroutine);

        //ReadBufferData();
    }
    //private void ComputeMeshCPU()
    //{
    //    vertices = new List<Vector3>();
    //    normals = new List<Vector3>();
    //    uvs = new List<Vector2>();
    //    triangles = new List<int>();
    //    colors = new List<Color>();
    //    t = 0;
    //    /**
    //    // get model data for each voxel
    //    for (int z = 0; z < Size3D.z; z++) {
    //        for (int y = 0; y < Size3D.y; y++) {
    //            for (int x = 0; x < Size3D.x; x++) {

    //                VoxelData vox = voxels[x, y, z];
    //                if ((BlockShapes)vox.BlockShape != BlockShapes.EMPTY)
    //                {
    //                    Vector3 pos = new Vector3(x, y, z);

    //                    int[] neighbors = new int[6] { 0, 0, 0, 0, 0, 0 };
    //                    for (int n = 0; n < 6; n++)
    //                        neighbors[n] = World().LookupVoxel(LocalToWorld(new Vector3Int(x, y, z) + Directions[n], ChunkCoord, Size3D)).BlockShape;

    //                    BlockModel model = new BlockModel(pos, t, neighbors, vox);
    //                    foreach (Vector3 v in model.vertices) vertices.Add(v);
    //                    foreach (Vector3 n in model.normals) normals.Add(n);
    //                    foreach (Vector2 uv in model.uvs) uvs.Add(uv);
    //                    foreach (Color c in model.colors) colors.Add(c);
    //                    foreach (int tri in model.triangles) triangles.Add(tri);
    //                    t = model.lastT;
    //                }
    //            }
    //        }
    //    }
    //    **/

    //    Loop3D(ComputeMeshAction);

    //    // send all data for chunk into mesh
    //    mesh = new Mesh();
    //    mesh.vertices = vertices.ToArray();
    //    mesh.triangles = triangles.ToArray();
    //    mesh.normals = normals.ToArray();
    //    mesh.colors = colors.ToArray();
    //    mesh.uv = uvs.ToArray();
    //    meshFilter.mesh = mesh;
    //    meshCollider.sharedMesh = mesh;

    //    vertices.Clear();
    //    normals.Clear();
    //    uvs.Clear();
    //    triangles.Clear();
    //    colors.Clear();
    //}
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

        vBuffer.GetData(vData);
        nBuffer.GetData(nData);
        cBuffer.GetData(cData);
        tBuffer.GetData(tData);
        iBuffer.GetData(iData);

        List<int> validIndices = GetValidIndices(iData);

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

        meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh.vertices = vDataTrimmed;
        meshFilter.sharedMesh.uv = tDataTrimmed;
        meshFilter.sharedMesh.normals = nDataTrimmed;
        meshFilter.sharedMesh.colors = cDataTrimmed;
        meshFilter.sharedMesh.triangles = GenerateIndices(vDataTrimmed.Length);
        meshFilter.sharedMesh.RecalculateBounds();

        meshCollider.sharedMesh = meshFilter.sharedMesh;
    }

    private List<int> GetValidIndices(int[] array)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != 0)
            {
                result.Add(i);
            }
        }
        Debug.Log($"num indices = {result.Count}");
        return result;
    }
}
