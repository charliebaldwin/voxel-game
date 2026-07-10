using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;
//using VInspector.Libs;
using static VoxelHelper;

public class ChunkLoader : MonoBehaviour
{
    Vector3Int currentChunkPos = Vector3Int.zero;
    public int RenderDistance = 6;
    private VoxelWorld world;

    private void Start()
    {
        world = VoxelWorld.Instance;
    }

    void Update()
    {
        if (world == null) world = VoxelWorld.Instance;

        Vector3Int chunkPos = FindContainingChunk(SnapToGrid(transform.position), World().ChunkSize);

        if (chunkPos.x != currentChunkPos.x || chunkPos.z != currentChunkPos.z)
        {
            Debug.Log($"starting load from {chunkPos}");
            world.LoadChunkSpread(chunkPos, RenderDistance);
            world.UnloadDistantChunks(chunkPos, RenderDistance + 2);
            //World().UnloadChunk(currentChunkPos);
            currentChunkPos = chunkPos;
        }
    }
}
