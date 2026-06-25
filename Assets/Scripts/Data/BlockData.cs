using System;
using System.Reflection;
using UnityEngine;

//[Serializable]
public class BlockData : Item
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

    public BlockData(string Name, ItemID ItemID, string Tooltip, int StackSize, ItemType Type, Sprite GUIIcon, Mesh ViewmodelMesh, Material ViewmodelMat) : base (Name, ItemID, Tooltip, StackSize, Type, GUIIcon, ViewmodelMesh, ViewmodelMat)
    {

    }
    public BlockData(Item parent)
    {
        foreach (PropertyInfo property in parent.GetType().GetProperties())
        {
            if (property.CanWrite)
                property.SetValue(this, property.GetValue(parent, null), null);
        }
    }
}
 