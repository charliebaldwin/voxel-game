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
using static VoxelHelper;
using UnityEngine.VFX;


public class VoxelChunk : MonoBehaviour
{
    public static bool drawDebugs = false;
    public static bool useGreedy = false;

    public Vector3Int Size3D = new Vector3Int(16,32,16);
    private int bufferSizeMult = 24;
    ComputeBuffer cBuffer;
    ComputeBuffer vBuffer;
    ComputeBuffer nBuffer; 
    ComputeBuffer tBuffer;
    ComputeBuffer iBuffer;
    ComputeBuffer voxelBuffer;
    ComputeBuffer idBuffer;

    public bool Loaded = true;

    public int2 ChunkCoord;
    private VoxelChunk neighborNX;
    private VoxelChunk neighborPX;
    private VoxelChunk neighborNZ;
    private VoxelChunk neighborPZ;


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
    [SerializeField] private GameObject blockBreakVFXPrefab;
    [SerializeField] private GameObject blockHitVFXPrefab;



    private Vector3 tempOrigin = Vector3.zero;
    private Vector3 tempDirection = Vector3.forward;
    private List<Vector4> tempCubes = new List<Vector4>();


    private IEnumerator computeReadCoroutine;
    public float BufferReadDelay = 0.5f;


    private void Awake()
    {
        voxels = new VoxelData[Size3D.x, Size3D.y, Size3D.z];

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
            //ComputeMesh(Compute);

            if (useGreedy)
            {
                GreedyMesh();
            }
            else
            {
                ComputeMeshCPU();
            }
            meshDirty = false;
        }
    }
    private void OnValidate()
    {
        //if (useGreedy)
        //{
        //    GreedyMesh();
        //}
        //else
        //{
        //    ComputeMeshCPU();
        //}
    }

    public void SetVoxels(VoxelData[,,] newVoxels)
    {
        voxels = newVoxels;
    }

    public void FindNeighbors()
    {
        if (neighborNX == null)
            neighborNX = VoxelWorld.Instance.GetChunk(ChunkCoord + new int2(-1,  0));
        if (neighborPX == null)
            neighborPX = VoxelWorld.Instance.GetChunk(ChunkCoord + new int2( 1,  0));
        if (neighborNZ == null)
            neighborNZ = VoxelWorld.Instance.GetChunk(ChunkCoord + new int2( 0, -1));
        if (neighborPZ == null)
            neighborPZ = VoxelWorld.Instance.GetChunk(ChunkCoord + new int2( 0,  1));
    }
    public void InitializeChunk()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = new Mesh();
        meshCollider = GetComponent<MeshCollider>();

        //voxelBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, 3*sizeof(int));
        //voxelTex = new RenderTexture(voxelTex);


        //GenerateVoxels(Compute);
        //ComputeMesh(Compute);
        //GenerateVoxelsCPU();
        if (useGreedy)
        {
            GreedyMesh();
        }
        else
        {
            ComputeMeshCPU();
        }
    }



    private void ComputeMeshCPU()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        int t = 0;
        Vector3Int[] dirs = new Vector3Int[6] { Vector3Int.left, Vector3Int.right, Vector3Int.down, Vector3Int.up, Vector3Int.back, Vector3Int.forward };
        
        for (int z = 0; z < Size3D.z; z++) {
            for (int y = 0; y < Size3D.y; y++) {
                for (int x = 0; x < Size3D.x; x++) {

                    VoxelData vox = voxels[x, y, z];
                    if ((BlockShapes)vox.BlockShape == BlockShapes.SOLID)
                    {
                        int[] neighbors = new int[6] { 0, 0, 0, 0, 0, 0 };
                        Vector3 pos = new Vector3(x, y, z);
                        for (int n = 0; n < 6; n++)
                        {
                            Vector3Int n_pos = new Vector3Int(x, y, z) + dirs[n];
                           

                            neighbors[n] = VoxelWorld.Instance.LookupVoxel(LocalToWorld(n_pos, ChunkCoord, Size3D)).BlockShape;

                            //if ((BlockShapes)neighbors[n] != BlockShapes.SOLID)
                            //{
                            //    Vector3[] new_verts = GetFaceVerts(dirs[n]);
                            //    Vector2[] new_uvs = GetFaceUVs(dirs[n]);
                            //    for(int v=0; v<new_verts.Length; v++)
                            //    {
                            //        vertices.Add(new_verts[v] * 0.5f + pos);
                            //        normals.Add(dirs[n]);
                            //        colors.Add(new Color(vox.ID, n, (float)vox.Damage / (float)vox.Toughness));
                            //        uvs.Add(new_uvs[v]);
                            //    }
                            //    for (int i = 0; i < 6; i++)
                            //    {
                            //        triangles.Add(t + Triangles[i]);
                            //    }
                            //    t += 4;
                            //}
                        }

                        BlockModel model = new BlockModel(pos, t, neighbors, vox);
                        foreach (Vector3 v in model.vertices)   vertices.Add(v);
                        foreach (Vector3 n in model.normals)    normals.Add(n);
                        foreach (Vector2 uv in model.uvs)       uvs.Add(uv);
                        foreach (Color c in model.colors)       colors.Add(c);
                        foreach (int tri in model.triangles)    triangles.Add(tri);
                        t = model.lastT;
                    }
                    else if ((BlockShapes)vox.BlockShape == BlockShapes.HALF_SLAB)
                    {
                        Vector3 pos = new Vector3(x, y, z);
                        int[] neighbors = new int[6] { 0, 0, 0, 0, 0, 0 };
                        BlockModel model = new BlockModel(pos, t, neighbors, vox);
                        foreach (Vector3 v in model.vertices) vertices.Add(v);
                        foreach (Vector3 n in model.normals) normals.Add(n);
                        foreach (Vector2 uv in model.uvs) uvs.Add(uv);
                        foreach (Color c in model.colors) colors.Add(c);
                        foreach (int tri in model.triangles) triangles.Add(tri);
                        t = model.lastT;

                        //for (int n=0; n<6; n++)
                        //{
                        //    Vector3[] new_verts = GetFaceVerts(dirs[n]);
                        //    Vector2[] new_uvs = GetFaceUVs(dirs[n]);
                        //    for (int v = 0; v < new_verts.Length; v++)
                        //    {
                        //        Vector3 new_vert = new_verts[v];
                        //        new_vert.y = Mathf.Clamp(new_vert.y, -1f, 0f);
                        //        vertices.Add(new_vert * 0.5f + pos);
                        //        normals.Add(dirs[n]);
                        //        colors.Add(new Color(vox.ID, n, (float)vox.Damage / (float)vox.Toughness));
                        //        Vector2 new_uv = new_uvs[v];
                        //        if (n!=2 && n!=3)
                        //            new_uv.y = Mathf.Clamp(new_uv.y, 0.5f, 1f);
                        //        uvs.Add(new_uv);
                        //    }
                        //    for (int i = 0; i < 6; i++)
                        //    {
                        //        triangles.Add(t + Triangles[i]);
                        //    }
                        //    t += 4;
                        //}
                    }
                }
            }
        }

        //Debug.Log($"list counts - v:{vertices.Count()}, n:{normals.Count()}, c:{colors.Count()}, uv:{uvs.Count()}, t:{triangles.Count()}");
        mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.colors = colors.ToArray();
        mesh.uv = uvs.ToArray();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private void GreedyMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        int t = 0;
        for (int y = 0; y < Size3D.y; y++) {
            for (int x = 0; x < Size3D.x; x++) {
                for (int z = 0; z < Size3D.z; z++) {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (GetVoxelFaceVisible(pos, Directions[0]))
                    {
                        GreedyFace newFace = new GreedyFace(Directions[0], pos);
                        for (int i = z+1; i < Size3D.z; i++)
                        {
                            Vector3Int neighborPos = new Vector3Int(pos.x, pos.y, i);
                            if (GetVoxelFaceVisible(neighborPos, newFace.faceDirection))
                                newFace.lengthPrimary += 1;
                            else
                                break;
                        }
                        Vector3[] newVerts = newFace.GetFaceData();
                        foreach (Vector3 v in newVerts)
                        {
                            vertices.Add(v);
                            normals.Add(Directions[0]);
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            triangles.Add(t + Triangles[i]);
                        }
                        t += 4;
                        x += newFace.lengthPrimary-1;
                    }
                }
            }
        }
        for (int y = 0; y < Size3D.y; y++) {
            for (int x = 0; x < Size3D.x; x++) {
                for (int z = 0; z < Size3D.z; z++) {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (GetVoxelFaceVisible(pos, Directions[1]))
                    {
                        GreedyFace newFace = new GreedyFace(Directions[1], pos);
                        for (int i = z+1; i < Size3D.z; i++)
                        {
                            Vector3Int neighborPos = new Vector3Int(pos.x, pos.y, i);
                            if (GetVoxelFaceVisible(neighborPos, newFace.faceDirection))
                                newFace.lengthPrimary += 1;
                            else
                                break;
                        }
                        Vector3[] newVerts = newFace.GetFaceData();
                        foreach (Vector3 v in newVerts)
                        {
                            vertices.Add(v);
                            normals.Add(Directions[1]);
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            triangles.Add(t + Triangles[i]);
                        }
                        t += 4;
                        x += newFace.lengthPrimary-1;
                    }
                }
            }
        }

        for (int y = 0; y < Size3D.y; y++) {
            for (int z = 0; z < Size3D.z; z++) {
                for (int x = 0; x < Size3D.x; x++) {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (GetVoxelFaceVisible(pos, Directions[3]))
                    {
                        GreedyFace newFace = new GreedyFace(Directions[3], pos);
                        for (int i = x+1; i < Size3D.x; i++)
                        {
                            Vector3Int neighborPos = new Vector3Int(i, pos.y, pos.z);
                            if (GetVoxelFaceVisible(neighborPos, newFace.faceDirection))
                                newFace.lengthPrimary += 1;
                            else
                                break;
                        }
                        Vector3[] newVerts = newFace.GetFaceData();
                        foreach (Vector3 v in newVerts)
                        {
                            vertices.Add(v);
                            normals.Add(Directions[3]);
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            triangles.Add(t + Triangles[i]);
                        }
                        t += 4;
                        x += newFace.lengthPrimary-1;
                    }
                }
            }
        }
        for (int y = 0; y < Size3D.y; y++) {
            for (int z = 0; z < Size3D.z; z++) {
                for (int x = 0; x < Size3D.x; x++) {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (GetVoxelFaceVisible(pos, Directions[4]))
                    {
                        GreedyFace newFace = new GreedyFace(Directions[4], pos);
                        for (int i = x+1; i < Size3D.x; i++)
                        {
                            Vector3Int neighborPos = new Vector3Int(i, pos.y, pos.z);
                            if (GetVoxelFaceVisible(neighborPos, newFace.faceDirection))
                                newFace.lengthPrimary += 1;
                            else
                                break;
                        }
                        Vector3[] newVerts = newFace.GetFaceData();
                        foreach (Vector3 v in newVerts)
                        {
                            vertices.Add(v);
                            normals.Add(Directions[4]);
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            triangles.Add(t + Triangles[i]);
                        }
                        t += 4;
                        x += newFace.lengthPrimary-1;
                    }
                }
            }
        }
        for (int y = 0; y < Size3D.y; y++) {
            for (int z = 0; z < Size3D.z; z++) {
                for (int x = 0; x < Size3D.x; x++) {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (GetVoxelFaceVisible(pos, Directions[5]))
                    {
                        GreedyFace newFace = new GreedyFace(Directions[5], pos);
                        for (int i = x+1; i < Size3D.x; i++)
                        {
                            Vector3Int neighborPos = new Vector3Int(i, pos.y, pos.z);
                            if (GetVoxelFaceVisible(neighborPos, newFace.faceDirection))
                                newFace.lengthPrimary += 1;
                            else
                                break;
                        }
                        Vector3[] newVerts = newFace.GetFaceData();
                        foreach (Vector3 v in newVerts)
                        {
                            vertices.Add(v);
                            normals.Add(Directions[5]);
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            triangles.Add(t + Triangles[i]);
                        }
                        t += 4;
                        x += newFace.lengthPrimary-1;
                    }
                }
            }
        }
        mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private bool GetVoxelFaceVisible(Vector3Int pos, Vector3Int faceDirection)
    {
        VoxelData thisVoxel = VoxelWorld.Instance.LookupVoxel(LocalToWorld(pos, ChunkCoord, Size3D));
        VoxelData neighborVoxel = VoxelWorld.Instance.LookupVoxel(LocalToWorld(pos + faceDirection, ChunkCoord, Size3D));
        return neighborVoxel.ID == 0 && thisVoxel.ID > 0;
    }

    
    public void SetDirty()
    {
        meshDirty = true;
    }
    private void BlockUpdate()
    {
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    VoxelData voxel = voxels[x, y, z];
                    int voxelID = voxel.ID;
                    switch (voxelID)
                    {
                        case (Blocks.GRASS):
                            if (y < Size3D.y - 1)
                            {
                                if (Blocks.IsSolid(voxels[x, y + 1, z]))
                                {
                                    voxels[x, y, z].ID = Blocks.DIRT;
                                    meshDirty = true;
                                }
                            }
                            break;
                        case (Blocks.DIRT):
                            if (y < Size3D.y - 1)
                            {
                                VoxelData upVoxel = LookupVoxel(pos + new Vector3Int(0, 1, 0));
                                if (upVoxel.ID == Blocks.AIR)
                                {
                                    // grow into dirt with random chance
                                    if (BlockRandomEvent(new int3(x, y, z), 0.0005f))
                                    {
                                        voxels[x, y, z].ID = Blocks.GRASS;
                                        SetDirty();
                                    }
                                }
                            }
                            break;
                    }

                    if (voxels[x, y, z].Damage > 0 && !PlayerView.usingTool)
                    {
                        if (BlockRandomEvent(new int3(x, y, z), 0.003f))
                        {
                            voxel.Damage -= 1;
                            SetBlock(LocalToWorld(new Vector3Int(x, y, z), ChunkCoord, Size3D), voxel);
                            SetDirty();
                        }
                    }
                }
            }
        }
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
            if (array[i] != 0)
            {
                result.Add(i);
            }
        }
        Debug.Log($"num indices = {result.Count}");
        return result;
    }



    private const int DAMAGE_THRESH = 12;
    public void DamageBlock(Vector3Int worldPosition, Vector3 hitPosition, byte damage)
    {
        Vector3Int localPos = WorldToLocal(worldPosition, ChunkCoord, Size3D);
        VoxelData voxel = LookupVoxel(localPos);
        voxel.Damage += damage;
        GameObject hitVFX = Instantiate(blockHitVFXPrefab, hitPosition, Quaternion.identity);
        hitVFX.GetComponent<VFXObject>().InitVFX(voxel.ID, 0.5f);

        if (voxel.Damage >= voxel.Toughness)
        {
            GameObject breakVFX = Instantiate(blockBreakVFXPrefab, worldPosition, Quaternion.identity);
            breakVFX.GetComponent<VFXObject>().InitVFX(voxel.ID, 1f);

            voxel = new VoxelData(Blocks.AIR, 0, 0);
            voxel.BlockShape = 0;
        }
        SetBlock(worldPosition, voxel);

    }

    public void SetBlock(Vector3Int worldPosition, int blockType)
    {
        Vector3Int localPos = WorldToLocal(worldPosition, ChunkCoord, Size3D);
        if (IsPosInGridBounds(localPos, Size3D)) { 
            if (voxels[localPos.x, localPos.y, localPos.z].ID == 0 || true)
            {
                voxels[localPos.x, localPos.y, localPos.z] = new VoxelData(blockType, 0, 0);
                VoxelWorld.Instance.SetVoxel(worldPosition, new VoxelData(blockType, 0, 0));

                //voxelBuffer.SetData(VoxelDataToFlatArray(voxels));
                meshDirty = true;
                
                Vector3Int dirtyNeighbors = CheckPosOnEdge(localPos, Size3D);
                if (dirtyNeighbors.x == -1 && neighborNX != null)
                    neighborNX.SetDirty();
                if (dirtyNeighbors.x == 1 && neighborPX != null)
                    neighborPX.SetDirty();
                if (dirtyNeighbors.z == -1 && neighborNZ != null)
                    neighborNZ.SetDirty();
                if (dirtyNeighbors.z == 1 && neighborPZ  != null)
                    neighborPZ.SetDirty();

            }
        }
    }
    public void SetBlock(Vector3Int worldPosition, VoxelData voxelData)
    {
       
        Vector3Int localPos = WorldToLocal(worldPosition, ChunkCoord, Size3D);
        if (IsPosInGridBounds(localPos, Size3D))
        {
            if (voxels[localPos.x, localPos.y, localPos.z].ID == 0 || true)
            {
                voxels[localPos.x, localPos.y, localPos.z] = voxelData;
                VoxelWorld.Instance.SetVoxel(worldPosition, voxelData);

                //voxelBuffer.SetData(VoxelDataToFlatArray(voxels));
                meshDirty = true;

                Vector3Int dirtyNeighbors = CheckPosOnEdge(localPos, Size3D);
                if (dirtyNeighbors.x == -1 && neighborNX != null)
                    neighborNX.SetDirty();
                if (dirtyNeighbors.x == 1 && neighborPX != null)
                    neighborPX.SetDirty();
                if (dirtyNeighbors.z == -1 && neighborNZ != null)
                    neighborNZ.SetDirty();
                if (dirtyNeighbors.z == 1 && neighborPZ != null)
                    neighborPZ.SetDirty();

            }
        }
    }






    public VoxelData LookupVoxel(Vector3Int localPos)
    {
        return VoxelWorld.Instance.LookupVoxel(LocalToWorld(localPos, ChunkCoord, Size3D));
    }



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


    private void OnDrawGizmos()
    {
        if (drawDebugs)
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
    }





}


