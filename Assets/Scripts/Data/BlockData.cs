using System;
using UnityEngine;

[Serializable]
public class BlockData
{
    public BlockID BlockID;
    public int Toughness = 6;
    public bool IsBlockEntity = false;
}

[CreateAssetMenu(fileName = "Block_", menuName = "Scriptable Objects/BlockDataObject")]
public class BlockDataObject : ScriptableObject
{
    public BlockData Data = new BlockData();
}

