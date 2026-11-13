using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public abstract class ItemComponent { }

public class BlockItemComponent : ItemComponent
{
    public string BlockType;
}

public class ToolItemComponent : ItemComponent
{
    public int Strength;
    public float UseTime;
}

