using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using VFolders.Libs;
using VInspector;

public class VoxelWorld : MonoBehaviour
{
    public static VoxelWorld Instance { get; private set; }

    public int2 WorldSize = new int2(32, 32);
    public int2 InitialChunks = new int2(4, 4);

    public GameObject ChunkPrefab;
    public int Spacing = 8;

    public LayerMask BlockVoxelPlacement;

    public GameObject BlockBreakVFXPrefab;

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


    private int2 FindContainingChunk(Vector3 voxelWorldPos)
    {
        return new int2(Mathf.FloorToInt(voxelWorldPos.x / Spacing), Mathf.FloorToInt(voxelWorldPos.z / Spacing));
    }

    private Vector3Int WorldPosToVoxel(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        Vector3Int result = new Vector3Int(Mathf.RoundToInt(localPos.x), Mathf.RoundToInt(localPos.y), Mathf.RoundToInt(localPos.z));
        return result;
    }
}
