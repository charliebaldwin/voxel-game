using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;
using VInspector.Libs;
using static VoxelHelper;

public class ChunkLoader : MonoBehaviour
{
    int2 currentChunkPos = new int2(0, 0);
    public int RenderDistance = 6;
    
    void Update()
    {
        int2 chunkPos = FindContainingChunk(SnapToGrid(transform.position), World().ChunkSize);

        if (chunkPos.x != currentChunkPos.x || chunkPos.y != currentChunkPos.y)
        {
            Debug.Log($"ChunkPos = {chunkPos}");
            World().LoadChunkSpread(chunkPos, RenderDistance);
            World().UnloadDistantChunks(chunkPos, RenderDistance + 2);
            //World().UnloadChunk(currentChunkPos);
            currentChunkPos = chunkPos;
        }
    }
}
