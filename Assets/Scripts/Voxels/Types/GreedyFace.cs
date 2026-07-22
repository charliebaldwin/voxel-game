using UnityEngine;
using static VoxelHelper;

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

        if (faceDirection == VectorDirections[0])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f - 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f - 0.5f);
            newVerts = new Vector3[4] {
                originVert,
                originVert + new Vector3(0f, -lengthSecondary, 0f),
                originVert + new Vector3(0f, -lengthSecondary, lengthPrimary),
                originVert + new Vector3(0f, 0f,            lengthPrimary),
            };
        }
        if (faceDirection == VectorDirections[1])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f + 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f - 0.5f);
            newVerts = new Vector3[4] {
                originVert + new Vector3(0f, 0f,            lengthPrimary),
                originVert + new Vector3(0f, -lengthSecondary, lengthPrimary),
                originVert + new Vector3(0f, -lengthSecondary, 0f),
                originVert
            };
        }
        else if (faceDirection == VectorDirections[2] || faceDirection == VectorDirections[3])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f - 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f - 0.5f);
            newVerts = new Vector3[4] {
                originVert + new Vector3(0f,            0f, lengthSecondary),
                originVert + new Vector3(lengthPrimary, 0f, lengthSecondary),
                originVert + new Vector3(lengthPrimary, 0f, 0f),
                originVert
            };
        }
        else if (faceDirection == VectorDirections[4])
        {
            Vector3 originVert = new Vector3(originVoxel.x * 1f - 0.5f, originVoxel.y * 1f + 0.5f, originVoxel.z * 1f - 0.5f);
            newVerts = new Vector3[4] {
                originVert + new Vector3(lengthPrimary, 0f,               0f),
                originVert + new Vector3(lengthPrimary, -lengthSecondary,   0f),
                originVert + new Vector3(0f,              -lengthSecondary,   0f),
                originVert
            };
        }
        else if (faceDirection == VectorDirections[5])
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