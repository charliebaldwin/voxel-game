using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class VoxelStructureComponent : MonoBehaviour
{
    public BoxCollider Box;
    public Color PreviewColor;
    public BlockID BlockID;
    private Dictionary<Vector3Int, Voxel> voxels = new Dictionary<Vector3Int, Voxel>();

    private void OnValidate()
    {
        GenerateVoxels();
    }

    [Button]
    public Dictionary<Vector3Int, Voxel> GenerateVoxels()
    {
        transform.position = VoxelHelper.SnapToGrid(transform.position);
        Box.size = VoxelHelper.SnapToGrid(Box.size);
        Box.center = Box.size * 0.5f - Vector3.one * 0.5f;
        voxels = new Dictionary<Vector3Int, Voxel>();
        voxels.Clear();
        Vector3Int root = new Vector3Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), Mathf.RoundToInt(transform.position.z));
        Vector3Int size = new Vector3Int(Mathf.RoundToInt(Box.size.x), Mathf.RoundToInt(Box.size.y), Mathf.RoundToInt(Box.size.z));
        for (int x = root.x; x < root.x + size.x; x++)
        {
            for (int y = root.y; y < root.y + size.y; y++)
            {
                for (int z = root.z; z < root.z + size.z; z++)
                {
                    voxels.Add(new Vector3Int(x, y, z), new Voxel(BlockID, 0, 0));
                }
            }
        }
        return voxels;
    }

    private void OnDrawGizmos()
    {
        foreach(KeyValuePair<Vector3Int, Voxel> v in voxels)
        {
            Gizmos.color = PreviewColor;
            Gizmos.DrawCube(v.Key, Vector3.one);
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
}
