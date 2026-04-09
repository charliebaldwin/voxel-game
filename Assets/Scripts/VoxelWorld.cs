using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using VFolders.Libs;
using VInspector;
using static UnityEngine.Analytics.IAnalytic;

public class VoxelWorld : MonoBehaviour
{
    public static VoxelWorld Instance { get; private set; }

    public int2 WorldSize = new int2(32, 32);
    public int2 InitialChunks = new int2(4, 4);

    public GameObject ChunkPrefab;
    public int Spacing = 8;

    public LayerMask BlockVoxelPlacement;

    public GameObject BlockBreakVFXPrefab;

    private List<Vector3Int> DEBUGTraversalPosList = new List<Vector3Int>();
    private List<Color> DEBUGTraversalColorList;

   // private List<int2> chunks = new List<int2>();
    private VoxelChunk[,] voxelChunks;

    private void Awake()
    {
        InitializeWorld();
    }

    [Button(name = "Initialize World", size = 20, color = "black")]
    private void InitializeWorld()
    {


        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        for(int i=transform.childCount-1; i>=0; i--)
        {
            GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
        }


        voxelChunks = new VoxelChunk[WorldSize.x, WorldSize.y];
        for (int x = 0; x < InitialChunks.x; x++)
        {
            for (int z = 0; z < InitialChunks.y; z++)
            {
                AddChunk(new int2(x, z));
            }
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
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
    }

    public void AddChunk(int2 pos)
    {

        try
        {
            if (voxelChunks[pos.x, pos.y] == null)
            {
                //Debug.Log($"Adding chunk at ({pos.x},{pos.y})");
                //chunks.Add(pos);
                VoxelChunk newChunk = Instantiate(ChunkPrefab).GetComponent<VoxelChunk>();
                voxelChunks[pos.x, pos.y] = newChunk;
                newChunk.ChunkCoord = pos;
                newChunk.transform.position = new Vector3(pos.x, 0, pos.y) * Spacing;
                newChunk.transform.parent = transform;
                newChunk.InitializeChunk();
            }
        }
        catch (IndexOutOfRangeException ex) 
        {
            Debug.LogWarning(ex.Message);
            return;
        }
    }

    public void DestroyVoxel(Vector3 worldPos)
    {
        int2 chunkPos = FindContainingChunk(worldPos);
        VoxelChunk chunk = voxelChunks[chunkPos.x, chunkPos.y];
        GameObject breakVFX = Instantiate(BlockBreakVFXPrefab, worldPos, Quaternion.identity);
        breakVFX.GetComponent<VFXObject>().InitVFX(chunk.LookupVoxel(worldPos));
        chunk.BreakBlock(worldPos);
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
    public VoxelHitData VoxelTraversal(Vector3 pos, Vector3 dir, int maxDepth)
    {
        DEBUGTraversalPosList = new List<Vector3Int>();
        DEBUGTraversalColorList = new List<Color>();

        Vector3Int start = WorldPosToVoxel(pos);
        int stepX = Math.Sign(dir.x);
        int stepY = Math.Sign(dir.y);
        int stepZ = Math.Sign(dir.z);


        // Calculate cell boundaries. When the step (i.e. direction sign) is positive,
        // the next boundary is AFTER our current position, meaning that we have to add 1.
        // Otherwise, it is BEFORE our current position, in which case we add nothing.
        Vector3Int voxelBoundary = new Vector3Int(
            start.x + (stepX > 0 ? 1 : 0),
            start.y + (stepY > 0 ? 1 : 0),
            start.z + (stepZ > 0 ? 1 : 0)
        );

        // tMax : Determine how far we can travel along the ray before we hit a voxel boundary.
        Vector3 tMax = new Vector3(
            (voxelBoundary.x - pos.x) / dir.x,  // Boundary is a plane on the YZ axis
            (voxelBoundary.y - pos.y) / dir.y,  // Boundary is a plane on the XZ axis
            (voxelBoundary.z - pos.z) / dir.z   // Boundary is a plane on the XY axis
        );
        Debug.Log($"INIT tMax.x :  ({voxelBoundary.x}-{pos.x})/{dir.x}={tMax.x}");
        Debug.Log($"INIT tMax.y :  ({voxelBoundary.y}-{pos.y})/{dir.y}={tMax.y}");
        Debug.Log($"INIT tMax.z :  ({voxelBoundary.z}-{pos.z})/{dir.z}={tMax.z}");
        Vector3 tDelta = new Vector3(
            stepX / dir.x,               // Crossing the width of a cell.
            stepY / dir.y,               // Crossing the height of a cell.
            stepZ / dir.z                // Crossing the depth of a cell.
        );
        if (Single.IsNaN(tDelta.x)) tDelta.x = Single.PositiveInfinity;
        if (Single.IsNaN(tDelta.y)) tDelta.y = Single.PositiveInfinity;
        if (Single.IsNaN(tDelta.z)) tDelta.z = Single.PositiveInfinity;

        // For each step, determine which distance to the next voxel boundary is lowest (i.e.
        // which voxel boundary is nearest) and walk that way.
        Vector3Int stepPos = start;
        Vector3Int hitNormal = new Vector3Int(0,0,0);
        for (int i = 0; i < maxDepth; i++)
        {
            DEBUGTraversalPosList.Add(stepPos);
            Debug.Log($"Step {i}: starting at ({stepPos.x}, {stepPos.y}, {stepPos.z})");
            Debug.Log($"Step {i}: tMax is ({tMax.x}, {tMax.y}, {tMax.z})");


            int blockID = LookupVoxel(stepPos);
            if (blockID > 0)
            {
                DEBUGTraversalColorList.Add(Color.white);

                VoxelHitData hitData = new VoxelHitData(true);
                hitData.hitNormal = hitNormal;
                hitData.blockID = blockID;
                hitData.worldVoxelPos = stepPos;
                return hitData;
            }
            else if (blockID == -1)
            {
                return new VoxelHitData(false);
            }
            else
            {
                DEBUGTraversalColorList.Add(new Color(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z)));
            }

            Vector3 absTMax = new Vector3(Mathf.Abs(tMax.x), Mathf.Abs(tMax.y), Mathf.Abs(tMax.z)); 
            if (absTMax.x < absTMax.y && absTMax.x < absTMax.z) // tMax.X is the lowest, an YZ cell boundary plane is nearest.
            {
                stepPos.x += stepX;
                tMax.x += tDelta.x;
                hitNormal = new Vector3Int(-stepX, 0, 0);
                Debug.Log($"Step {i}: tMax.X is lowest, add tDelta.x ({tDelta.x})");
            }
            else if (absTMax.y < absTMax.z)               // tMax.Y is the lowest, an XZ cell boundary plane is nearest.
            {
                stepPos.y += stepY;
                tMax.y += tDelta.y;
                hitNormal = new Vector3Int(0, -stepY, 0);
                Debug.Log($"Step {i}: tMax.Y is lowest, add tDelta.y ({tDelta.y})");
            }
            else                                    // tMax.Z is the lowest, an XY cell boundary plane is nearest.
            {
                stepPos.z += stepZ;
                tMax.z += tDelta.z;
                hitNormal = new Vector3Int(0, 0, -stepZ);
                Debug.Log($"Step {i}: tMax.Z is lowest, add tDelta.z ({tDelta.z})");
            }

            
        }
        return new VoxelHitData(false);
    }
    public VoxelHitData VoxelRaycast(Vector3 pos, Vector3 dir, float distance, int steps)
    {
        Vector3 d = (distance / (float)steps) * dir;
        Vector3 stepPos = pos;
        Vector3Int lastVoxelPos = WorldPosToVoxel(stepPos);
        Vector3Int lastVoxelPos2 = lastVoxelPos;
        VoxelHitData hitData = new VoxelHitData(false);


        for (int i = 0; i < steps; i++) {
            stepPos += d;
            Vector3Int voxelPos = WorldPosToVoxel(stepPos);
            if (voxelPos != lastVoxelPos) {
                //Debug.Log($"last voxel pos changed from {lastVoxelPos} to {voxelPos}");
                lastVoxelPos2 = lastVoxelPos;
                lastVoxelPos = voxelPos;
            }

            int2 chunkPos = FindContainingChunk(stepPos);
            try
            {
                VoxelChunk chunk = voxelChunks[chunkPos.x, chunkPos.y];
                int lookup = chunk.LookupVoxel(voxelPos);

                if (lookup != 0)
                {
                    hitData.worldVoxelPos = voxelPos;
                    hitData.hitNormal = lastVoxelPos2 - voxelPos;
                    //Debug.Log($"hitdata normal: {hitData.hitNormal}");
                    hitData.blockID = lookup;
                    hitData.didHit = true;
                    return hitData;
                }
            }
            catch (NullReferenceException ex)
            {
                Debug.Log($"No chunk at ({chunkPos.x}, {chunkPos.y}) [{ex.Message}]");
            }
            catch (IndexOutOfRangeException ex)
            {
                Debug.LogWarning(ex.Message);
            }


        }
        return hitData;

    }

    private int LookupVoxel(Vector3Int voxelPos)
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
            return -1;
        }
        catch (IndexOutOfRangeException ex)
        {
            Debug.LogWarning(ex.Message);
            return -1;
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
