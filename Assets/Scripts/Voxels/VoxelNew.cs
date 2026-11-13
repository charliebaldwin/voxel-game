using UnityEngine;
using VInspector.Libs;

public class VoxelNew
{
    public int BaseType;
    public VoxelState State;
    public int Damage;

    public VoxelNew(int blockBase, VoxelState state, int damage)
    {
        BaseType = blockBase;
        State = state;
        Damage = damage;
    }

    public VoxelNew(string blockBase)
    {
        BaseType = 0;// Registry.Instance.GetBlockBaseFromString(blockBase);
        State = VoxelState.Solid;
        Damage = 0;
    }
}

public enum VoxelState
{
    Invalid = -1,
    Empty = 0,
    Solid = 1,
    Permeable = 2,
    SemiSolid = 3
}

public enum VoxelBaseType
{
    None = 0,
    Sand = 10, Silt = 11, Clay = 12,
    Stone = 20, Granite = 21, Chert = 22, Limestone = 23, Sandstone = 24, Dolomite = 25,
    OakLog = 50, PineLog = 51, WalnutLog = 52, AshLog = 53,
    OakWood = 60, PineWood = 61, WalnutWood = 62, AshWood = 63
}

public enum VoxelTags
{

}