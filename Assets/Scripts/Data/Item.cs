using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class Item 
{
    [Header("Generic")]
    public string Name = "Item";
    public ItemID ItemID = ItemID.NullItem;
    public string Tooltip = "Item tooltip";
    public int StackSize = 999;
    public ItemRarity Rarity = ItemRarity.Common;


    [Header("Rendering")]
    public Sprite GUIIcon;
    public Mesh ViewmodelMesh;
    public Material ViewmodelMat;
    public Sprite TooltipIcon;

    [Title("Item Type")]
    public ItemType Type = ItemType.Default;
    [PropertySpace]

    [ShowIfGroup("Block Data", ItemType.Block, true)]
    public BlockID BlockID;
    public int TextureIndex;
    public BlockData BlockData;

    [ShowIfGroup("Tool Data", ItemType.Tool, true)]
    public int Strength = 1;
    public float UseTime = 1;
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


