using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;
using VInspector.Libs;

public class ChunkLoader : MonoBehaviour
{
    public float Radius;
    private int spacing = 12;

    void Start()
    {
        spacing = VoxelWorld.Instance.Spacing;

    }

    
    void Update()
    {
        int2 steppedPos = new int2(Mathf.FloorToInt(transform.position.x / spacing), Mathf.FloorToInt(transform.position.z / spacing));
        VoxelWorld.Instance.AddChunk(steppedPos);

    }
}
