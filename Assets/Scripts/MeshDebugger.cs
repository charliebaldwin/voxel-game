using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class MeshDebugger : MonoBehaviour
{

    public MeshFilter meshFilter;
    public int index;
    public bool showDot = false;

    private void OnDrawGizmos()
    {
        if (showDot)
        {
            List<Vector3> verts = new List<Vector3>();
            meshFilter.mesh.GetVertices(verts);
            Vector3 vertPos = verts[index];
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(vertPos, 0.2f);
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
