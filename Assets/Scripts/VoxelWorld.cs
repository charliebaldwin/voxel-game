using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.VFX;
using VFolders.Libs;
using VInspector;
using static UnityEngine.Analytics.IAnalytic;
using Random = UnityEngine.Random;

public class VoxelWorld : MonoBehaviour
{
    public static VoxelWorld Instance { get; private set; }

    public Vector3Int chunkSize = new Vector3Int(8, 8, 8);
    public int2 WorldSize = new int2(32, 32);
    public int2 InitialChunks = new int2(4, 4);

    private VoxelData[,,] voxels;

    public GameObject ChunkPrefab;
    public int Spacing = 8;

    public LayerMask BlockVoxelPlacement;

    public GameObject BlockBreakVFXPrefab;

    private List<Vector3Int> DEBUGTraversalPosList = new List<Vector3Int>();
    private List<Color> DEBUGTraversalColorList;
    private Vector3 DEBUGWorldHitPoint = Vector3.zero;

    public bool Initialized = false;

   // private List<int2> chunks = new List<int2>();
    private VoxelChunk[,] voxelChunks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }


    void Start()
    {
        InitializeWorld();
    }


    [Button(name = "Initialize World", size = 20, color = "black")]
    public void InitializeWorld()
    {
        if (Instance != null && Instance != this) {
            Destroy(this);
        } else {
            Instance = this;
        }
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

        Vector3Int worldVoxelSize = new Vector3Int(chunkSize.x * WorldSize.x, chunkSize.y, chunkSize.z * WorldSize.y);
        voxels = GenerateVoxelsCPU(worldVoxelSize);
        Debug.Log($"voxels size={worldVoxelSize}");


        voxelChunks = new VoxelChunk[WorldSize.x, WorldSize.y];
        for (int x = 0; x < InitialChunks.x; x++)
        {
            for (int z = 0; z < InitialChunks.y; z++)
            {
                AddChunk(new int2(x, z));
            }
        }
    }


    private void OnDrawGizmos()
    {
        for (int i=0; i<DEBUGTraversalPosList.Count; i++)
        {
            //Gizmos.color = DEBUGTraversalColorList[i];
            Gizmos.color = new Color((float)i / DEBUGTraversalPosList.Count, 0f, 0f);
            if (i >= DEBUGTraversalPosList.Count-1) Gizmos.color = Color.white;
            Gizmos.DrawCube(DEBUGTraversalPosList[i], Vector3.one);
        }
        if (DEBUGWorldHitPoint != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(DEBUGWorldHitPoint, 0.2f);
        }
    }

    public void AddChunk(int2 pos)
    {
        if (voxelChunks[pos.x, pos.y] == null)
        {
            //Debug.Log($"Adding chunk at ({pos.x},{pos.y})");
            //chunks.Add(pos);
            VoxelChunk newChunk = Instantiate(ChunkPrefab).GetComponent<VoxelChunk>();
            voxelChunks[pos.x, pos.y] = newChunk;

            newChunk.Size3D = chunkSize;
            newChunk.ChunkCoord = pos;
            newChunk.transform.name = $"Chunk_x{pos.x}_z{pos.y}";
            newChunk.transform.position = new Vector3(pos.x * chunkSize.x, 0, pos.y * chunkSize.z);
            newChunk.transform.parent = transform;

            MinMaxAABB bounds = new MinMaxAABB(new float3(pos.x * chunkSize.x, 0f, pos.y * chunkSize.z), new float3((pos.x + 1) * chunkSize.x, chunkSize.y, (pos.y + 1) * chunkSize.z));

            Debug.Log($"Bounds = ({bounds.Min}) - ({bounds.Max})");

            VoxelData[,,] chunkData = new VoxelData[chunkSize.x, chunkSize.y, chunkSize.z];
  
            for (int x = 0; x < chunkSize.x; x++)
            {
                for (int y = 0; y < chunkSize.y; y++)
                {
                    for (int z = 0; z < chunkSize.z; z++)
                    {
                        VoxelData v = voxels[x + pos.x*chunkSize.x, y, z + pos.y* chunkSize.z];
                        chunkData[x, y, z] = v;
                    }
                }
            }
            newChunk.SetVoxels(chunkData);
            newChunk.InitializeChunk();
        }
        //try
        //{
            
        //}
        //catch (IndexOutOfRangeException ex) 
        //{
        //    Debug.LogWarning(ex.Message);
        //    return;
        //}
    }
    private VoxelData[,,] GenerateVoxelsCPU(Vector3Int Size3D)
    {
        VoxelData[,,] voxels = new VoxelData[Size3D.x, Size3D.y, Size3D.z];
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int z = 0; z < Size3D.z; z++)
            {
                float r = Mathf.Cos(z * .1f) * Mathf.Sin(x * 0.1f) * 4f + 8f;

                for (int y = 0; y < Size3D.y; y++)
                {

                    voxels[x, y, z] = new VoxelData(y < r ? 1 : 0, 0, 0);
                }
            }
        }
        return voxels;
    }

    public void DestroyVoxel(Vector3 worldPos)
    {
        int2 chunkPos = FindContainingChunk(worldPos);
        VoxelChunk chunk = voxelChunks[chunkPos.x, chunkPos.y];
        GameObject breakVFX = Instantiate(BlockBreakVFXPrefab, worldPos, Quaternion.identity);
        breakVFX.GetComponent<VFXObject>().InitVFX(chunk.LookupVoxel(worldPos).ID);
        chunk.DamageBlock(worldPos, 1);
    }

    public void AddVoxel(Vector3 worldPos, int blockType)
    {
        int2 chunkPos = FindContainingChunk(worldPos);
        VoxelChunk chunk = voxelChunks[chunkPos.x, chunkPos.y];
        if (Physics.CheckBox(worldPos, Vector3.one * 0.5f, Quaternion.identity, BlockVoxelPlacement.value))
        {
            return;
        }
        chunk.PlaceBlock(worldPos, blockType);
    }

    // from https://web.archive.org/web/20121024081332/www.xnawiki.com/index.php?title=Voxel_traversal
    public VoxelHitInfo VoxelTraversal(Vector3 pos, Vector3 dir, int maxDepth)
    {
        DEBUGTraversalPosList = new List<Vector3Int>();
        DEBUGTraversalColorList = new List<Color>();

        //pos += positionOffset;

        Vector3Int start = WorldPosToVoxel(pos + 0.5f * Vector3.one);
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
       //Debug.Log($"INIT tMax.x :  ({voxelBoundary.x}-{pos.x})/{dir.x} = {tMax.x}");
       //Debug.Log($"INIT tMax.y :  ({voxelBoundary.y}-{pos.y})/{dir.y} = {tMax.y}");
       //Debug.Log($"INIT tMax.z :  ({voxelBoundary.z}-{pos.z})/{dir.z} = {tMax.z}");
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
            if (hitVoxel.ID > 0)
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
            else if (hitVoxel.ID == -1)
            {
                return new VoxelHitInfo(false);
            }
            else
            {
                DEBUGTraversalColorList.Add(new Color(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z)));
            }

            Vector3 absTMax = new Vector3(Mathf.Abs(tMax.x), Mathf.Abs(tMax.y), Mathf.Abs(tMax.z)); 
            if (absTMax.x < absTMax.y && absTMax.x < absTMax.z) // tMax.X is the lowest, an YZ cell boundary plane is nearest.
            {
                stepPos.x += stepX;
                t = tMax.x;
                tMax.x += tDelta.x;
                hitNormal = new Vector3Int(-stepX, 0, 0);
              //  Debug.Log($"Step {i}: tMax.X is lowest, add tDelta.x ({tDelta.x})   ->   new tMax = ({tMax.x}, {tMax.y}, {tMax.z})");
            }
            else if (absTMax.y < absTMax.z)               // tMax.Y is the lowest, an XZ cell boundary plane is nearest.
            {
                stepPos.y += stepY;
                t = tMax.y;
                tMax.y += tDelta.y;
                hitNormal = new Vector3Int(0, -stepY, 0);
                //Debug.Log($"Step {i}: tMax.Y is lowest, add tDelta.y ({tDelta.y})   ->   new tMax = ({tMax.x}, {tMax.y}, {tMax.z})");
            }
            else                                    // tMax.Z is the lowest, an XY cell boundary plane is nearest.
            {
                stepPos.z += stepZ;
                t = tMax.z;
                tMax.z += tDelta.z;
                hitNormal = new Vector3Int(0, 0, -stepZ);
                //Debug.Log($"Step {i}: tMax.Z is lowest, add tDelta.z ({tDelta.z})   ->   new tMax = ({tMax.x}, {tMax.y}, {tMax.z})");
            }

            
        }
        return new VoxelHitInfo(false);
    }

    private VoxelData LookupVoxel(Vector3Int voxelPos)
    {
        int2 chunkPos = new int2(Mathf.FloorToInt(voxelPos.x / Spacing), Mathf.FloorToInt(voxelPos.z / Spacing));
        try
        {
            VoxelChunk chunk = voxelChunks[chunkPos.x, chunkPos.y];
            return chunk.LookupVoxel(voxelPos);

        }
        catch (NullReferenceException ex)
        {
            Debug.Log($"No chunk at ({chunkPos.x}, {chunkPos.y}) [{ex.Message}]");
            return new VoxelData(-1,0,0);
        }
        catch (IndexOutOfRangeException ex)
        {
            Debug.LogWarning(ex.Message);
            return new VoxelData(-1, 0, 0);
        }
    }


    private int2 FindContainingChunk(Vector3 voxelWorldPos)
    {
        return new int2(Mathf.FloorToInt(voxelWorldPos.x / Spacing), Mathf.FloorToInt(voxelWorldPos.z / Spacing));
    }

    private Vector3Int WorldPosToVoxel(Vector3 worldPos)
    {
        Vector3Int result = new Vector3Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y), Mathf.RoundToInt(worldPos.z));
        //Vector3Int result = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), Mathf.FloorToInt(worldPos.z));

        return result;
    }
}

public struct VoxelData
{
    public int ID;
    public int Damage;
    public int Orientation;

    public VoxelData(int id, int damage, int orientation)
    {
        ID = id;
        Damage = damage;
        Orientation = orientation;
    }
}

public struct VoxelHitInfo
{
    public bool didHit;
    public int blockID;
    public VoxelData voxel;
    public Vector3Int voxelPos;
    public Vector3 hitPos;
    public Vector3Int hitNormal;

    public VoxelHitInfo(bool didHit)
    {
        this.didHit = didHit;
        blockID = 0;
        voxel = new VoxelData();
        voxelPos = Vector3Int.zero;
        hitPos = Vector3.zero;
        hitNormal = Vector3Int.up;
    }
}
