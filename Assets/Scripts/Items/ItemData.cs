using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName = "Item Name";
    public ItemType type;

    // block settings
    public int blockID = 0;

    // tool settings
    public byte toolDamage = 1;
    public float toolUseTime = 1f;

    // rendering settings
    public Sprite sprite;
    public Mesh mesh;
    public Material material;    
}


public enum ItemType : byte
{
    Tool = 0,
    Block = 1
}