using Unity.Mathematics;
using UnityEngine;

public static class VoxelHelper
{
    public static int[] Triangles = new int[6] { 0, 1, 2, 0, 2, 3 };

    public static Vector3Int[] Directions = new Vector3Int[6] { Vector3Int.left, Vector3Int.right, Vector3Int.down, Vector3Int.up, Vector3Int.back, Vector3Int.forward };


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

    public static bool IsPosInGridBounds(Vector3Int pos, Vector3Int size)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.z >= 0 && pos.x < size.x && pos.y < size.y && pos.z < size.z;
    }

    public static Vector3Int LocalToWorld(Vector3Int localPos, int2 chunkCoord, Vector3Int size)
    {
        return new Vector3Int(localPos.x + chunkCoord.x * size.x, localPos.y, localPos.z + chunkCoord.y * size.z);
    }
    public static Vector3Int WorldToLocal(Vector3 worldPos, Vector3 chunkPos)
    {
        Vector3 localPos = worldPos - chunkPos;
        Vector3Int result = new Vector3Int(Mathf.RoundToInt(localPos.x), Mathf.RoundToInt(localPos.y), Mathf.RoundToInt(localPos.z));
        return result;
    }

    public static int[] GenerateIndices(int vertexCount)
    {
        int[] result = new int[(vertexCount / 4) * 6];
        for (int i = 0; i < vertexCount / 4 - 0; i++)
        {
            result[i * 6 + 0] = i * 4 + 0;
            result[i * 6 + 1] = i * 4 + 1;
            result[i * 6 + 2] = i * 4 + 2;
            result[i * 6 + 3] = i * 4 + 0;
            result[i * 6 + 4] = i * 4 + 2;
            result[i * 6 + 5] = i * 4 + 3;
        }
        return result;
    }

    public static int[,,] FlatTo3DArray(int[] flat, Vector3Int size)
    {
        int[,,] result = new int[size.x, size.y, size.z];

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {

                    result[x, y, z] = flat[x + size.x * y + size.x * size.y * z];
                }
            }
        }
        return result;
    }

    public static int[] ThreeDToFlatArray(int[,,] threeDarray, Vector3Int size)
    {
        int[] result = new int[size.x * size.y * size.z];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {

                    result[x + size.x * y + size.x * size.y * z] = threeDarray[x, y, z];
                }
            }
        }
        return result;
    }

    public static VoxelData[,,] IntTo3DVoxelData(int[] flat, Vector3Int size)
    {
        VoxelData[,,] result = new VoxelData[size.x, size.y, size.z];

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    int id = flat[x + size.x * y + size.x * size.y * z];
                    result[x, y, z] = new VoxelData(id, 0, 0);
                }
            }
        }
        return result;
    }
    public static VoxelData[] VoxelDataToFlatArray(VoxelData[,,] threeDarray, Vector3Int size)
    {
        VoxelData[] result = new VoxelData[size.x * size.y * size.z];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    result[x + size.x * y + size.x * size.y * z] = threeDarray[x, y, z];
                }
            }
        }
        return result;
    }


}
