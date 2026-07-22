using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using VInspector;
//using static Perlin;
//using static UnityEditor.PlayerSettings;
// static UnityEditor.Searcher.SearcherWindow.Alignment;
using static VoxelHelper;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;


public class VoxelChunk : MonoBehaviour
{
    private Voxel[,,] Voxels = new Voxel[1, 1, 1];
    private Dictionary<Vector3Int, BlockEntityActor> BlockEntities;

    public static Vector3Int Size3D = new Vector3Int(16,32,16);
    public Vector3Int ChunkCoord { get; private set; }

    public bool Loaded { get; private set; } = false;

    private VoxelChunk neighborNX;
    private VoxelChunk neighborPX;
    private VoxelChunk neighborNZ;
    private VoxelChunk neighborPZ;

    private List<Vector3Int> activeVoxels = new List<Vector3Int>();


    private bool meshDirty = true;

    [Foldout("References")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Mesh mesh;
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private GameObject blockBreakVFXPrefab;
    [SerializeField] private GameObject blockHitVFXPrefab;
    [EndFoldout]


    public static bool drawDebugs = false;
    public static bool useGreedy = false;
    private Vector3 tempOrigin = Vector3.zero;
    private Vector3 tempDirection = Vector3.forward;
    private List<Vector4> tempCubes = new List<Vector4>();


    private JobHandle handle;
    //private bool jobActive = false;
    public NativeArray<Vector3> verticesResult;
    public NativeArray<Vector3> normalsResult;
    public NativeArray<Vector2> uvsResult;
    public NativeArray<int> trianglesResult;
    public NativeArray<Color> colorsResult;

    readonly ProfilerMarker meshMarker = new ProfilerMarker("Chunk Mesher");

    private VoxelWorld world;

    private List<BlockID> containedIDs = new List<BlockID>();


    private void Awake()
    {
        //Voxels = new VoxelData[Size3D.x, Size3D.y, Size3D.z];
        world = World();
    }
    private void FixedUpdate()
    {
        //BlockUpdate();
        //Loop3D(BlockUpdateAction);
        if (meshDirty && Loaded)
        {

            ChunkMesh();

            meshDirty = false;
        }
    }
    private void LateUpdate()
    {
        //if (jobActive)
        //{
        //    if (handle.IsCompleted)
        //    {
        //        FinishChunkJob();
        //    }
        //}

    }

    public void InitializeChunk(Vector3Int size, Vector3Int chunkCoord)
    {
        Size3D = size;
        Voxels = new Voxel[Size3D.x, Size3D.y, Size3D.z];
        ChunkCoord = chunkCoord;
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider.enabled = false;
        meshRenderer.enabled = false;
        gameObject.SetActive(false);
        BlockEntities = new Dictionary<Vector3Int, BlockEntityActor>();
    }
    public VoxelChunk LoadChunk()
    {
        //Debug.Log($"loaded {name}");
        gameObject.SetActive(true);

        Loaded = true;

        meshRenderer.enabled = true;
        meshFilter.sharedMesh = new Mesh();

        ChunkMesh();

        meshCollider.enabled = true;

        foreach(KeyValuePair<Vector3Int, BlockEntityActor> entity in BlockEntities)
        {
            entity.Value.LoadEntity();
        }
        return this;
    }
    public VoxelChunk UnloadChunk()
    {
        Loaded = false;
        meshCollider.enabled = false;
        meshRenderer.enabled = false;
        meshFilter.sharedMesh.Clear();

        foreach (KeyValuePair<Vector3Int, BlockEntityActor> entity in BlockEntities)
        {
            entity.Value.UnloadEntity();
        }

        gameObject.SetActive(false);


        return this;
    }
    public void FillVoxelData(Voxel[,,] newVoxels)
    {
        Voxels = newVoxels;
        activeVoxels = new List<Vector3Int>();
        Loop3D(FillActiveVoxelsAction);
    }

    public void FindNeighbors()
    {
        if (neighborNX == null)
            neighborNX = world.GetChunk(ChunkCoord + Vector3Int.left);
        if (neighborPX == null)
            neighborPX = world.GetChunk(ChunkCoord + Vector3Int.right);
        if (neighborNZ == null)
            neighborNZ = world.GetChunk(ChunkCoord + Vector3Int.back);
        if (neighborPZ == null)
            neighborPZ = world.GetChunk(ChunkCoord + Vector3Int.forward);
    }

    private void Loop3D(Action<int, int, int> loopFunction)
    {
        for (int z = 0; z < Size3D.z; z++) {
            for (int y = 0; y < Size3D.y; y++) {
                for (int x = 0; x < Size3D.x; x++) {
                    loopFunction(x, y, z);
                }
            }
        }
    }

    #region MESHING
    public void SetDirty()
    {
        meshDirty = true;
    }
    private void ChunkMesh()
    {
        if (useGreedy)
            GreedyMesh();
        else
            ComputeMeshCPU();

    }

    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector4> uvs = new List<Vector4>();
    private Dictionary<BlockID, List<int>> submeshes = new Dictionary<BlockID, List<int>>();
    private List<int>     triangles = new List<int>();
    private List<int>     triangles2 = new List<int>();
    private List<Color>   colors = new List<Color>();
    private int           t = 0;
    private void ComputeMeshCPU()
    {
        meshMarker.Begin();

        vertices = new List<Vector3>();
        normals = new List<Vector3>();
        uvs = new List<Vector4>();
        triangles = new List<int>();
        colors = new List<Color>();

        UpdateMaterials();

        submeshes = new Dictionary<BlockID, List<int>>();
        foreach (BlockID id in containedIDs)
        {
            submeshes.Add(id, new List<int>());
        }

        t = 0;

        Loop3D(ComputeMeshAction);

        // send all data for chunk into mesh
        mesh = new Mesh();

        //mesh.triangles = triangles.ToArray();
        //SubMeshDescriptor[] sm = new SubMeshDescriptor[]
        //{
        //    new SubMeshDescriptor(triangles[0], 3, MeshTopology.Triangles),
        //    new SubMeshDescriptor(triangles2[0], 3 , MeshTopology.Triangles)
        //};
        //mesh.SetSubMeshes(sm);
        mesh.subMeshCount = submeshes.Count;
        mesh.vertices = vertices.ToArray();
        //mesh.SetTriangles(triangles.ToArray(), 0);
        int i = 0;
        foreach (KeyValuePair<BlockID, List<int>> s in submeshes) 
        {
            mesh.SetTriangles(submeshes[s.Key].ToArray(), i);
            i++;
        }
        mesh.normals = normals.ToArray();
        mesh.colors = colors.ToArray();
        mesh.SetUVs(0,uvs.ToArray());
        mesh.RecalculateTangents();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshRenderer.materials = materials;

        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        triangles.Clear();
        colors.Clear();

        meshMarker.End();
    }

    readonly ProfilerMarker voxelMarker = new ProfilerMarker("Mesh for Single Voxel");
    private void ComputeMeshAction(int x, int y, int z)
    {
        Voxel vox = Voxels[x, y, z];
        if (vox.BlockID == BlockID.Air || vox.BlockID == BlockID.Machine) return;
        voxelMarker.Begin();
        //if ((BlockShape)vox.Shape != BlockShape.Empty && !BlockRegistry.LookupBlock(vox.BlockID).IsBlockEntity )
        //{
        Vector3 pos = new Vector3(x, y, z);

        Voxel[] neighborVoxels = new Voxel[6];
        for (int n = 0; n < 6; n++)
        {
            Vector3Int dir = OrthoDirections[n].AlignYZ(vox.UpAxis, vox.ForwardAxis).ToVector();
            Voxel neighbor = world.LookupVoxelWorld(LocalToWorld(new Vector3Int(x, y, z) + dir, ChunkCoord, Size3D));
            neighborVoxels[n] = neighbor;
            //neighbors[n] = (int)world.LookupVoxel(LocalToWorld(new Vector3Int(x, y, z) + Directions[n], ChunkCoord, Size3D)).Shape;
        }
        BlockModel model = new BlockModel(pos, t, vox, neighborVoxels);
        foreach (Vector3 v in model.vertices) vertices.Add(v);
        foreach (Vector3 n in model.normals) normals.Add(n);
        foreach (Vector4 uv in model.uvs) uvs.Add(uv);
        foreach (Color c in model.colors) colors.Add(c);
        //foreach (int tri in model.triangles) triangles2.Add(tri);

        foreach (int tri in model.triangles)
        {
            submeshes[vox.BlockID].Add(tri);
        }
        
        t = model.lastT;
        //}
        voxelMarker.End();
    }

    Material[] materials;
    private void UpdateMaterials()
    {
        materials = new Material[containedIDs.Count];
        int i = 0;
        foreach(BlockID id in containedIDs)
        {
            materials[i] = BlockRegistry.LookupBlock(id).Material;
            i++;
        }
    }

    private bool GetVoxelFaceVisible(Vector3Int pos, Vector3Int faceDirection)
    {
        Voxel thisVoxel = world.LookupVoxelWorld(LocalToWorld(pos, ChunkCoord, Size3D));
        Voxel neighborVoxel = world.LookupVoxelWorld(LocalToWorld(pos + faceDirection, ChunkCoord, Size3D));
        return neighborVoxel.BlockID == 0 && thisVoxel.BlockID > 0;
    }
    #endregion

    #region BLOCK UPDATE
    public void BlockUpdate()
    {
        BlockUpdateFast();
        //Loop3D(BlockUpdateAction);
    }
    private void BlockUpdateAction(int  x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        Voxel voxel = Voxels[x, y, z];
        BlockID voxelID = voxel.BlockID;
        switch (voxelID)
        {
            case (BlockID.Grass):
                if (y < Size3D.y - 1)
                {
                    if (Voxels[x, y + 1, z].BlockID != BlockID.Air)
                    {
                        world.SetVoxel(LocalToWorld(pos, ChunkCoord, Size3D), new Voxel(BlockID.Dirt, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));
                        meshDirty = true;
                    }
                }
                break;
            case (BlockID.Dirt):
                if (y < Size3D.y - 1)
                {
                    //Voxel upVoxel = World().LookupVoxel(LocalToWorldPos((pos + new Vector3Int(0, 1, 0))));
                    Voxel upVoxel = GetVoxelLocal(pos + Vector3Int.up);
                    if (upVoxel.BlockID == BlockID.Air)
                    {
                        // grow into dirt with random chance
                        if (BlockRandomEvent(new int3(x, y, z), 0.04f))
                        {
                            world.SetVoxel(LocalToWorld(pos, ChunkCoord, Size3D), new Voxel(BlockID.Grass, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));

                            SetDirty();
                        }
                    }
                }
                break;
        }

        if (Voxels[x, y, z].Damage > 0 && !PlayerView.usingTool)
        {
            if (BlockRandomEvent(new int3(x, y, z), 0.05f))
            {
                voxel.Damage -= 1;
                //SetBlock(LocalToWorld(new Vector3Int(x, y, z), ChunkCoord, Size3D), voxel);
                world.SetVoxel(LocalToWorld(new Vector3Int(x, y, z), ChunkCoord, Size3D), voxel);
                SetDirty();
            }
        }
    }

    private void FillActiveVoxelsAction(int x, int y, int z)
    {
        BlockID id = Voxels[x, y, z].BlockID;
        if (id != BlockID.Air )
        {
            if (!containedIDs.Contains(id))
            {
                containedIDs.Add(id);
            }
            activeVoxels.Add(new Vector3Int(x, y, z));
        }
    }

    private void BlockUpdateFast()
    {
        if (activeVoxels.Count > 0)
        {
            //Debug.Log($"updating chunk {ChunkCoord}- there are {activeVoxels.Count} voxels to check");
            Vector3Int[] tempActiveVoxels = new Vector3Int[activeVoxels.Count];
            activeVoxels.CopyTo(tempActiveVoxels);
            foreach (Vector3Int pos in tempActiveVoxels)
            {
                if (IsPosInGridBounds(pos, Size3D))
                {
                    Voxel voxel = Voxels[pos.x, pos.y, pos.z];
                    BlockID voxelID = voxel.BlockID;
                    switch (voxelID)
                    {
                        case (BlockID.Air):
                            activeVoxels.Remove(pos);
                            break;
                        case (BlockID.Grass):
                            if (pos.y < Size3D.y - 1)
                            {
                                if (Voxels[pos.x, pos.y + 1, pos.z].BlockID != BlockID.Air)
                                {
                                    world.SetVoxel(LocalToWorld(pos, ChunkCoord, Size3D), new Voxel(BlockID.Dirt, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));
                                    meshDirty = true;
                                }

                            }
                            if (voxel.Damage <= 0) activeVoxels.Remove(pos);
                            break;
                        case (BlockID.Dirt):
                            if (pos.y < Size3D.y - 1)
                            {
                                //Voxel upVoxel = World().LookupVoxel(LocalToWorldPos((pos + new Vector3Int(0, 1, 0))));
                                Voxel upVoxel = GetVoxelLocal(pos + Vector3Int.up);
                                if (upVoxel.BlockID == BlockID.Air)
                                {
                                    // grow into dirt with random chance
                                    if (BlockRandomEvent(new int3(pos.x, pos.y, pos.z), 0.04f))
                                    {
                                        world.SetVoxel(LocalToWorld(pos, ChunkCoord, Size3D), new Voxel(BlockID.Grass, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));
                                        if (voxel.Damage <= 0) activeVoxels.Remove(pos);
                                        SetDirty();
                                    }
                                }
                                else
                                {
                                    if (voxel.Damage <= 0) activeVoxels.Remove(pos);
                                }
                            }
                            break;
                        default:
                            if (voxel.Damage <= 0) activeVoxels.Remove(pos);
                            break;
                    }
                    if (voxel.Damage > 0 && !PlayerView.usingTool)
                    {
                        if (BlockRandomEvent(new int3(pos.x, pos.y, pos.z), 0.1f))
                        {
                            voxel.Damage -= 1;
                            world.SetVoxel(LocalToWorld(new Vector3Int(pos.x, pos.y, pos.z), ChunkCoord, Size3D), voxel);
                            if (voxel.Damage <= 0) activeVoxels.Remove(pos);
                            SetDirty();
                        }
                    }
                }
                else
                {
                    activeVoxels.Remove(pos);
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
    #endregion

    #region ACCESS VOXELS
    public Voxel GetVoxel(Vector3Int worldPos)
    {
        Vector3Int localPos = WorldToLocal(worldPos, ChunkCoord, Size3D);
        return Voxels[localPos.x, localPos.y, localPos.z];
    }
    public Voxel GetVoxelLocal(Vector3Int localPos)
    {
        return Voxels[localPos.x, localPos.y, localPos.z];
    }

    public Voxel LookupWorldVoxel(Vector3Int localPos)
    {
        return world.LookupVoxelWorld(LocalToWorldPos(localPos));
    }

    public Vector3Int LocalToWorldPos(Vector3Int localPos)
    {
        Vector3Int worldPos = new Vector3Int(localPos.x + ChunkCoord.x * Size3D.x, localPos.y, localPos.z + ChunkCoord.y * Size3D.z);
        return worldPos;
    }
    public Vector3Int WorldToLocalPos(Vector3Int worldPos)
    {
        Vector3Int localPos = worldPos - new Vector3Int(ChunkCoord.x * Size3D.x, 0, ChunkCoord.y * Size3D.z);
        return localPos;
    }
    #endregion

    #region MODIFY VOXELS
    public void SetVoxel(Vector3Int worldPos, BlockID blockID) // DEPRECATED
    {
        Vector3Int localPos = WorldToLocal(worldPos, ChunkCoord, Size3D);
        if (IsPosInGridBounds(localPos, Size3D)) { 
            if (Voxels[localPos.x, localPos.y, localPos.z].BlockID == 0 || true)
            {
                Voxels[localPos.x, localPos.y, localPos.z] = new Voxel(blockID, 0, 0);
                //World().SetVoxel(worldPosition, new VoxelData(blockType, 0, 0));

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
        activeVoxels.Add(localPos);
        activeVoxels.Add(localPos + Vector3Int.left);
        activeVoxels.Add(localPos + Vector3Int.right);
        activeVoxels.Add(localPos + Vector3Int.down);
        activeVoxels.Add(localPos + Vector3Int.up);
        activeVoxels.Add(localPos + Vector3Int.back);
        activeVoxels.Add(localPos + Vector3Int.forward);
    }
    public void SetVoxel(Vector3Int worldPos, Voxel voxelData)
    {
        if (!containedIDs.Contains(voxelData.BlockID) && voxelData.BlockID is not BlockID.Air or BlockID.Invalid)
        {
            containedIDs.Add(voxelData.BlockID);
        }
        Vector3Int localPos = WorldToLocal(worldPos, ChunkCoord, Size3D);
        if (IsPosInGridBounds(localPos, Size3D))
        {
            if (Voxels[localPos.x, localPos.y, localPos.z].BlockID == 0 || true)
            {
                Voxels[localPos.x, localPos.y, localPos.z] = voxelData;
                //World().SetVoxel(worldPos, voxelData);

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
        activeVoxels.Add(localPos);
        activeVoxels.Add(localPos + Vector3Int.left);
        activeVoxels.Add(localPos + Vector3Int.right);
        activeVoxels.Add(localPos + Vector3Int.down);
        activeVoxels.Add(localPos + Vector3Int.up);
        activeVoxels.Add(localPos + Vector3Int.back);
        activeVoxels.Add(localPos + Vector3Int.forward);
    }
    public void AddBlockEntity(BlockEntityActor entity, Vector3Int worldPos, Voxel voxel)
    {
        if (!BlockEntities.ContainsKey(worldPos))
        {
            entity.VoxelData = voxel;
            entity.VoxelPosition = worldPos;
            entity.SetPosition();
            entity.LoadEntity();
            BlockEntities.Add(worldPos, entity);
        }
    }

    private const int DAMAGE_THRESH = 12;
    public void DamageVoxel(Vector3Int worldPos, VoxelHitInfo hitInfo, byte damage)
    {
        Voxel voxel = GetVoxel(worldPos);
        BlockData data = BlockRegistry.LookupBlock(voxel.BlockID);

        VFX().SpawnVFX(VFXType.BLOCK_DMG, hitInfo.hitPos, hitInfo.hitNormal, (int)voxel.BlockID);

        voxel.Damage += damage;
        int toughness = data.Toughness;
        if (voxel.Damage >= toughness)
        {
            if (data.IsBlockEntity)
            {
                if (BlockEntities.ContainsKey(worldPos))
                {
                    Destroy(BlockEntities[worldPos].gameObject);
                    BlockEntities.Remove(worldPos);
                }
            }
            VFX().SpawnVFX(VFXType.BLOCK_BREAK, worldPos, Vector3.zero, (int)voxel.BlockID);

            voxel = new Voxel(BlockID.Air, 0, 0);
            voxel.Shape = 0;
        }
        world.SetVoxel(worldPos, voxel);
    }

    #endregion



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

    /**
    private void DispatchChunkJob()
    {
        verticesResult = new NativeArray<Vector3>(30000,Allocator.TempJob);
        normalsResult = new NativeArray<Vector3>(30000, Allocator.TempJob);
        uvsResult = new NativeArray<Vector2>(30000, Allocator.TempJob);
        trianglesResult = new NativeArray<int>(30000, Allocator.TempJob);
        colorsResult = new NativeArray<Color>(30000, Allocator.TempJob);
        NativeArray<BlockID> voxelInts = new NativeArray<BlockID>(Size3D.x*Size3D.y*Size3D.z, Allocator.TempJob);
        int i = 0;
        foreach (Voxel v in Voxels)
        {
            voxelInts[i] = v.BlockID;
            i++;
        }
        VoxelMesherJob job = new VoxelMesherJob
        {
            Size3D = Size3D,
            Voxels = voxelInts,
            ChunkCoord = ChunkCoord,
            verticesResult = verticesResult,
            normalsResult = normalsResult,
            uvsResult = uvsResult,
            trianglesResult = trianglesResult,
            colorsResult = colorsResult
        };

        handle = job.Schedule();
        jobActive = true;

    }

    private void FinishChunkJob()
    {
        handle.Complete();

        mesh = new Mesh();
        mesh.vertices = verticesResult.ToArray();
        Debug.Log(trianglesResult.Length);
        mesh.triangles = trianglesResult.ToArray();
        mesh.normals = normalsResult.ToArray();
        mesh.colors = colorsResult.ToArray();
        mesh.uv = uvsResult.ToArray();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;


        verticesResult.Dispose();
        normalsResult.Dispose();
        uvsResult.Dispose();
        trianglesResult.Dispose();
        colorsResult.Dispose();

        jobActive = false;
    }
    **/
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
                    if (GetVoxelFaceVisible(pos, VectorDirections[0]))
                    {
                        GreedyFace newFace = new GreedyFace(VectorDirections[0], pos);
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
                            normals.Add(VectorDirections[0]);
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
                    if (GetVoxelFaceVisible(pos, VectorDirections[1]))
                    {
                        GreedyFace newFace = new GreedyFace(VectorDirections[1], pos);
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
                            normals.Add(VectorDirections[1]);
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
                    if (GetVoxelFaceVisible(pos, VectorDirections[3]))
                    {
                        GreedyFace newFace = new GreedyFace(VectorDirections[3], pos);
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
                            normals.Add(VectorDirections[3]);
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
                    if (GetVoxelFaceVisible(pos, VectorDirections[4]))
                    {
                        GreedyFace newFace = new GreedyFace(VectorDirections[4], pos);
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
                            normals.Add(VectorDirections[4]);
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
                    if (GetVoxelFaceVisible(pos, VectorDirections[5]))
                    {
                        GreedyFace newFace = new GreedyFace(VectorDirections[5], pos);
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
                            normals.Add(VectorDirections[5]);
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
    
}


