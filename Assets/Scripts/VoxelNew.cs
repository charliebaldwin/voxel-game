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
        BaseType = Registry.Instance.GetBlockBaseFromString(blockBase);
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