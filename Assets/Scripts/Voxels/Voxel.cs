using UnityEngine;
public class Voxel
{
    // instance data for voxels in world
    //public BlockID BlockID;
    public int BlockType;
    public BlockShape Shape; // 0 = empty, 1 = full, 2 = slab, 3 = stairs
    public int Damage;
    public OrthoNormal UpAxis;
    public OrthoNormal ForwardAxis;
    public Color VertexColor;
    public bool Grassy;


    public Voxel(int blockType)
    {
        BlockType = blockType;
        Damage = 0;

        Shape = BlockShape.Solid;

        UpAxis = OrthoNormal.up;
        ForwardAxis = OrthoNormal.forward;
        VertexColor = Color.black;
        Grassy = false;
    }

    public Voxel(string id)
    {
        new Voxel(Registry.GetVoxelID(id));
    }
    public bool IsEmpty()
    {
        return BlockType == 0;
    }



    // public Voxel(BlockID id, byte damage, BlockShape blockShape, OrthoNormal upAxis, OrthoNormal fwdAxis)
    // {
    //     BlockID = id;
    //     Damage = damage;
    //     Orientation = 1;
    //     Toughness = 12;
    //     Shape = blockShape;

    //     UpAxis = upAxis;
    //     ForwardAxis = fwdAxis;
    //     VertexColor = Color.black;

    //     ExposedToSky = false;

    // }



}