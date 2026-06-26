using System;
using UnityEngine;

public struct BlockDataStruct
{
    // static data for different block types
    public BlockID Type;
    public string Name;
    public int ID;
    public byte Toughness;
}

public enum BlockID : short
{
    Invalid = -1,
    Air,
    Grass, Dirt, Stone, Planks, Log, Leaves, 
    StoneBricks, ClayBricks, Tiles_2x2, Tiles_1x2, Tiles_1x1, Tiles_1x1_Smooth
}
public static class Blocks
{
    public const int INVALID = -1;
    public const int AIR = 0;
    public const int DIRT = 2;
    public const int GRASS = 1;
    public const int STONE = 3;
    public const int GRANITE = 4;
    public const int WOOD = 5;
    public const int ORE = 6;
    public const int LOG = 7;
    public const int LEAVES = 8;

    public static bool IsSolid(int shape)
    {
        return (BlockShapes)shape == BlockShapes.SOLID;

    }
    public static bool IsSolid(Voxel voxelData)
    {
        return (BlockShapes)voxelData.BlockShape == BlockShapes.SOLID;
    }
}



public enum BlockShapes : byte
{
    EMPTY = 0,
    SOLID = 1,
    HALF_SLAB = 2,
    STAIRS = 3
}
