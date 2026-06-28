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
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using VInspector;
using static Perlin;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
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
    private bool jobActive = false;
    public NativeArray<Vector3> verticesResult;
    public NativeArray<Vector3> normalsResult;
    public NativeArray<Vector2> uvsResult;
    public NativeArray<int> trianglesResult;
    public NativeArray<Color> colorsResult;


    private void Awake()
    {
        //Voxels = new VoxelData[Size3D.x, Size3D.y, Size3D.z];
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
        if (jobActive)
        {
            if (handle.IsCompleted)
            {
                FinishChunkJob();
            }
        }

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
    }

    public void FindNeighbors()
    {
        if (neighborNX == null)
            neighborNX = World().GetChunk(ChunkCoord + Vector3Int.left);
        if (neighborPX == null)
            neighborPX = World().GetChunk(ChunkCoord + Vector3Int.right);
        if (neighborNZ == null)
            neighborNZ = World().GetChunk(ChunkCoord + Vector3Int.back);
        if (neighborPZ == null)
            neighborPZ = World().GetChunk(ChunkCoord + Vector3Int.forward);
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
    private List<Vector2> uvs = new List<Vector2>();
    private List<int>     triangles = new List<int>();
    private List<Color>   colors = new List<Color>();
    private int           t = 0;
    private void ComputeMeshCPU()
    {
        vertices = new List<Vector3>();
        normals = new List<Vector3>();
        uvs = new List<Vector2>();
        triangles = new List<int>();
        colors = new List<Color>();
        t = 0;
        /**
        // get model data for each voxel
        for (int z = 0; z < Size3D.z; z++) {
            for (int y = 0; y < Size3D.y; y++) {
                for (int x = 0; x < Size3D.x; x++) {

                    VoxelData vox = voxels[x, y, z];
                    if ((BlockShapes)vox.BlockShape != BlockShapes.EMPTY)
                    {
                        Vector3 pos = new Vector3(x, y, z);

                        int[] neighbors = new int[6] { 0, 0, 0, 0, 0, 0 };
                        for (int n = 0; n < 6; n++)
                            neighbors[n] = World().LookupVoxel(LocalToWorld(new Vector3Int(x, y, z) + Directions[n], ChunkCoord, Size3D)).BlockShape;

                        BlockModel model = new BlockModel(pos, t, neighbors, vox);
                        foreach (Vector3 v in model.vertices) vertices.Add(v);
                        foreach (Vector3 n in model.normals) normals.Add(n);
                        foreach (Vector2 uv in model.uvs) uvs.Add(uv);
                        foreach (Color c in model.colors) colors.Add(c);
                        foreach (int tri in model.triangles) triangles.Add(tri);
                        t = model.lastT;
                    }
                }
            }
        }
        **/

        Loop3D(ComputeMeshAction);

        // send all data for chunk into mesh
        mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.colors = colors.ToArray();
        mesh.uv = uvs.ToArray();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        triangles.Clear();
        colors.Clear();
    }

    private void ComputeMeshAction(int x, int y, int z)
    {
        Voxel vox = Voxels[x, y, z];
        if ((BlockShape)vox.Shape != BlockShape.Empty && !BlockRegistry.LookupBlock(vox.BlockID).IsBlockEntity)
        {
            Vector3 pos = new Vector3(x, y, z);

            int[] neighbors = new int[6] { 0, 0, 0, 0, 0, 0 };
            for (int n = 0; n < 6; n++)
            {
                Vector3Int dir = OrthoDirs[n].AlignYZ(vox.UpAxis, vox.ForwardAxis).ToVector();
                BlockShape neighborShape = World().LookupVoxel(LocalToWorld(new Vector3Int(x, y, z) + dir, ChunkCoord, Size3D)).Shape;
                neighbors[n] = (neighborShape == BlockShape.Solid) ? 1 : 0;
                //neighbors[n] = (int)World().LookupVoxel(LocalToWorld(new Vector3Int(x, y, z) + Directions[n], ChunkCoord, Size3D)).Shape;
            }
            BlockModel model = new BlockModel(pos, t, neighbors, vox);
            foreach (Vector3 v in model.vertices) vertices.Add(v);
            foreach (Vector3 n in model.normals) normals.Add(n);
            foreach (Vector2 uv in model.uvs) uvs.Add(uv);
            foreach (Color c in model.colors) colors.Add(c);
            foreach (int tri in model.triangles) triangles.Add(tri);
            t = model.lastT;
        }
    }

    private bool GetVoxelFaceVisible(Vector3Int pos, Vector3Int faceDirection)
    {
        Voxel thisVoxel = World().LookupVoxel(LocalToWorld(pos, ChunkCoord, Size3D));
        Voxel neighborVoxel = World().LookupVoxel(LocalToWorld(pos + faceDirection, ChunkCoord, Size3D));
        return neighborVoxel.BlockID == 0 && thisVoxel.BlockID > 0;
    }
    #endregion

    #region BLOCK UPDATE
    public void BlockUpdate()
    {
        Loop3D(BlockUpdateAction);
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
                        World().SetVoxel(LocalToWorld(pos, ChunkCoord, Size3D), new Voxel(BlockID.Dirt, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));
                        meshDirty = true;
                    }
                }
                break;
            case (BlockID.Dirt):
                if (y < Size3D.y - 1)
                {
                    Voxel upVoxel = World().LookupVoxel(LocalToWorldPos((pos + new Vector3Int(0, 1, 0))));
                    if (upVoxel.BlockID == BlockID.Air)
                    {
                        // grow into dirt with random chance
                        if (BlockRandomEvent(new int3(x, y, z), 0.04f))
                        {
                            World().SetVoxel(LocalToWorld(pos, ChunkCoord, Size3D), new Voxel(BlockID.Grass, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));

                            SetDirty();
                        }
                    }
                }
                break;
        }

        if (Voxels[x, y, z].Damage > 0 && !PlayerView.usingTool)
        {
            if (BlockRandomEvent(new int3(x, y, z), 0.5f))
            {
                voxel.Damage -= 1;
                //SetBlock(LocalToWorld(new Vector3Int(x, y, z), ChunkCoord, Size3D), voxel);
                World().SetVoxel(LocalToWorld(new Vector3Int(x, y, z), ChunkCoord, Size3D), voxel);
                SetDirty();
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

    public Voxel LookupWorldVoxel(Vector3Int localPos)
    {
        return World().LookupVoxel(LocalToWorldPos(localPos));
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
    public void SetVoxel(Vector3Int worldPos, BlockID blockID)
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
    }
    public void SetVoxel(Vector3Int worldPos, Voxel voxelData)
    {
       
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
    }
    public void AddBlockEntity(BlockEntityActor entity, Vector3Int worldPos)
    {
        if (!BlockEntities.ContainsKey(worldPos))
        {
            entity.SetPosition(worldPos);
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
        World().SetVoxel(worldPos, voxel);
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

}


