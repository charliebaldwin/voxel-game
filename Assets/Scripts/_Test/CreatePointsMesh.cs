using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatePointsMesh : MonoBehaviour
{
    public List<Vector3> positions;
    [Sirenix.OdinInspector.Button]
    void CreateMesh()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(positions);
        mesh.SetIndices(Enumerable.Range(0, positions.Count).ToArray(), MeshTopology.Points, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000); // avoid bad culling
        filter.mesh = mesh;
    }
}
