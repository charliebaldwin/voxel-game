using System;
using UnityEngine;
using VInspector;

public class RaycastTester : MonoBehaviour
{
    public VoxelWorld world;
    public Vector3 offset = new Vector3(0.5f, 0.5f, 0.5f);

    public void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, transform.forward * 10f);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        TestRaycast();
    }

    [Button(name = "TestRaycast", size = 20, color = "black")]

    public void TestRaycast()
    {
        if (!world.Initialized) world.InitializeWorld();
        world.VoxelTraversal(transform.position + offset, transform.forward, 30);

    }
}
