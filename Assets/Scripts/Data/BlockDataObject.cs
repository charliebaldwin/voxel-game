using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockDataObject", menuName = "Scriptable Objects/BlockDataObject")]
public class BlockDataObject : ItemDataObject
{
    public new BlockData Data = new BlockData();

    //public new BlockDataStruct DataStruct = new BlockDataStruct();

}
