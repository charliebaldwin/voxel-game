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

    private VoxelData[,,] Voxels = new VoxelData[1, 1, 1];

    private Vector3Int Size3D = new Vector3Int(16,32,16);
    [ShowInInspector] public bool Loaded { get; private set; } = false;
    public int2 ChunkCoord { get; private set; }

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





    private void Awake()
    {
        //Voxels = new VoxelData[Size3D.x, Size3D.y, Size3D.z];
    }
    private void FixedUpdate()
    {
        //BlockUpdate();
        //Loop3D(BlockUpdateAction);
    }
    private void LateUpdate()
    {

        if (meshDirty && Loaded)
        {
            if (useGreedy) GreedyMesh();
            else ComputeMeshCPU();

            meshDirty = false;
        }
    }
    private void OnValidate()
    {
        
    }

    public void InitializeChunk(Vector3Int size, int2 coord)
    {
        Size3D = size;
        Voxels = new VoxelData[Size3D.x, Size3D.y, Size3D.z];
        ChunkCoord = coord;
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider.enabled = false;
        meshRenderer.enabled = false;
        gameObject.SetActive(false);
    }
    public VoxelChunk LoadChunk()
    {
        //Debug.Log($"loaded {name}");
        gameObject.SetActive(true);

        Loaded = true;

        meshRenderer.enabled = true;
        meshFilter.sharedMesh = new Mesh();

        if (useGreedy)
            GreedyMesh();
        else
            ComputeMeshCPU();

        meshCollider.enabled = true;
        return this;
    }
    public VoxelChunk UnloadChunk()
    {
        Loaded = false;
        meshCollider.enabled = false;
        meshRenderer.enabled = false;
        meshFilter.sharedMesh.Clear();
        gameObject.SetActive(false);


        return this;
    }
    public void FillVoxelData(VoxelData[,,] newVoxels)
    {
        Voxels = newVoxels;
    }

    public void FindNeighbors()
    {
        if (neighborNX == null)
            neighborNX = World().GetChunk(ChunkCoord + new int2(-1, 0));
        if (neighborPX == null)
            neighborPX = World().GetChunk(ChunkCoord + new int2(1, 0));
        if (neighborNZ == null)
            neighborNZ = World().GetChunk(ChunkCoord + new int2(0, -1));
        if (neighborPZ == null)
            neighborPZ = World().GetChunk(ChunkCoord + new int2(0, 1));
    }

    private void Loop3D(Action<int, int, int> loopFunction)
    {
        for (int z = 0; z < Size3D.z; z++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int x = 0; x < Size3D.x; x++)
                {
                    loopFunction(x, y, z);
                }
            }
        }
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
        VoxelData vox = Voxels[x, y, z];
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
        VoxelData thisVoxel = World().LookupVoxel(LocalToWorld(pos, ChunkCoord, Size3D));
        VoxelData neighborVoxel = World().LookupVoxel(LocalToWorld(pos + faceDirection, ChunkCoord, Size3D));
        return neighborVoxel.ID == 0 && thisVoxel.ID > 0;
    }

    
    public void SetDirty()
    {
        meshDirty = true;
    }
    public void BlockUpdate()
    {
        Loop3D(BlockUpdateAction);
    }
    private void BlockUpdateAction(int  x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        VoxelData voxel = Voxels[x, y, z];
        int voxelID = voxel.ID;
        switch (voxelID)
        {
            case (Blocks.GRASS):
                if (y < Size3D.y - 1)
                {
                    if (Blocks.IsSolid(Voxels[x, y + 1, z]))
                    {
                        Voxels[x, y, z].ID = Blocks.DIRT;
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
                        if (BlockRandomEvent(new int3(x, y, z), 0.01f))
                        {
                            Voxels[x, y, z].ID = Blocks.GRASS;
                            SetDirty();
                        }
                    }
                }
                break;
        }

        if (Voxels[x, y, z].Damage > 0 && !PlayerView.usingTool)
        {
            if (BlockRandomEvent(new int3(x, y, z), 0.003f))
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






    private const int DAMAGE_THRESH = 12;
    public void DamageVoxel(Vector3Int worldPos, VoxelHitInfo hitInfo, byte damage)
    {
        Vector3Int localPos = WorldToLocal(worldPos, ChunkCoord, Size3D);
        VoxelData voxel = LookupVoxel(localPos);

        voxel.Damage += damage;

        //GameObject hitVFX = Instantiate(blockHitVFXPrefab, hitInfo.hitPos, Quaternion.identity);
        //hitVFX.GetComponent<VFXObject>().InitVFX(voxel.ID, 0.5f, hitInfo.hitNormal);
        VFX().SpawnVFX(VFXType.BLOCK_DMG, hitInfo.hitPos, hitInfo.hitNormal, voxel.ID);

        if (voxel.Damage >= voxel.Toughness)
        {
            //GameObject breakVFX = Instantiate(blockBreakVFXPrefab, worldPos, Quaternion.identity);
            //breakVFX.GetComponent<VFXObject>().InitVFX(voxel.ID, 1f);
            VFX().SpawnVFX(VFXType.BLOCK_BREAK, worldPos, Vector3.zero, voxel.ID);

            voxel = new VoxelData(Blocks.AIR, 0, 0);
            voxel.BlockShape = 0;
        }
        World().SetVoxel(worldPos, voxel);
    }

    public void SetVoxel(Vector3Int worldPos, int blockType)
    {
        Vector3Int localPos = WorldToLocal(worldPos, ChunkCoord, Size3D);
        if (IsPosInGridBounds(localPos, Size3D)) { 
            if (Voxels[localPos.x, localPos.y, localPos.z].ID == 0 || true)
            {
                Voxels[localPos.x, localPos.y, localPos.z] = new VoxelData(blockType, 0, 0);
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
    public void SetVoxel(Vector3Int worldPos, VoxelData voxelData)
    {
       
        Vector3Int localPos = WorldToLocal(worldPos, ChunkCoord, Size3D);
        if (IsPosInGridBounds(localPos, Size3D))
        {
            if (Voxels[localPos.x, localPos.y, localPos.z].ID == 0 || true)
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






    public VoxelData LookupVoxel(Vector3Int localPos)
    {
        return World().LookupVoxel(LocalToWorld(localPos, ChunkCoord, Size3D));
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


