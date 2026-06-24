using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public class ToolData : ItemData
{
    [Header("Block Info")]

    public int Strength;
    public float UseTime;

    public ToolData()
    {
        //Strength = 0;
        StackSize = 1;
    }

    public ToolData(ItemData parent)
    {
        foreach (PropertyInfo property in parent.GetType().GetProperties())
        {
            if (property.CanWrite)
                property.SetValue(this, property.GetValue(parent, null), null);
        }
        StackSize = 1;
    }
    
}
