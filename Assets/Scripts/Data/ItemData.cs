using System;
using UnityEngine;

//[Serializable]
public class ItemData 
{
    [Header("General Info")]
    public string Name = "Item";
    public ItemID ItemID = ItemID.NullItem;
    public string Tooltip = "Item tooltip";
    public int StackSize = 999;
    public ItemType Type = ItemType.Default;

    [Header("Rendering")]
    public Sprite GUIIcon;
    public Mesh ViewmodelMesh;
    public Material ViewmodelMat;


}

public enum ItemType : byte
{
    Null,
    Default,
    Tool,
    Block,
}
