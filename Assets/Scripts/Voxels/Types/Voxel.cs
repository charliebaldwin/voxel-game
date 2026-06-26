using UnityEngine;
public struct Voxel
{
    // instance data for voxels in world
    public BlockID BlockID;
    public BlockShape Shape; // 0 = empty, 1 = full, 2 = slab, 3 = stairs
    public byte Damage;
    public byte Toughness;
    public byte Orientation;
    public OrthoNormal UpAxis;
    public OrthoNormal ForwardAxis;

    public Voxel(BlockID id, byte damage, byte orientation)
    {
        BlockID = id;
        Damage = damage;
        Orientation = orientation;
        Toughness = 12;
        Shape = BlockShape.Solid;

        UpAxis = OrthoNormal.up;
        ForwardAxis = OrthoNormal.forward;

    }
    public Voxel(BlockID id, byte damage, byte orientation, BlockShape blockShape)
    {
        BlockID = id;
        Damage = damage;
        Orientation = orientation;
        Toughness = 12;
        Shape = blockShape;

        UpAxis = OrthoNormal.up;
        ForwardAxis = OrthoNormal.forward;
    }
    public Voxel(BlockID id, byte damage, BlockShape blockShape, OrthoNormal upAxis, OrthoNormal fwdAxis)
    {
        BlockID = id;
        Damage = damage;
        Orientation = 1;
        Toughness = 12;
        Shape = blockShape;

        UpAxis = upAxis;
        ForwardAxis = fwdAxis;
    }
}