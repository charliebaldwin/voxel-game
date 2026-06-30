using System;
using UnityEngine;

[Serializable]
public class Item 
{
    [Header("Generic")]
    public string Name = "Item";
    public ItemType Type = ItemType.Default;
    public ItemID ItemID = ItemID.NullItem;
    public string Tooltip = "Item tooltip";
    public int StackSize = 999;
    public ItemRarity Rarity = ItemRarity.Common;


    [Header("Rendering")]
    public Sprite GUIIcon;
    public Mesh ViewmodelMesh;
    public Material ViewmodelMat;
    public Sprite TooltipIcon;

    [Header("Block Data")]
    public BlockID BlockID;
    public int TextureIndex;

    [Header("Tool Data")]
    public int Strength;
    public float UseTime;
    public ToolType ToolType= ToolType.None;

    public Item()
    {

    }
    public Item(string Name, ItemID ItemID, string Tooltip, int StackSize, ItemType Type, Sprite GUIIcon, Mesh ViewmodelMesh, Material ViewmodelMat)
    {
        this.Name = Name;
        this.ItemID = ItemID;
        this.Tooltip = Tooltip;
        this.StackSize = StackSize;
        this.Type = Type;
        this.GUIIcon = GUIIcon;
        this.ViewmodelMesh = ViewmodelMesh;
        this.ViewmodelMat = ViewmodelMat;
    }


}


