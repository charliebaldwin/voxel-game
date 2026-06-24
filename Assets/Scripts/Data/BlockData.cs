using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public class BlockData : ItemData
{
    [Header("Block Info")]

    //[SerializeField]
    public BlockID BlockID;

    //[SerializeField]
    public int TextureIndex;

    public BlockData()
    {
        StackSize = 999;
        TextureIndex = (int)BlockID;
    }
    public BlockData(ItemData parent)
    {
        foreach (PropertyInfo property in parent.GetType().GetProperties())
        {
            if (property.CanWrite)
                property.SetValue(this, property.GetValue(parent, null), null);
        }
    }
}
 