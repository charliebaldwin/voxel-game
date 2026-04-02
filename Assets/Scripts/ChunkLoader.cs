using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;
using VInspector.Libs;

public class ChunkLoader : MonoBehaviour
{
    public GameObject ChunkPrefab;

    public float Radius;
    public Vector2Int InitialChunks = new Vector2Int(8,8);

    [ShowInInspector]
    private List<int2> chunks = new List<int2>();
    private VoxelChunk[,] voxelChunks;
    private int spacing = 12;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        voxelChunks = new VoxelChunk[InitialChunks.x, InitialChunks.y];
        spacing = VoxelWorld.Instance.Spacing;

        for (int x = 0; x < InitialChunks.x; x++)
        {
            for (int z = 0; z < InitialChunks.y; z++)
            {
                VoxelWorld.Instance.AddChunk(new int2(x, z));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        int2 steppedPos = new int2(Mathf.FloorToInt(transform.position.x / spacing), Mathf.FloorToInt(transform.position.z / spacing));
        VoxelWorld.Instance.AddChunk(steppedPos);

    }


    public VoxelChunk GetAdjacentChunk(int2 pos, int2 dir)
    {
        return voxelChunks[pos.x + dir.x, pos.y + dir.y];
    }


}
