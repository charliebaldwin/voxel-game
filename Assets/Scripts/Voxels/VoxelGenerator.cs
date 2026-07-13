using System;
using UnityEngine;

public class VoxelGenerator
{
    private Vector3Int worldSize;
    private WorldGenSettings worldSettings;
    private Voxel[,,] voxels;
    public VoxelGenerator(Vector3Int worldVoxelSize, WorldGenSettings worldGenSettings)
    {
        worldSize = worldVoxelSize;
        worldSettings = worldGenSettings;
        voxels = new Voxel[worldSize.x, worldSize.y, worldSize.z];

        LoopXZ(TerrainNoise);
        LoopXYZ(AddGrass);
        LoopXYZ(CarveCaves);
    }

    public Voxel[,,] GetGeneratedVoxels()
    {
        return voxels;
    }

    #region GET/SET
    private bool SetVoxel(int x, int y, int z, Voxel voxel)
    {
        if (x < worldSize.x - 1 && y < worldSize.y - 1 && z < worldSize.z - 1 && x > 0 && y > 0 && z > 0)
        {
            voxels[x, y, z] = voxel;
            return true;
        }
        else
        {
            return false;
        }
    }
    private Voxel GetVoxel(int x, int y, int z)
    {
        if (x < worldSize.x - 1 && y < worldSize.y - 1 && z < worldSize.z - 1 && x > 0 && y > 0 && z > 0)
        {
            return voxels[x, y, z];
        }
        else
        {
            return new Voxel(BlockID.Invalid, 0, 0);
        }
    }
    #endregion

    #region LOOPS
    private void LoopXYZ(Action<int, int, int> loopFunction)
    {
        Vector3Int Size3D = new Vector3Int(worldSize.x, worldSize.y, worldSize.z);
        for (int x = 0; x < Size3D.x; x++) { 
            for (int z = 0; z < Size3D.z; z++) {
                for (int y = 0; y < Size3D.y; y++)
                {
                    loopFunction(x, y, z);
                }
            }
        }
    }
    private void LoopXZ(Action<int, int> loopFunction)
    {
        Vector2Int Size2D = new Vector2Int(worldSize.x, worldSize.z);
        for (int x = 0; x < Size2D.x; x++) {
            for (int z = 0; z < Size2D.y; z++)
            {
                loopFunction(x, z);
            }
        }
    }
    #endregion

    #region PASSES 

    private void TerrainNoise (int x, int z)
    {
        float noise = Perlin.Fbm(x * worldSettings.NoiseScale, z * worldSettings.NoiseScale, worldSettings.NoiseOctaves);
        float noise2 = Perlin.Fbm(x * worldSettings.NoiseScale * 0.2f, z * worldSettings.NoiseScale * 0.2f, worldSettings.NoiseOctaves);
        float h = noise * worldSettings.HeightRange + worldSettings.HeightOffset;
        h = h + (noise2 * worldSettings.HeightRange * 4);

        for (int y = 0; y < worldSize.y; y++)
        {
            float diff = h - y;
            if (diff < 0f)
                SetVoxel(x, y, z, new Voxel(BlockID.Air, 0, 0, BlockShape.Empty));
            else if (diff < 4f)
                SetVoxel(x, y, z, new Voxel(BlockID.Dirt, 0, 0, BlockShape.Solid));
            else if (diff < 8f)
                SetVoxel(x, y, z, new Voxel(BlockID.Rocky_Dirt, 0, 0, BlockShape.Solid));
            else if (diff < 15f)
                SetVoxel(x, y, z, new Voxel(BlockID.Stone_Sandstone, 0, 0, BlockShape.Solid));
            else if (diff < 22f)
                SetVoxel(x, y, z, new Voxel(BlockID.Stone_Limestone, 0, 0, BlockShape.Solid));
            else if (diff < 30f)
                SetVoxel(x, y, z, new Voxel(BlockID.Stone_Dolomite, 0, 0, BlockShape.Solid));
            else if (diff < 40f)
                SetVoxel(x, y, z, new Voxel(BlockID.Stone_Shale, 0, 0, BlockShape.Solid));
            else if (diff < 55f)
                SetVoxel(x, y, z, new Voxel(BlockID.Stone_Slate, 0, 0, BlockShape.Solid));
            else if (diff < 70f)
                SetVoxel(x, y, z, new Voxel(BlockID.Stone_Basalt, 0, 0, BlockShape.Solid));
        }
    }

    private void AddGrass(int x, int y, int z)
    {
        if (GetVoxel(x, y, z).BlockID == BlockID.Dirt && GetVoxel(x, y + 1, z).BlockID == BlockID.Air)
        {
            SetVoxel(x, y, z, new Voxel(BlockID.Grass));
            //SetVoxel(x, y-1, z, new Voxel(BlockID.Dirt));
            //SetVoxel(x, y-2, z, new Voxel(BlockID.Dirt));

        }
    }

    private void CarveCaves(int x, int y, int z) 
    {
        if (y < 2 || GetVoxel(x,y,z).BlockID == BlockID.Air) return;
        float noise = Perlin.Fbm(x * worldSettings.NoiseScale * 1.4f, y * worldSettings.NoiseScale *1.4f, z * worldSettings.NoiseScale * 1.4f, 2);
        if (noise < -0.1f)
        {
            SetVoxel(x, y, z, new Voxel(BlockID.Air));
        }

    }

    #endregion

}
