using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;
using VFolders.Libs;
using VInspector;
using static Perlin;
using static UnityEngine.Analytics.IAnalytic;
using static VoxelHelper;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

public partial class VoxelWorld : MonoBehaviour
{
    public static VoxelWorld Instance { get; private set; }

    public Vector3Int ChunkSize = new Vector3Int(8, 8, 8);
    public Vector3Int WorldSize = new Vector3Int(32, 1, 32);
    public int2 InitialChunks = new int2(4, 4);
    public WorldGenSettings Settings;

    private VoxelData[,,] Voxels;
    private VoxelChunk[,] Chunks;

    public GameObject ChunkPrefab;
    public int Spacing = 8;

    public LayerMask BlockVoxelPlacement;


    private List<Vector3Int> DEBUGTraversalPosList = new List<Vector3Int>();
    private List<Color> DEBUGTraversalColorList;
    private Vector3 DEBUGWorldHitPoint = Vector3.zero;

    public bool Initialized = false;

    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }
    private void Awake()
    {
        SetInstance();
    }


    void Start()
    {
        InitializeWorld();
    }

    public int[] shuffle = new int[6] { 0, 1, 2, 3, 4, 5 };
    public byte orientation = 5;
    [Button(name = "Initialize World", size = 20, color = "black")]
    public void InitializeWorld()
    {
        SetInstance();
        Initialized = true;


        for(int i=transform.childCount-1; i>=0; i--)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(transform.GetChild(i).gameObject);
            }
            else
            {
                GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        Vector3Int worldVoxelSize = new Vector3Int(ChunkSize.x * WorldSize.x, ChunkSize.y * WorldSize.y, ChunkSize.z * WorldSize.z);
        GenerateVoxelsCPU(worldVoxelSize);
       // Debug.Log($"voxels size={worldVoxelSize}");


        Chunks = new VoxelChunk[WorldSize.x, WorldSize.z];
        for (int x = 0; x < InitialChunks.x; x++)
        {
            for (int z = 0; z < InitialChunks.y; z++)
            {
                AddChunk(new int2(x, z));
            }
        }
    }

    private void Loop3D(Action<int, int, int> loopFunction)
    {
        Vector3Int Size3D = new Vector3Int(ChunkSize.x * WorldSize.x, ChunkSize.y * WorldSize.y, ChunkSize.z * WorldSize.z);
        for (int x = 0; x < Size3D.x; x++) {
            for (int z = 0; z < Size3D.z; z++) {
                for (int y = 0; y < Size3D.y; y++) {
                    loopFunction(x, y, z);
                }
            }
        }
    }
    private void Loop3D(Action<int, int, int> loopFunction, Vector3Int origin, Vector3Int size)
    {
        Vector3Int endPoint = origin + size;
        for (int x = origin.x; x < endPoint.x; x++) {
            for (int z = origin.z; z < endPoint.z; z++) {
                for (int y = origin.y; y < endPoint.y; y++) {
                    loopFunction(x, y, z);
                }
            }
        }
    }


    public void AddChunk(int2 pos)
    {
        if (Chunks[pos.x, pos.y] == null)
        {
            VoxelChunk newChunk = Instantiate(ChunkPrefab).GetComponent<VoxelChunk>();
            Chunks[pos.x, pos.y] = newChunk;

            newChunk.Size3D = ChunkSize;
            newChunk.ChunkCoord = pos;
            newChunk.transform.name = $"Chunk_x{pos.x}_z{pos.y}";
            newChunk.transform.position = new Vector3(pos.x * ChunkSize.x, 0, pos.y * ChunkSize.z);
            newChunk.transform.parent = transform;

            MinMaxAABB bounds = new MinMaxAABB(new float3(pos.x * ChunkSize.x, 0f, pos.y * ChunkSize.z), new float3((pos.x + 1) * ChunkSize.x, ChunkSize.y, (pos.y + 1) * ChunkSize.z));

            //Debug.Log($"Bounds = ({bounds.Min}) - ({bounds.Max})");

            VoxelData[,,] chunkData = new VoxelData[ChunkSize.x, ChunkSize.y, ChunkSize.z];
  
            for (int x = 0; x < ChunkSize.x; x++)
            {
                for (int y = 0; y < ChunkSize.y; y++)
                {
                    for (int z = 0; z < ChunkSize.z; z++)
                    {
                        VoxelData v = LookupVoxel(new Vector3Int(x + pos.x * ChunkSize.x, y, z + pos.y * ChunkSize.z));
                        chunkData[x, y, z] = v;
                    }
                }
            }
            newChunk.SetVoxels(chunkData);
            newChunk.InitializeChunk();
        }

        for(int x=0; x<WorldSize.x; x++)
        {
            for (int z=0; z<WorldSize.z;z++)
            {
                if (Chunks[x,z] != null)
                {
                    Chunks[x, z].FindNeighbors();
                }
            }
        }
    }
    public VoxelChunk GetChunk(int2 chunkCoord)
    {
        if (chunkCoord.x >= 0 && chunkCoord.y >= 0 && chunkCoord.x < WorldSize.x &&  chunkCoord.y < WorldSize.z)
        {
            if (Chunks[chunkCoord.x, chunkCoord.y] != null)
                return Chunks[chunkCoord.x, chunkCoord.y];
            else
                return null;
        } 
        else
            return null;       
    }

    private void GenerateVoxelsCPU(Vector3Int Size3D)
    {
        Voxels = new VoxelData[Size3D.x, Size3D.y, Size3D.z];
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int z = 0; z < Size3D.z; z++)
            {
                //float h = Mathf.Cos(z * .1f) * Mathf.Sin(x * 0.1f) * 4f + 8f;

                float noise = Perlin.Fbm(x * Settings.NoiseScale, z * Settings.NoiseScale, Settings.NoiseOctaves);
                float noise2 = Perlin.Fbm(x * Settings.NoiseScale * 0.2f, z * Settings.NoiseScale * 0.2f, Settings.NoiseOctaves);
                float h = noise * Settings.HeightRange + Settings.HeightOffset;
                h = h + (noise2 * Settings.HeightRange * 4);


                for (int y = 0; y < Size3D.y; y++)
                {
                    if (y < h)
                    {
                        Voxels[x, y, z] = new VoxelData(Blocks.DIRT, 0, orientation, 1);
                    }
                    else
                    {
                        Voxels[x, y, z] = new VoxelData(Blocks.AIR, 0, 0, 0);
                    }
                }
            }
        }

        // grass
        /**
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int z = 0; z < Size3D.z; z++)
            {
                for (int y = 0; y < Size3D.y; y++)
                {
                    if (Voxels[x, y, z].ID != Blocks.AIR && y < Size3D.y - 1)
                    {
                        if (Voxels[x, y + 1, z].ID == Blocks.AIR)
                        {
                            Voxels[x, y, z].ID = Blocks.GRASS;
                        }
                    }
                }
            }
        }**/
        Loop3D(GenerateGrassAction);

        Vector3Int[] sphere = GetCoordinateSphere(new Vector3Int(20,12, 20), 10f);
        foreach (Vector3Int p in sphere)
        {
            Voxels[p.x, p.y, p.z] = new VoxelData(Blocks.STONE, 0, 0, 1);
            Voxels[p.x,p.y, p.z].Toughness = 24;
        }
    }

    private void GenerateGrassAction(int x, int y, int z)
    {
        if (Voxels[x, y, z].ID != Blocks.AIR && y < ChunkSize.y - 1)
        {
            if (Voxels[x, y + 1, z].ID == Blocks.AIR)
            {
                Voxels[x, y, z].ID = Blocks.GRASS;
            }
        }
    }

    private Vector3Int[] GetCoordinateCuboid(Vector3Int cornerPos, Vector3Int size)
    {
        List<Vector3Int> points = new List<Vector3Int>();
        for (int x = 0;x < size.x; x++)
        {
            for (int y=0;y < size.y; y++)
            {
                for (int z=0;z < size.z; z++)
                {
                    points.Add(cornerPos + new Vector3Int(x, y, z));
                }
            }
        }
        return points.ToArray();
    }

    private List<Vector3Int> tempSpherePoints;
    private Vector3 tempSphereCenter;
    private float tempSphereRadius;

    private Vector3Int[] GetCoordinateSphere(Vector3Int center, float radius)
    {
        Vector3Int corner = new Vector3Int(radius.CeilToInt(), radius.CeilToInt(), radius.CeilToInt());
        tempSpherePoints = new List<Vector3Int>();
        tempSphereRadius = radius;
        tempSphereCenter = center;
        Loop3D(CoordinateSphereAction, center - corner, corner * 2);

        Vector3Int[] array = tempSpherePoints.ToArray();
        tempSpherePoints.Clear();

        return array;

    }
    private void CoordinateSphereAction(int x, int y, int z)
    {
        Vector3Int p = new Vector3Int(x, y, z);
        float dist = Vector3.Distance(p, tempSphereCenter);
        if (dist < tempSphereRadius)
            tempSpherePoints.Add(p);
    }

    public void DamageVoxel(Vector3 worldPos, Vector3 hitPos, byte damage)
    {
        int2 chunkPos = FindContainingChunk(SnapToGrid(worldPos), ChunkSize);
        VoxelChunk chunk = Chunks[chunkPos.x, chunkPos.y];
        chunk.DamageBlock(SnapToGrid(worldPos), hitPos, damage);
    }

    public void AddVoxel(Vector3 worldPos, VoxelData voxel)
    {
        if (voxel.ID == 0) voxel.BlockShape = 0;
        int2 chunkPos = FindContainingChunk(SnapToGrid(worldPos), ChunkSize);
        VoxelChunk chunk = Chunks[chunkPos.x, chunkPos.y];
        if (Physics.CheckBox(worldPos, Vector3.one * 0.5f, Quaternion.identity, BlockVoxelPlacement.value))
        {
            return;
        }
        chunk.SetBlock(SnapToGrid(worldPos), voxel);
    }

    // from https://web.archive.org/web/20121024081332/www.xnawiki.com/index.php?title=Voxel_traversal
    public VoxelHitInfo VoxelTraversal(Vector3 pos, Vector3 dir, int maxDepth)
    {
        DEBUGTraversalPosList = new List<Vector3Int>();
        DEBUGTraversalColorList = new List<Color>();

        Vector3Int start = SnapToGrid(pos + 0.5f * Vector3.one);
        //  Debug.Log($"pos = ({pos.x}, {pos.y}, {pos.z}) -> start = ({start.x}, {start.y}, {start.z})");
        int stepX = System.Math.Sign(dir.x);
        int stepY = System.Math.Sign(dir.y);
        int stepZ = System.Math.Sign(dir.z);


        // Calculate cell boundaries. When the step (i.e. direction sign) is positive,
        // the next boundary is AFTER our current position, meaning that we have to add 1.
        // Otherwise, it is BEFORE our current position, in which case we add nothing.
        Vector3 voxelBoundary = new Vector3(
            start.x + (stepX > 0 ? 0.5f : -0.5f),
            start.y + (stepY > 0 ? 0.5f : -0.5f),
            start.z + (stepZ > 0 ? 0.5f : -0.5f)
        );

        // tMax : Determine how far we can travel along the ray before we hit a voxel boundary.
        Vector3 tMax = new Vector3(
            (voxelBoundary.x - pos.x) / dir.x,  // Boundary is a plane on the YZ axis
            (voxelBoundary.y - pos.y) / dir.y,  // Boundary is a plane on the XZ axis
            (voxelBoundary.z - pos.z) / dir.z   // Boundary is a plane on the XY axis
        );

        Vector3 tDelta = new Vector3(
            stepX / dir.x,               // Crossing the width of a cell.
            stepY / dir.y,               // Crossing the height of a cell.
            stepZ / dir.z                // Crossing the depth of a cell.
        );
        if (Single.IsNaN(tDelta.x)) tDelta.x = Single.PositiveInfinity;
        if (Single.IsNaN(tDelta.y)) tDelta.y = Single.PositiveInfinity;
        if (Single.IsNaN(tDelta.z)) tDelta.z = Single.PositiveInfinity;
        if (tMax.x == 0f) tMax.x = Single.PositiveInfinity;
        if (tMax.y == 0f) tMax.y = Single.PositiveInfinity;
        if (tMax.z == 0f) tMax.z = Single.PositiveInfinity;

        // For each step, determine which distance to the next voxel boundary is lowest (i.e.
        // which voxel boundary is nearest) and walk that way.
        Vector3Int stepPos = start;
        Vector3Int hitNormal = new Vector3Int(0,0,0);
        float t = 0f;
        for (int i = 0; i < maxDepth; i++)
        {
            DEBUGTraversalPosList.Add(stepPos);

            VoxelData hitVoxel = LookupVoxel(stepPos);
            if (hitVoxel.ID > Blocks.AIR)
            {
                DEBUGTraversalColorList.Add(Color.white);

                VoxelHitInfo hitData = new VoxelHitInfo(true);
                hitData.hitNormal = hitNormal;
                hitData.blockID = hitVoxel.ID;
                hitData.voxelPos = stepPos;
                hitData.hitPos = pos + t * dir;
                
                DEBUGWorldHitPoint = hitData.hitPos;

                return hitData;
            }
            else if (hitVoxel.ID == Blocks.INVALID)
                return new VoxelHitInfo(false);
            else
                DEBUGTraversalColorList.Add(new Color(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z)));

            Vector3 absTMax = new Vector3(Mathf.Abs(tMax.x), Mathf.Abs(tMax.y), Mathf.Abs(tMax.z)); 
            if (absTMax.x < absTMax.y && absTMax.x < absTMax.z) // tMax.X is the lowest, an YZ cell boundary plane is nearest.
            {
                stepPos.x += stepX;
                t = tMax.x;
                tMax.x += tDelta.x;
                hitNormal = new Vector3Int(-stepX, 0, 0);
            }
            else if (absTMax.y < absTMax.z)               // tMax.Y is the lowest, an XZ cell boundary plane is nearest.
            {
                stepPos.y += stepY;
                t = tMax.y;
                tMax.y += tDelta.y;
                hitNormal = new Vector3Int(0, -stepY, 0);
            }
            else                                    // tMax.Z is the lowest, an XY cell boundary plane is nearest.
            {
                stepPos.z += stepZ;
                t = tMax.z;
                tMax.z += tDelta.z;
                hitNormal = new Vector3Int(0, 0, -stepZ);
            }
        }
        return new VoxelHitInfo(false);
    }

    public VoxelData LookupVoxel(Vector3Int p)
    {
        if (CheckWorldBounds(p))
            return Voxels[p.x, p.y, p.z];
        else
            return new VoxelData(0, 0, 0); 
    }

    public void SetVoxel(Vector3Int p, VoxelData newVoxel)
    {
        if (CheckWorldBounds(p))
            Voxels[p.x,p.y,p.z] = newVoxel;
    }

    public bool CheckWorldBounds(Vector3Int p)
    {
        return !(p.x < 0 || p.y < 0 || p.z < 0 || p.x >= ChunkSize.x * WorldSize.x || p.y >= ChunkSize.y * WorldSize.y || p.z >= ChunkSize.z * WorldSize.z);
    }

    public void Explode(Vector3Int center, float radius)
    {
        Vector3Int[] sphere = GetCoordinateSphere(center,radius);
        foreach (Vector3Int p in sphere)
        {
            int2 chunkCoord = FindContainingChunk(p, ChunkSize);
            Chunks[chunkCoord.x, chunkCoord.y].SetBlock(p, new VoxelData(Blocks.AIR,0,0,0));
        }
    }


    

    private Vector3Int WorldPosToVoxel(Vector3 worldPos)
    {
        Vector3Int result = new Vector3Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y), Mathf.RoundToInt(worldPos.z));
        //Vector3Int result = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), Mathf.FloorToInt(worldPos.z));

        return result;
    }
    private void OnDrawGizmos()
    { 
        for (int i = 0; i < DEBUGTraversalPosList.Count; i++)
        {
            //Gizmos.color = DEBUGTraversalColorList[i];
            Gizmos.color = new Color((float)i / DEBUGTraversalPosList.Count, 0f, 0f);
            if (i >= DEBUGTraversalPosList.Count - 1) Gizmos.color = Color.white;
            Gizmos.DrawCube(DEBUGTraversalPosList[i], Vector3.one);
        }

        if (DEBUGWorldHitPoint != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(DEBUGWorldHitPoint, 0.2f);
        }

    }
}

