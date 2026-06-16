using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static VoxelHelper;

public class BlockModel
{
    public Vector3 blockPos;
    public bool empty = true;

    public Vector3[] vertices;
    public Vector3[] normals;
    public Vector2[] uvs;
    public Color[] colors;
    public int[] triangles;

    public int lastT;

    public BlockModel(Vector3 pos, int firstTriangle, int[] neighbors, VoxelData voxel)
    {
        blockPos = pos;
        List<Vector3> vertList = new List<Vector3>();
        List<Vector3> normalList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();
        List<Color> colorList = new List<Color>();
        List<int> triangleList = new List<int>();
        int t = firstTriangle;
        int[] nb = neighbors;

        switch ((BlockShapes)voxel.BlockShape)
        {
            case BlockShapes.EMPTY:
                break;

            case BlockShapes.SOLID:
                Quaternion q = Quaternion.identity;
                int[] s = VoxelWorld.Instance.shuffle;
                switch (voxel.Orientation)
                {
                    case 0:
                        break;
                    case 1:
                        q = Quaternion.Euler(180f, 0f, 0f);
                        nb = new int[6] { nb[0], nb[1], nb[3], nb[2], nb[5], nb[4] };
                        break;
                    case 2:
                        q = Quaternion.Euler(0f, 0f, 90f);
                        nb = new int[6] { nb[2], nb[3], nb[1], nb[0], nb[4], nb[5] };
                        break;
                    case 3:
                        q = Quaternion.Euler(0f, 0f, -90f);
                        nb = new int[6] { nb[3], nb[2], nb[0], nb[1], nb[4], nb[5] };
                       // nb = new int[6] { nb[s[0]], nb[s[1]], nb[s[2]], nb[s[3]], nb[s[4]], nb[s[5]] };
                        break;
                    case 4:
                        q = Quaternion.Euler(-90f, 0f, 0f);
                        nb = new int[6] { nb[0], nb[1], nb[5], nb[4], nb[2], nb[3] };
                        break;
                    case 5:
                        q = Quaternion.Euler(90f, 0f, 0f);
                        nb = new int[6] { nb[0], nb[1], nb[4], nb[5], nb[3], nb[2] };
                        //nb = new int[6] { nb[s[0]], nb[s[1]], nb[s[2]], nb[s[3]], nb[s[4]], nb[s[5]] };
                        break;
                }
                Matrix4x4 rotateMat = Matrix4x4.Rotate(q);

                for (int n = 0; n < 6; n++) // iterate per face
                {
                    if ((BlockShapes)nb[n] != BlockShapes.SOLID)
                    { 
                        foreach(Vector3 v in GetFaceVerts(Directions[n])) {
                            Vector3 newV = v;
                            newV = rotateMat * v;
                            vertList.Add(newV * 0.5f + blockPos);
                        }

                        Vector3 normal = Directions[n];
                        for(int i=0;i<4;i++)
                            normalList.Add(rotateMat * normal);

                        foreach (Vector2 uv in GetFaceUVs(Directions[n]))
                            uvList.Add(uv);

                        Color c = new Color(voxel.ID, n, (float)voxel.Damage / (float)voxel.Toughness);
                        colorList.AddRange(new Color[4] { c, c, c, c });

                        for (int i = 0; i < 6; i++)
                            triangleList.Add(t + Triangles[i]);

                        t += 4;
                    }
                }
                break;

            case BlockShapes.HALF_SLAB:
                for (int n = 0; n < 6; n++)
                {
                    if ((BlockShapes)neighbors[n] != BlockShapes.SOLID || n==3)
                    {
                        foreach (Vector3 v in GetFaceVerts(Directions[n]))
                        {
                            Vector3 slabV = v;
                            slabV.y = Mathf.Clamp(v.y, -1f, 0f);
                            vertList.Add(slabV * 0.5f + blockPos);
                        }

                        Vector3 normal = Directions[n];
                        for (int i = 0; i < 4; i++)
                            normalList.Add(normal);

                        foreach (Vector2 uv in GetFaceUVs(Directions[n])) {
                            Vector2 slabUV = uv;
                            if (n != 2 && n != 3)
                                slabUV.y = Mathf.Clamp(slabUV.y, 0.5f, 1f);
                            uvList.Add(slabUV);
                        }

                        Color c = new Color(voxel.ID, n, (float)voxel.Damage / (float)voxel.Toughness);
                        colorList.AddRange(new Color[4] { c, c, c, c });

                        for (int i = 0; i < 6; i++)
                            triangleList.Add(t + Triangles[i]);

                        t += 4;
                    }
                }
                break;
        }

        lastT = t;

        vertices = vertList.ToArray();
        normals = normalList.ToArray();
        uvs = uvList.ToArray();
        colors = colorList.ToArray();
        triangles = triangleList.ToArray();
    }
    
    
    public static Vector3[] GetVertices(BlockShapes shape)
    {
        if (shape == BlockShapes.SOLID) { 
            return new Vector3[8] {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
            };
        }
        else if (shape == BlockShapes.HALF_SLAB)
        {
            return new Vector3[8] {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f,  0.0f,  0.5f),
                new Vector3( 0.5f,  0.0f,  0.5f),
                new Vector3( 0.5f,  0.0f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f,  0.0f, -0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
            };
        }
        else
        {
            return null;
        }
    }
}
