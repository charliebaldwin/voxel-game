using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static VoxelHelper;

public struct VoxelData 
{
    // data for voxels in world
    public int ID;
    public byte Damage;
    public byte Toughness;
    public byte Orientation;
    public byte BlockShape; // 0 = empty, 1 = full, 2 = slab, 3 = stairs

    public VoxelData(int id, byte damage, byte orientation)
    {
        ID = id;
        Damage = damage;
        Orientation = orientation;
        Toughness = 12;
        BlockShape = 1;
    }
    public VoxelData(int id, byte damage, byte orientation, byte blockShape)
    {
        ID = id;
        Damage = damage;
        Orientation = orientation;
        Toughness = 12;
        BlockShape = blockShape;
    }
}

public struct BlockData
{
    // data for different block types
    public int ID;
    public byte Toughness;
}



public struct VoxelHitInfo
{
    public bool didHit;
    public int blockID;
    public VoxelData voxel;
    public Vector3Int voxelPos;
    public Vector3 hitPos;
    public Vector3Int hitNormal;

    public VoxelHitInfo(bool didHit)
    {
        this.didHit = didHit;
        blockID = 0;
        voxel = new VoxelData();
        voxelPos = Vector3Int.zero;
        hitPos = Vector3.zero;
        hitNormal = Vector3Int.up;
    }
}

public class GreedyFace
{
    public Vector3Int faceDirection;
    public Vector3Int originVoxel;
    public int lengthPrimary;
    public int lengthSecondary;

    public GreedyFace(Vector3Int direction, Vector3Int origin)
    {
        faceDirection = direction;
        originVoxel = origin;
        lengthPrimary = 1;
        lengthSecondary = 1;
    }

    public Vector3[] GetFaceData()
    {

        Vector3[] newVerts = new Vector3[4] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };

        if (faceDirection == Directions[0])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f - 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f - 0.5f);
            newVerts = new Vector3[4] {
                originVert,
                originVert + new Vector3(0f, -lengthSecondary, 0f),
                originVert + new Vector3(0f, -lengthSecondary, lengthPrimary),
                originVert + new Vector3(0f, 0f,            lengthPrimary),
            };
        }
        if (faceDirection == Directions[1])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f + 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f - 0.5f);
            newVerts = new Vector3[4] {
                originVert + new Vector3(0f, 0f,            lengthPrimary),
                originVert + new Vector3(0f, -lengthSecondary, lengthPrimary),
                originVert + new Vector3(0f, -lengthSecondary, 0f),
                originVert
            };
        }
        else if (faceDirection == Directions[2] || faceDirection == Directions[3])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f - 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f - 0.5f);
            newVerts = new Vector3[4] {
                originVert + new Vector3(0f,            0f, lengthSecondary),
                originVert + new Vector3(lengthPrimary, 0f, lengthSecondary),
                originVert + new Vector3(lengthPrimary, 0f, 0f),
                originVert
            };
        }
        else if (faceDirection == Directions[4])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f - 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f - 0.5f);
            newVerts = new Vector3[4] {
                originVert + new Vector3(lengthPrimary, 0f,               0f),
                originVert + new Vector3(lengthPrimary, -lengthSecondary,   0f),
                originVert + new Vector3(0f,              -lengthSecondary,   0f),
                originVert
            };
        }
        else if (faceDirection == Directions[5])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f - 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f + 0.5f);
            newVerts = new Vector3[4] {
                originVert,
                originVert + new Vector3(0f,              -lengthSecondary,   0f),
                originVert + new Vector3(lengthPrimary, -lengthSecondary,   0f),
                originVert + new Vector3(lengthPrimary, 0f,               0f),
            };
        }

        return newVerts;
    }
}

public struct FaceData
{
    public Vector3[] vertices;
    public int[] triangles;
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
    public static bool IsSolid(VoxelData voxelData)
    {
        return (BlockShapes)voxelData.BlockShape == BlockShapes.SOLID;
    }

}

public enum BlockShapes
{
    EMPTY       = 0,
    SOLID       = 1,
    HALF_SLAB   = 2,
    STAIRS      = 3
}
