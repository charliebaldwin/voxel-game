using System;
using System.Reflection;
using UnityEngine;

//[Serializable]
public class ToolData : Item
{
    [Header("Tool Info")]

    public int Strength;
    public float UseTime;

    public ToolData()
    {
        //Strength = 0;
        StackSize = 1;
    }
    public ToolData(string Name, ItemID ItemID, string Tooltip, int StackSize, ItemType Type, Sprite GUIIcon, Mesh ViewmodelMesh, Material ViewmodelMat) : base(Name, ItemID, Tooltip, StackSize, Type, GUIIcon, ViewmodelMesh, ViewmodelMat)
    {

    }
    public ToolData(Item parent)
    {
        foreach (PropertyInfo property in parent.GetType().GetProperties())
        {
            if (property.CanWrite)
                property.SetValue(this, property.GetValue(parent, null), null);
        }
        StackSize = 1;
    }
    
}
