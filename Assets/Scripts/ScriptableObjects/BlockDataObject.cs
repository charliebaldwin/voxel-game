using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Block_", menuName = "Scriptable Objects/BlockDataObject")]
public class BlockDataObject : ScriptableObject
{
    public BlockData Data = new BlockData();
}