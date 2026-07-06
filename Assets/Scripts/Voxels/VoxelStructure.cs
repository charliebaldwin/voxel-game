using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class VoxelStructure : MonoBehaviour
{

    //public List<VoxelStructureComponent> Components = new List<VoxelStructureComponent>();

    public Dictionary<Vector3Int, Voxel> voxels;

    [Button]
    public Dictionary<Vector3Int, Voxel> GetStructureVoxels()
    {
        voxels = new Dictionary<Vector3Int, Voxel>();
        voxels.Clear();
        VoxelStructureComponent[] components = transform.GetComponentsInChildren<VoxelStructureComponent>();
        foreach(VoxelStructureComponent component in components)
        {
            Dictionary<Vector3Int, Voxel> componentVoxels = component.GenerateVoxels();
            foreach(KeyValuePair<Vector3Int, Voxel> v in componentVoxels)
            {
                voxels.Add(v.Key, v.Value);
            }
        }
        return voxels;
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
