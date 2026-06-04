using UnityEngine;

public static class VoxelHelper
{
    public static int[] Triangles = new int[6] { 0, 1, 2, 0, 2, 3 };


    private static Vector3 nx_ny_nz = new Vector3(-1f, -1f, -1f);
    private static Vector3 nx_ny_pz = new Vector3(-1f, -1f, 1f);
    private static Vector3 nx_py_pz = new Vector3(-1f, 1f, 1f);
    private static Vector3 px_py_pz = new Vector3(1f, 1f, 1f);
    private static Vector3 px_py_nz = new Vector3(1f, 1f, -1f);
    private static Vector3 px_ny_nz = new Vector3(1f, -1f, -1f);
    private static Vector3 nx_py_nz = new Vector3(-1f, 1f, -1f);
    private static Vector3 px_ny_pz = new Vector3(1f, -1f, 1f);

    public static Vector3[] GetFaceVerts(Vector3Int normal)
    {
        if (normal == Vector3Int.left)
        {
            return new Vector3[4] { nx_py_nz, nx_ny_nz, nx_ny_pz, nx_py_pz };
        }
        else if (normal == Vector3Int.right)
        {
            return new Vector3[4] { px_ny_nz, px_py_nz, px_py_pz, px_ny_pz };
        }
        else if (normal == Vector3Int.down)
        {
            return new Vector3[4] { nx_ny_nz, px_ny_nz, px_ny_pz, nx_ny_pz };
        }
        else if (normal == Vector3Int.up)
        {
            return new Vector3[4] { px_py_nz, nx_py_nz, nx_py_pz, px_py_pz };
        }
        else if (normal == Vector3Int.back)
        {
            return new Vector3[4] { px_py_nz, px_ny_nz, nx_ny_nz, nx_py_nz };
        }
        else if (normal == Vector3Int.forward)
        {
            return new Vector3[4] { px_ny_pz, px_py_pz, nx_py_pz, nx_ny_pz };
        }
        else
        {
            return null;
        }
    }
}
