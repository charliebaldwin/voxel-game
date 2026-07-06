using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Timeline;
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

    public int DEBUG_CoCount = 0;

    [Foldout("World Settings")]
    public Vector3Int ChunkSize = new Vector3Int(8, 8, 8);
    [SerializeField] private Vector3Int WorldSize = new Vector3Int(32, 1, 32);
    [SerializeField] private int2 InitialChunks = new int2(4, 4);
    [SerializeField] private int2 InitialLoadedChunks = new int2(2, 2);
    [SerializeField] private WorldGenSettings GenerationSettings;
    [SerializeField] private GameObject ChunkPrefab;
    [SerializeField] private GameObject BlockEntityPrefab;
    [EndFoldout]

    [Foldout("Data")]
    [ShowInInspector] private Voxel[,,] Voxels;
    [ShowInInspector] private VoxelChunk[,] Chunks;
    private List<VoxelChunk> loadedChunks;
    public VoxelStructure tempStructure;
    [EndFoldout]

    public bool Initialized = false;

    public LayerMask BlockPlacementMask;

    public float BlockUpdateDelay = 0.05f;
    private IEnumerator blockUpdate_co;
    private IEnumerator chunkLoad_co;

    [Foldout("Debug")]
    private List<Vector3Int> DEBUGTraversalPosList = new List<Vector3Int>();
    private List<Color> DEBUGTraversalColorList;
    private Vector3 DEBUGWorldHitPoint = Vector3.zero;
    public int[] shuffle = new int[6] { 0, 1, 2, 3, 4, 5 };
    public byte orientation = 5;
    [EndFoldout]

    static readonly ProfilerMarker marker = new ProfilerMarker("Voxel Generation");


    private void Awake()
    {
        //SetInstance();

    }

    void Start()
    {
        InitializeWorld();
        blockUpdate_co = BlockUpdateCO();
        StartCoroutine(blockUpdate_co);
    }

    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    [Button(name = "Initialize World", size = 20, color = "black")]
    public void InitializeWorld()
    {
        SetInstance();
        Initialized = true;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                GameObject.Destroy(transform.GetChild(i).gameObject);
            else
                GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Vector3Int worldVoxelSize = new Vector3Int(ChunkSize.x * WorldSize.x, ChunkSize.y * WorldSize.y, ChunkSize.z * WorldSize.z);
        GenerateVoxelsCPU(worldVoxelSize);
        // Debug.Log($"voxels size={worldVoxelSize}");


        loadedChunks = new List<VoxelChunk>();
        Chunks = new VoxelChunk[WorldSize.x, WorldSize.z];
        for (int x = 0; x < WorldSize.x; x++)
        {
            for (int z = 0; z < WorldSize.z; z++)
            {
                AddChunk(new Vector3Int(x, 0, z));
            }
        }
    }

    private void FixedUpdate()
    {
        if (chunksToLoad.Count > 0 && chunkLoad_co == null)
        {
            chunkLoad_co = LoadChunkCO();
            StartCoroutine(chunkLoad_co);
            //LoadLastQueuedChunk();
        }
    }

    #region CHUNK MANAGEMENT
    public void AddChunk(Vector3Int pos)
    {
        if (Chunks[pos.x, pos.z] == null)
        {
            VoxelChunk newChunk = Instantiate(ChunkPrefab).GetComponent<VoxelChunk>();
            Chunks[pos.x, pos.z] = newChunk;
            newChunk.transform.name = $"Chunk_x{pos.x}_z{pos.z}";
            newChunk.transform.position = new Vector3(pos.x * ChunkSize.x, 0, pos.z * ChunkSize.z);
            newChunk.transform.parent = transform;

            MinMaxAABB bounds = new MinMaxAABB(new float3(pos.x * ChunkSize.x, 0f, pos.z * ChunkSize.z), new float3((pos.x + 1) * ChunkSize.x, ChunkSize.y, (pos.z + 1) * ChunkSize.z));

            Voxel[,,] chunkData = new Voxel[ChunkSize.x, ChunkSize.y, ChunkSize.z];

            for (int x = 0; x < ChunkSize.x; x++)
            {
                for (int y = 0; y < ChunkSize.y; y++)
                {
                    for (int z = 0; z < ChunkSize.z; z++)
                    {
                        //Voxel v = LookupVoxelWorld(new Vector3Int(x + pos.x * ChunkSize.x, y, z + pos.z * ChunkSize.z));
                        Voxel v = Voxels[x + pos.x * ChunkSize.x, y, z + pos.z * ChunkSize.z];
                        chunkData[x, y, z] = v;
                    }
                }
            }
            newChunk.InitializeChunk(ChunkSize, pos);
            newChunk.FillVoxelData(chunkData);
            if (pos.x <= InitialLoadedChunks.x && pos.z <= InitialLoadedChunks.y)
                LoadChunk(pos);
        }

        for (int x = 0; x < WorldSize.x; x++) {
            for (int z = 0; z < WorldSize.z; z++) {
                if (Chunks[x, z] != null)
                    Chunks[x, z].FindNeighbors();
            }
        }
    }

    private List<VoxelChunk> chunksToLoad = new List<VoxelChunk>();
    public void EnqueueChunk (Vector3Int coord)
    {
        if (IsChunkCoordInBounds(coord))
        {
            VoxelChunk c = Chunks[coord.x, coord.z];
            if (c != null && !c.Loaded && !loadedChunks.Contains(c) && !chunksToLoad.Contains(c))
                chunksToLoad.Add(Chunks[coord.x, coord.z]);
        }
        else
            Debug.Log("chunk outside bounds");
    }
    public void LoadLastQueuedChunk()
    {
        VoxelChunk chunk = chunksToLoad.RemoveLast(); 
        loadedChunks.Add(chunk.LoadChunk());
    }
    private IEnumerator LoadChunkCO()
    {
        Debug.Log($"starting chunk loading co, count={chunksToLoad.Count}");
        List<VoxelChunk> chunksToLoadCopy = chunksToLoad;
        for (int i = 0; i < chunksToLoadCopy.Count; i++)
        {
            loadedChunks.Add(chunksToLoadCopy[i].LoadChunk());
            yield return new WaitForSeconds(0.1f);
        }
        chunksToLoad = new List<VoxelChunk>();
        chunkLoad_co = null;
    }

    public void LoadChunk(Vector3Int coord)
    {
        if (IsChunkCoordInBounds(coord)) {
            if (Chunks[coord.x, coord.z] != null) 
                if (!Chunks[coord.x, coord.z].Loaded)
                    loadedChunks.Add(Chunks[coord.x, coord.z].LoadChunk());
        } else
        {
            Debug.Log("chunk outside bounds");
        }
    }
    public void LoadChunkSpread(Vector3Int coord, int spreadDist)
    {

        if (IsChunkCoordInBounds(coord))
        {
            //LoadChunk(coord);
            EnqueueChunk(coord);
            //Debug.Log($"spreading into {coord}");
            if (spreadDist > 0)
            {
                spreadDist -= 1;
                LoadChunkSpread(coord + Vector3Int.left, spreadDist);
                LoadChunkSpread(coord + Vector3Int.right, spreadDist);
                LoadChunkSpread(coord + Vector3Int.back, spreadDist);
                LoadChunkSpread(coord + Vector3Int.forward, spreadDist);
            }
        }
    }

    public void UnloadChunk(Vector3Int coord)
    {
        if (IsChunkCoordInBounds(coord)) {
            loadedChunks.Remove(Chunks[coord.x, coord.z].UnloadChunk());
        } 
    }

    public void UnloadDistantChunks(Vector3Int centerCoord, int dist)
    {
        List<VoxelChunk> chunksToUnload = new List<VoxelChunk>();
        foreach (VoxelChunk c in loadedChunks)
        {
            Vector2 center = new Vector2(centerCoord.x, centerCoord.z);
            Vector2 chunkPos = new Vector2(c.ChunkCoord.x, c.ChunkCoord.z);
            if (Vector2.Distance(center, chunkPos) > dist)
            {
                chunksToUnload.Add(c);
            }
        }
        foreach (VoxelChunk c in chunksToUnload)
        {
            UnloadChunk(c.ChunkCoord);
        }
        chunksToUnload.Clear();
    }
    public VoxelChunk GetContainingChunk(Vector3Int worldPos)
    {
        Vector3Int chunkCoord = new Vector3Int(Mathf.FloorToInt(worldPos.x / ChunkSize.x), 0, Mathf.FloorToInt(worldPos.z / ChunkSize.z));
        if (IsChunkCoordInBounds(chunkCoord))
        {
            return Chunks[chunkCoord.x, chunkCoord.z];
        }
        return null;
    }
    public VoxelChunk GetChunk(Vector3Int chunkCoord)
    {
        //if (chunkCoord.x >= 0 && chunkCoord.z >= 0 && chunkCoord.x < WorldSize.x && chunkCoord.z < WorldSize.z)
        if (IsChunkCoordInBounds(chunkCoord))
        {
            if (Chunks[chunkCoord.x, chunkCoord.z] != null)
                return Chunks[chunkCoord.x, chunkCoord.z];
            else
                return null;
        }
        else
            return null;
    }
    #endregion

    private void Loop3D(Action<int, int, int> loopFunction)
    {
        Vector3Int Size3D = new Vector3Int(ChunkSize.x * WorldSize.x, ChunkSize.y * WorldSize.y, ChunkSize.z * WorldSize.z);
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int z = 0; z < Size3D.z; z++)
            {
                for (int y = 0; y < Size3D.y; y++)
                {
                    loopFunction(x, y, z);
                }
            }
        }
    }
    private void Loop3D(Action<int, int, int> loopFunction, Vector3Int origin, Vector3Int size)
    {
        Vector3Int endPoint = origin + size;
        for (int x = origin.x; x < endPoint.x; x++)
        {
            for (int z = origin.z; z < endPoint.z; z++)
            {
                for (int y = origin.y; y < endPoint.y; y++)
                {
                    loopFunction(x, y, z);
                }
            }
        }
    }

    private void GenerateVoxelsCPU(Vector3Int Size3D)
    {
        marker.Begin();

        Voxels = new Voxel[Size3D.x, Size3D.y, Size3D.z];
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int z = 0; z < Size3D.z; z++)
            {
                //float h = Mathf.Cos(z * .1f) * Mathf.Sin(x * 0.1f) * 4f + 8f;

                float noise = Perlin.Fbm(x * GenerationSettings.NoiseScale, z * GenerationSettings.NoiseScale, GenerationSettings.NoiseOctaves);
                float noise2 = Perlin.Fbm(x * GenerationSettings.NoiseScale * 0.2f, z * GenerationSettings.NoiseScale * 0.2f, GenerationSettings.NoiseOctaves);
                float h = noise * GenerationSettings.HeightRange + GenerationSettings.HeightOffset;
                h = h + (noise2 * GenerationSettings.HeightRange * 4);

                for (int y = 0; y < Size3D.y; y++)
                {
                    if (y < h)
                        Voxels[x, y, z] = new Voxel(BlockID.Stone, 0, orientation, BlockShape.Solid);
                    else
                        Voxels[x, y, z] = new Voxel(BlockID.Air, 0, 0, BlockShape.Empty);
                }
            }
        }

        Loop3D(GenerateGrassAction);

        Vector3Int[] sphere = GetCoordinateSphere(new Vector3Int(20, 12, 20), 10f);
        foreach (Vector3Int p in sphere)
        {
            Voxels[p.x, p.y, p.z] = new Voxel(BlockID.Stone, 0, 0, BlockShape.Solid);
            Voxels[p.x, p.y, p.z].Toughness = 24;
        }

        LoadStructure(tempStructure);

        marker.End();
    }

    private void GenerateGrassAction(int x, int y, int z)
    {
        if (Voxels[x, y, z].BlockID == BlockID.Stone && y < ChunkSize.y - 1) {
            if (Voxels[x, y + 1, z].BlockID == BlockID.Air) {
                Voxels[x, y, z].BlockID = BlockID.Grass;
            }
        }
        if (Voxels[x, y, z].BlockID == BlockID.Stone && y < ChunkSize.y - 2) {
            if (Voxels[x, y + 2, z].BlockID == BlockID.Air) {
                Voxels[x, y, z].BlockID = BlockID.Dirt;
            }
        }
        if (Voxels[x, y, z].BlockID == BlockID.Stone && y < ChunkSize.y - 3) {
            if (Voxels[x, y + 3, z].BlockID == BlockID.Air) {
                Voxels[x, y, z].BlockID = BlockID.Dirt;
            }
        }
        if (Voxels[x, y, z].BlockID == BlockID.Air && y > 0)
        {
            if (Voxels[x, y - 1, z].BlockID == BlockID.Grass)
            {
                float noise = Perlin.Fbm(x * 0.5f, z * 0.5f, 2);
                if (noise > 0.3f)
                {
                    Voxels[x, y, z] = new Voxel(BlockID.Log, 0, 0);
                    Voxels[x, y+1, z] = new Voxel(BlockID.Log, 0, 0); // dangerous
                    Voxels[x, y + 2, z] = new Voxel(BlockID.Log, 0, 0);
                    Voxels[x, y + 3, z] = new Voxel(BlockID.Leaves, 0, 0);
                    //Voxels[x, y + 3, z+1] = new Voxel(BlockID.Leaves, 0, 0);
                    //Voxels[x, y + 3, z-1] = new Voxel(BlockID.Leaves, 0, 0);
                    //Voxels[x+1, y + 3, z] = new Voxel(BlockID.Leaves, 0, 0);
                    //Voxels[x-1, y + 3, z] = new Voxel(BlockID.Leaves, 0, 0);

                }
            }
        }
    }

    private void LoadStructure(VoxelStructure structure)
    {
        Dictionary<Vector3Int, Voxel> structureVoxels = structure.GetStructureVoxels();
        foreach(KeyValuePair<Vector3Int, Voxel> v in structureVoxels)
        {
            Voxels[v.Key.x, v.Key.y, v.Key.z] = v.Value;
        }
        Destroy(structure.gameObject);
    }

    private Vector3Int[] GetCoordinateCuboid(Vector3Int cornerPos, Vector3Int size)
    {
        List<Vector3Int> points = new List<Vector3Int>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
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


    private IEnumerator BlockUpdateCO()
    {
        //Debug.Log($"world is starting chunk update, there are {loadedChunks.Count} loaded chunks");
        List<VoxelChunk> tempLoadedChunks = loadedChunks;
        for (int i = 0; i < tempLoadedChunks.Count; i++) {
            tempLoadedChunks[i].BlockUpdate();
            //Debug.Log($"chunk {loadedChunks[i].ChunkCoord} updated");
            yield return new WaitForSeconds(BlockUpdateDelay);
        }
        //Debug.Log($"world chunk update finished");
        //StopCoroutine(blockUpdate_co);
        blockUpdate_co = BlockUpdateCO();
        StartCoroutine(blockUpdate_co);
    }


    public Voxel LookupVoxelWorld(Vector3Int worldPos)
    {
        Vector3Int chunkPos = FindContainingChunk(SnapToGrid(worldPos), ChunkSize);
        if (CheckWorldBounds(worldPos))
        {
            VoxelChunk chunk = GetContainingChunk(worldPos);
            if (chunk != null)
            {
                return chunk.GetVoxel(worldPos);
            }
            //return Voxels[worldPos.x, worldPos.y, worldPos.z];
        }
        return new Voxel(BlockID.Invalid, 0, 0);
    }
    public void DamageVoxel(Vector3 worldPos, VoxelHitInfo hitInfo, byte damage)
    {
        Vector3Int chunkPos = FindContainingChunk(SnapToGrid(worldPos), ChunkSize);
        VoxelChunk chunk = Chunks[chunkPos.x, chunkPos.z];
        chunk.DamageVoxel(SnapToGrid(worldPos), hitInfo, damage);
    }
    public void AddVoxel(Vector3 worldPos, Voxel voxel)
    {
        if (voxel.BlockID == 0) voxel.Shape = 0;

        Vector3Int chunkPos = FindContainingChunk(SnapToGrid(worldPos), ChunkSize);
        VoxelChunk chunk = Chunks[chunkPos.x, chunkPos.z];
        if (Physics.CheckBox(worldPos, Vector3.one * 0.5f, Quaternion.identity, BlockPlacementMask.value))
        {
            return;
        }
        SetVoxel(SnapToGrid(worldPos), voxel);
    }
    public void SetVoxel(Vector3Int worldPos, Voxel newVoxel)
    {
        if (CheckWorldBounds(worldPos))
        {

            BlockData data = BlockRegistry.LookupBlock(newVoxel.BlockID);

            Vector3Int chunkPos = FindContainingChunk(SnapToGrid(worldPos), ChunkSize);
            VoxelChunk chunk = Chunks[chunkPos.x, chunkPos.z];
            
            //Debug.Log($"World is placing block, id={data.BlockID}, isEntity={data.IsBlockEntity}");
            
            if (data.IsBlockEntity)
            {
                BlockEntityActor entity = Instantiate(BlockEntityPrefab, chunk.transform).GetComponent<BlockEntityActor>();
                entity.Data = BlockRegistry.LookupBlockEntity(data.BlockID);
                newVoxel.Shape = BlockShape.BlockEntity;
                chunk.AddBlockEntity(entity, worldPos);
            }

            Voxels[worldPos.x, worldPos.y, worldPos.z] = newVoxel;
            chunk.SetVoxel(worldPos, newVoxel);
        }
    }


    public VoxelHitInfo VoxelTraversal(Vector3 pos, Vector3 dir, int maxDepth)
    {
        // from https://web.archive.org/web/20121024081332/www.xnawiki.com/index.php?title=Voxel_traversal
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
        bool didHit = false;

        // LOOP
        for (int i = 0; i < maxDepth; i++)
        {
            DEBUGTraversalPosList.Add(stepPos);

            Voxel hitVoxel = LookupVoxelWorld(stepPos);

            // hit full block
            if (hitVoxel.Shape == BlockShape.Solid)
            {
                didHit = true;
            }

            // hit half slab
            else if (hitVoxel.Shape == BlockShape.HalfSlab)
            {
                Vector3 localHitPos = (pos + t * dir) - stepPos;

                Vector3 absTMaxSlab = new Vector3(Mathf.Abs(tMax.x), Mathf.Abs(tMax.y), Mathf.Abs(tMax.z));
                float slope = Mathf.Max(tMax.y - tMax.x, tMax.y - tMax.z);
                Debug.Log($"tmax.y={tMax.y}, stepY={stepY}, dir.y={dir.y}, localHitPos.y={localHitPos.y}");
                if (localHitPos.y < 0)
                {
                    didHit = true;
                }
                else if(localHitPos.y + dir.y < 0)
                {
                    hitNormal = Vector3Int.up;
                    didHit = true;
                }

            }

            // HIT
            if (didHit)
            {
                DEBUGTraversalColorList.Add(Color.white);

                VoxelHitInfo hitData = new VoxelHitInfo(true);
                hitData.voxel = hitVoxel;
                hitData.hitNormal = hitNormal;
                hitData.blockID = hitVoxel.BlockID;
                hitData.voxelPos = stepPos;
                hitData.hitPos = pos + t * dir;
                hitData.chunkPos = FindContainingChunk(stepPos, ChunkSize);
                hitData.localVoxelPos = WorldToLocal(stepPos, hitData.chunkPos, ChunkSize);
                
                DEBUGWorldHitPoint = hitData.hitPos;
                DebugPanel.LastHitInfo = hitData;

                return hitData;
        
            }
            else if (hitVoxel.BlockID == BlockID.Invalid)
                return new VoxelHitInfo(false);
            else
                DEBUGTraversalColorList.Add(new Color(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z)));
            

            // going through air
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


    public bool CheckWorldBounds(Vector3Int p)
    {
        return !(p.x < 0 || p.y < 0 || p.z < 0 || p.x >= ChunkSize.x * WorldSize.x || p.y >= ChunkSize.y * WorldSize.y || p.z >= ChunkSize.z * WorldSize.z);
    }
    public bool IsChunkCoordInBounds(Vector3Int coord)
    {
        return coord.x >= 0 && coord.z >= 0 && coord.x < WorldSize.x - 1 && coord.z < WorldSize.z - 1;
    }

    public void Explode(Vector3Int center, float radius)
    {
        Vector3Int[] sphere = GetCoordinateSphere(center,radius);
        foreach (Vector3Int p in sphere)
        {
            //int2 chunkCoord = FindContainingChunk(p, ChunkSize);
            //Chunks[chunkCoord.x, chunkCoord.y].SetVoxel(p, new VoxelData(Blocks.AIR,0,0,0));
            SetVoxel(p, new Voxel(BlockID.Air, 0, 0, 0));
        }
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

