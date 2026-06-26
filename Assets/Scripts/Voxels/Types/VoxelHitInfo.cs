using UnityEngine;

public struct VoxelHitInfo
{
    public bool didHit;
    public BlockID blockID;
    public Voxel voxel;
    public Vector3Int voxelPos;
    public Vector3 hitPos;
    public Vector3Int hitNormal;
    public float distance;

    public VoxelHitInfo(bool didHit)
    {
        this.didHit = didHit;
        blockID = 0;
        voxel = new Voxel();
        voxelPos = Vector3Int.zero;
        hitPos = Vector3.zero;
        hitNormal = Vector3Int.up;
        distance = 0f;
    }
}