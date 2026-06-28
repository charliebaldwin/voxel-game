using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static VoxelHelper;


public struct VoxelMesherJob : IJob
{
    public Vector3Int Size3D;
    public NativeArray<BlockID> Voxels;
    public Vector3Int ChunkCoord;

    public int t;
    public NativeArray<Vector3> verticesResult;
    public NativeArray<Vector3> normalsResult;
    public NativeArray<Vector2> uvsResult;
    public NativeArray<int> trianglesResult;
    public NativeArray<Color> colorsResult;

    public void Execute()
    {
        t = 0;

        ComputeMesh();
    }

    private void ComputeMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        t = 0;

        int i_v = 0, i_n = 0, i_uv = 0, i_c = 0, i_t = 0;

        for (int z = 0; z < Size3D.z; z++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int x = 0; x < Size3D.x; x++)
                {
                    BlockID voxID = Voxels[x + Size3D.x * y + Size3D.x * Size3D.y * z];
                    if (voxID != 0)
                    {
                        Vector3 pos = new Vector3(x, y, z);

                        int[] neighbors = new int[6] { 0, 0, 0, 0, 0, 0 };
                        for (int n = 0; n < 6; n++)
                            neighbors[n] = (int)World().LookupVoxel(LocalToWorld(new Vector3Int(x, y, z) + Directions[n], ChunkCoord, Size3D)).Shape;

                        BlockModel model = new BlockModel(pos, t, neighbors, new Voxel(voxID, 0, 0));
                        foreach (Vector3 v in model.vertices)
                        {
                            verticesResult[i_v] = v;
                            i_v++;
                        }
                        foreach (Vector3 n in model.normals)
                        {
                            normalsResult[i_n] = n;
                            i_n++;
                        }
                        foreach (Vector2 uv in model.uvs)
                        {
                            uvsResult[i_uv] = uv;
                            i_uv++;
                        }
                        foreach (Color c in model.colors)
                        {
                            colorsResult[i_c] = c;
                            i_c++;
                        }
                        foreach (int tri in model.triangles)
                        {
                            trianglesResult[i_t] = tri;
                            i_t++;
                        }
                        t = model.lastT;
                    }
                }
            }
        }
        //verticesResult.CopyFrom(vertices.ToArray());
        //normalsResult.CopyFrom(normals.ToArray());
        //uvsResult.CopyFrom(uvs.ToArray());
        //trianglesResult.CopyFrom(triangles.ToArray());
        //colorsResult.CopyFrom(colors.ToArray());

    }


}