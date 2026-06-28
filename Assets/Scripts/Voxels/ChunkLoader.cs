using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;
using VInspector.Libs;
using static VoxelHelper;

public class ChunkLoader : MonoBehaviour
{
    Vector3Int currentChunkPos = Vector3Int.zero;
    public int RenderDistance = 6;
    
    void Update()
    {
        Vector3Int chunkPos = FindContainingChunk(SnapToGrid(transform.position), World().ChunkSize);

        if (chunkPos.x != currentChunkPos.x || chunkPos.z != currentChunkPos.z)
        {
            //Debug.Log($"ChunkPos = {chunkPos}");
            World().LoadChunkSpread(chunkPos, RenderDistance);
            World().UnloadDistantChunks(chunkPos, RenderDistance + 2);
            //World().UnloadChunk(currentChunkPos);
            currentChunkPos = chunkPos;
        }
    }
}
