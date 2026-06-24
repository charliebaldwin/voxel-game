using System;
using UnityEngine;

public struct ItemDataStruct
{
    public ItemType Type;
    public string Name;

    // block settings
    public int blockID;
    public BlockID Block;

    // tool settings
    public byte ToolDamage;
    public float ToolUseTime;

    // rendering settings
    public Sprite sprite;
    public Mesh mesh;
    public Material material;
}


