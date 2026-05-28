using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using VInspector;

public class NewVoxelChunk : MonoBehaviour
{
    public Vector3Int VoxelSize = new Vector3Int(8, 8, 8);
    public int[,,] Voxels;
    int VertexCount = 0;
    public Vector3[] Vertices;
    public int[] Triangles;
    

    public MeshFilter Filter;


    [Button(name = "Generate Voxel Data", size = 20, color = "black")]
    void GenerateVoxelData()
    {
        Voxels = new int[VoxelSize.x, VoxelSize.y, VoxelSize.z];

        VertexCount = (VoxelSize.x + 1) * (VoxelSize.y + 1) * (VoxelSize.z + 1);
        Vertices = new Vector3[VertexCount];
        Triangles = new int[36 * (VoxelSize.x * VoxelSize.y * VoxelSize.z)];
        int i = 0;
        for (int y = 0; y <= VoxelSize.y; y++) 
        {
            for (int x = 0; x <= VoxelSize.x; x++) 
            {
                for (int z = 0; z <= VoxelSize.z; z++)
                {
                    Vertices[i] = new Vector3(x, y, z);
                    i++;
                }
            }
        }
        i = 0;
        for (int y = 0; y < VoxelSize.y; y++)
        {
            for (int x = 0; x < VoxelSize.x; x++)
            {
                for (int z = 0; z < VoxelSize.z; z++)
                {
                    int[] tris = GetCubeTriangles(i, new Vector3Int(x, y, z), VoxelSize);// + Vector3Int.one);
                    for (int t=0; t < tris.Length; t++)
                    {
                        Triangles[i * 36 + t] = tris[t];
                    }
                    i++;
                }
            }
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = Vertices;
        newMesh.triangles = Triangles;

        Filter.mesh = newMesh; 
    }

    private int[] GetCubeTriangles(int cubeIndex, Vector3Int voxelPos, Vector3Int voxelGridSize)
    {

        int x = voxelGridSize.x;
        int y = voxelGridSize.y;
        int z = voxelGridSize.z;
        Vector3Int p = voxelPos;

        int i = (p.z) + (p.x * (z+1)) + (p.y * (x+p.y + 1) * (z + p.y));
        
        int[] t = new int[8]
        {
            i,
            i + 1,
            i + x * y + 1,
            i + x * y,
            i + x,
            i + x + 1,
            i + x * y + x,
            i + x * y + x + 1
        };

        int[] triangles = new int[36]
        {
            t[0], t[1], t[3], t[0], t[3], t[2],
            t[5], t[4], t[6], t[5], t[6], t[7],
            t[4], t[0], t[2], t[4], t[2], t[6],
            t[1], t[5], t[7], t[1], t[7], t[3],
            t[2], t[3], t[7], t[2], t[7], t[6],
            t[0], t[4], t[5], t[0], t[5], t[1]
        };

        Debug.Log($"voxel: (x={p.x}, y={p.y}, z={p.z}), i={i}, indices=[{t[0]}, {t[1]}, {t[2]}, {t[3]}, {t[4]}, {t[5]}, {t[6]}, {t[7]}]");


        if (cubeIndex == -1)
        {

            foreach (int d in triangles)
            {
                Debug.Log(d);
            }
        }
        return triangles;
    }

    private void OnDrawGizmos()
    {
        if (Vertices != null)
        {
            for(int i=0; i<VertexCount; i++)
            {

                //Gizmos.DrawCube(v, 0.25f * Vector3.one);
                Handles.Label(Vertices[i], $"{i}");
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateVoxelData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
