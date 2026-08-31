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
    public Vector4[] tangents;
    public Vector4[] uvs;
    public Color[] colors;
    public int[] triangles;

    public int lastT;

    public BlockModel(Vector3 pos, int firstTriangle, Voxel voxel, Voxel[] neighborVoxels, ref List<Vector3> vertList, ref List<Vector3> normalList, ref List<Vector4> uvList, ref List<Color> colorList, ref List<int> triangleList)
    {
        if (voxel.BlockID != BlockID.Air)
        {
            blockPos = pos;
            vertList.Clear();
            normalList.Clear();
            uvList.Clear();
            colorList.Clear();
            triangleList.Clear();

            int t = firstTriangle;
            //int[] nb = neighbors;
            Voxel[] nbVoxels = neighborVoxels;
            int toughness = BlockRegistry.LookupToughness(voxel.BlockID);
            float damage = (float)voxel.Damage / (float)toughness;

            Quaternion q = Quaternion.FromToRotation(Vector3.up, voxel.UpAxis.ToVector());
            q *= Quaternion.FromToRotation(Vector3.forward, voxel.ForwardAxis.ToVector());
            Matrix4x4 rotateMat = Matrix4x4.Rotate(q);
            switch (voxel.Shape)
            {
                case BlockShape.Empty:
                    break;

                case BlockShape.Solid:
                    List<int> textureIndices = BlockRegistry.LookupBlockTextures(voxel.BlockID);

                    int air_nX = !DoAdjacentIDsMatch(nbVoxels[0].BlockID, voxel.BlockID) ? 1 : 0;
                    int air_pX = !DoAdjacentIDsMatch(nbVoxels[1].BlockID, voxel.BlockID) ? 1 : 0;
                    int air_nY = !DoAdjacentIDsMatch(nbVoxels[2].BlockID, voxel.BlockID) ? 1 : 0;
                    int air_pY = !DoAdjacentIDsMatch(nbVoxels[3].BlockID, voxel.BlockID) ? 1 : 0;
                    int air_nZ = !DoAdjacentIDsMatch(nbVoxels[4].BlockID, voxel.BlockID) ? 1 : 0;
                    int air_pZ = !DoAdjacentIDsMatch(nbVoxels[5].BlockID, voxel.BlockID) ? 1 : 0;
                    for (int n = 0; n < 6; n++) // iterate per face
                    {
                        Direction dir = (Direction)n;

                        //if ((BlockShape)nb[n] != BlockShape.Solid)
                        if (!IsNeighborFaceSolid(dir, nbVoxels[n]))
                        {

                            // Vertex Position
                            foreach (Vector3 v in GetFaceVerts(VectorDirections[n]))
                            {
                                Vector3 newV = v;
                                newV = rotateMat * v;
                                vertList.Add(newV * 0.5f + blockPos);
                            }


                            // Normal
                            Vector3 normal = VectorDirections[n];

                            for (int i = 0; i < 4; i++)
                                normalList.Add(rotateMat * normal);

                            // UV
                            foreach (Vector2 uv in GetFaceUVs(VectorDirections[n]))
                                uvList.Add(new Vector4(uv.x, uv.y, 4294967296f, 0.75f));

                            // Vertex Color
                            int borderIndex = 0;
                            switch (n)
                            {
                                case 0: // -X
                                    borderIndex += 1 * air_pY;
                                    borderIndex += 2 * air_pZ;
                                    borderIndex += 4 * air_nY;
                                    borderIndex += 8 * air_nZ;
                                    break;
                                case 1: // +X
                                    borderIndex += 1 * air_pY;
                                    borderIndex += 2 * air_nZ;
                                    borderIndex += 4 * air_nY;
                                    borderIndex += 8 * air_pZ;
                                    break;
                                case 2: // -Y
                                    borderIndex += 1 * air_nX;
                                    borderIndex += 2 * air_pZ;
                                    borderIndex += 4 * air_pX;
                                    borderIndex += 8 * air_nZ;
                                    break;
                                case 3: // +Y
                                    borderIndex += 1 * air_nX;
                                    borderIndex += 2 * air_nZ;
                                    borderIndex += 4 * air_pX;
                                    borderIndex += 8 * air_pZ;
                                    break;
                                case 4: // -Z
                                    borderIndex += 1 * air_pY;
                                    borderIndex += 2 * air_nX;
                                    borderIndex += 4 * air_nY;
                                    borderIndex += 8 * air_pX;
                                    break;
                                case 5: // +Z
                                    borderIndex += 1 * air_pY;
                                    borderIndex += 2 * air_pX;
                                    borderIndex += 4 * air_nY;
                                    borderIndex += 8 * air_nX;
                                    break;
                            }


                            int edgeIndex = 0;
                            if (GetVoxelLeft(neighborVoxels, dir).BlockID == BlockID.StoneBricks)
                            {
                                edgeIndex = 1;
                            } else if (GetVoxelRight(neighborVoxels, dir).BlockID == BlockID.StoneBricks)
                            {
                                edgeIndex = 2;
                            }

                            int textureIndex = textureIndices[n];
                            Color c = new Color(textureIndex, edgeIndex, damage, borderIndex);
                            if (voxel.BlockID == BlockID.Color_Block)
                            {
                                c = voxel.VertexColor;
                            }
                            colorList.AddRange(new Color[4] { c, c, c, c });


                            // Triangles
                            for (int i = 0; i < 6; i++)
                                triangleList.Add(t + Triangles[i]);
                            t += 4;
                        }
                    }
                    break;

                case BlockShape.HalfSlab:
                    for (int n = 0; n < 6; n++)
                    {
                        //if ((BlockShape)neighbors[n] != BlockShape.Solid || n==3)
                        if (!IsNeighborFaceSolid(OrthoDirections[n], nbVoxels[n]) || !IsFaceSolid(OrthoDirections[n], voxel))
                        {
                            foreach (Vector3 v in GetFaceVerts(VectorDirections[n]))
                            {
                                Vector3 slabV = v;
                                slabV.y = Mathf.Clamp(v.y, -1f, 0f);
                                slabV = rotateMat * slabV;
                                vertList.Add(slabV * 0.5f + blockPos);
                            }

                            Vector3 normal = VectorDirections[n];
                            for (int i = 0; i < 4; i++)
                                normalList.Add(rotateMat * normal);

                            foreach (Vector2 uv in GetFaceUVs(VectorDirections[n]))
                            {
                                Vector2 slabUV = uv;
                                if (n != 2 && n != 3)
                                    slabUV.y = Mathf.Clamp(slabUV.y, 0.5f, 1f);
                                uvList.Add(slabUV);
                            }

                            Color c = new Color((int)voxel.BlockID, n, (float)voxel.Damage / (float)voxel.Toughness);
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
    }
    
    
    public static Vector3[] GetVertices(BlockShape shape)
    {
        if (shape == BlockShape.Solid) { 
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
        else if (shape == BlockShape.HalfSlab)
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
    public bool IsFaceSolid(OrthoNormal dir, Voxel voxel)
    {
        switch (voxel.Shape)
        {
            case BlockShape.Empty:
                return false;
            case BlockShape.Solid:
                return true;
            case BlockShape.HalfSlab:
                OrthoNormal slabBottom = voxel.UpAxis.Flip();
                return dir.IsEqual(slabBottom);
        }
        return false;
    }
    public bool IsFaceSolid(Direction dir, Voxel voxel)
    {
        switch (voxel.Shape)
        {
            case BlockShape.Empty:
                return false;
            case BlockShape.Solid:
                return true;
            case BlockShape.HalfSlab:
                Direction slabBottom = DirectionHelper.Flip(voxel.UpAxis.AsDirection());
                return dir == slabBottom;
        }
        return false;
    }

    public bool IsNeighborFaceSolid(OrthoNormal dirFromNb, Voxel nbVoxel)
    {
        switch (nbVoxel.Shape)
        {
            case BlockShape.Empty:
                return false;
            case BlockShape.Solid:
                return true;
            case BlockShape.HalfSlab:
                OrthoNormal dir = dirFromNb.Flip();
                OrthoNormal slabBottom = nbVoxel.UpAxis.Flip();
                bool match = dir.IsEqual(slabBottom);
                //Debug.Log($"dir to nb: {dir}, slab bottom dir: {slabBottom}, match: {match}");
                return match;
        }

        return false;
    }
    public bool IsNeighborFaceSolid(Direction dirFromNb, Voxel nbVoxel)
    {
        switch (nbVoxel.Shape)
        {
            case BlockShape.Empty:
                return false;
            case BlockShape.Solid:
                return true;
            case BlockShape.HalfSlab:
                Direction dir = DirectionHelper.Flip(dirFromNb);
                Direction slabBottom = DirectionHelper.Flip(nbVoxel.UpAxis.AsDirection());
                bool match = dir == slabBottom;
                //Debug.Log($"dir to nb: {dir}, slab bottom dir: {slabBottom}, match: {match}");
                return match;
        }

        return false;
    }
    public bool DoAdjacentIDsMatch(BlockID id1, BlockID id2) {
        if (id1 != BlockID.Air) return true; // TEMP

        if (id1 == BlockID.Dirt)
            return id2 == BlockID.Grass || id2 == BlockID.Dirt || id2 == BlockID.Log;
        if (id1 == BlockID.Grass)
            return id2 == BlockID.Grass || id2 == BlockID.Dirt;
        if (id1 == BlockID.Log)
            return id2 == BlockID.Dirt || id2 == BlockID.Leaves || id2 == BlockID.Log;
        else if (id1 != BlockID.Air)
            return true;
            //return true;
        return id1 == id2;
    }

    public Voxel GetVoxelLeft(Voxel[] neighbors, Vector3 faceNormal)
    {
        if (faceNormal == Vector3.left) // -X face
        {
            return neighbors[5]; // +Z neighbor is left
        }
        if (faceNormal == Vector3.right) // +X face
        {
            return neighbors[4]; // -Z neighbor is left
        }
        if (faceNormal == Vector3.back) // -Z face
        {
            return neighbors[0]; // -X neighbor is left
        }
        if (faceNormal == Vector3.forward) // +Z face
        {
            return neighbors[1]; // +X neighbor is left
        }
        if (faceNormal == Vector3.down || faceNormal == Vector3.up) // -Y face or +Y face
        {
            return neighbors[1]; // +X neighbor is left
        }
        return new Voxel(BlockID.Invalid);
    }
    public Voxel GetVoxelRight(Voxel[] neighbors, Vector3 faceNormal)
    {
        if (faceNormal == Vector3.left) // -X face
        {
            return neighbors[4]; // -Z neighbor is right
        }
        if (faceNormal == Vector3.right) // +X face
        {
            return neighbors[5]; // +Z neighbor is right
        }
        if (faceNormal == Vector3.back) // -Z face
        {
            return neighbors[1]; // +X neighbor is right
        }
        if (faceNormal == Vector3.forward) // +Z face
        {
            return neighbors[1]; // +X neighbor is right
        }
        if (faceNormal == Vector3.down || faceNormal == Vector3.up) // -Y face or +Y face
        {
            return neighbors[0]; // -X neighbor is right
        }
        return new Voxel(BlockID.Invalid);
    }
    public Voxel GetVoxelUp(Voxel[] neighbors, Vector3 faceNormal)
    {
        // +/- X or +/- Z face
        if (faceNormal == Vector3.left || faceNormal == Vector3.right || faceNormal == Vector3.back || faceNormal == Vector3.forward)
        {
            return neighbors[2]; // +Y neighbor is up
        }
        if (faceNormal == Vector3.down) // -Y face
        {
            return neighbors[5]; // +Z neighbor is up
        }
        if (faceNormal == Vector3.up) // +Y face
        {
            return neighbors[4]; // -Z neighbor is up
        }
        return new Voxel(BlockID.Invalid);
    }
    public Voxel GetVoxelDown(Voxel[] neighbors, Vector3 faceNormal)
    {
        // +/- X or +/- Z face
        if (faceNormal == Vector3.left || faceNormal == Vector3.right || faceNormal == Vector3.back || faceNormal == Vector3.forward)
        {
            return neighbors[3]; // -Y neighbor is down
        }
        if (faceNormal == Vector3.down) // -Y face
        {
            return neighbors[4]; // -Z neighbor is down
        }
        if (faceNormal == Vector3.up) // +Y face
        {
            return neighbors[5]; // +Z neighbor is down
        }
        return new Voxel(BlockID.Invalid);
    }

    public Voxel GetVoxelLeft(Voxel[] neighbors, Direction dir)
    {
        if (dir == Direction.NegativeX) 
            return neighbors[(int)Direction.PositiveZ];
        if (dir == Direction.PositiveX)
            return neighbors[(int)Direction.NegativeZ]; 
        if (dir == Direction.NegativeZ) 
            return neighbors[(int)Direction.NegativeX]; 
        if (dir == Direction.PositiveZ) 
            return neighbors[(int)Direction.PositiveX]; 
        if (dir == Direction.NegativeY || dir == Direction.PositiveY) 
            return neighbors[(int)Direction.PositiveX]; 

        return new Voxel(BlockID.Invalid);
    }
    public Voxel GetVoxelRight(Voxel[] neighbors, Direction dir)
    {
        if (dir == Direction.NegativeX) 
            return neighbors[(int)Direction.NegativeZ]; 
        if (dir == Direction.PositiveX) 
            return neighbors[(int)Direction.PositiveZ]; 
        if (dir == Direction.NegativeZ) 
            return neighbors[(int)Direction.PositiveX]; 
        if (dir == Direction.PositiveZ)
            return neighbors[(int)Direction.PositiveX]; 
        if (dir == Direction.NegativeY || dir == Direction.PositiveY) 
            return neighbors[(int)Direction.NegativeX]; 
        return new Voxel(BlockID.Invalid);
    }
    public Voxel GetVoxelUp(Voxel[] neighbors, Direction dir)
    {

        if (dir == Direction.NegativeX || dir == Direction.PositiveX || dir == Direction.NegativeZ || dir == Direction.PositiveZ)
            return neighbors[(int)Direction.PositiveY];
        if (dir == Direction.NegativeY)
            return neighbors[(int)Direction.PositiveZ]; 
        if (dir == Direction.PositiveY) 
            return neighbors[(int)Direction.NegativeZ];
        return new Voxel(BlockID.Invalid);
    }
    public Voxel GetVoxelDown(Voxel[] neighbors, Direction dir)
    {

        if (dir == Direction.NegativeX || dir == Direction.PositiveX || dir == Direction.NegativeZ || dir == Direction.PositiveZ)
            return neighbors[(int)Direction.NegativeY]; 
        if (dir == Direction.NegativeY) 
            return neighbors[(int)Direction.NegativeZ];
        if (dir == Direction.PositiveY) 
            return neighbors[(int)Direction.PositiveZ]; 
        return new Voxel(BlockID.Invalid);
    }
}
