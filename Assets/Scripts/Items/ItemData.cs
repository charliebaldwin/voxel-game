using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName = "Item Name";
    public ItemType type;
    public byte toolDamage;
    public Texture2D mainTex;
    public Texture2D metalSmoothTex;
  
}


public enum ItemType : byte
{
    Tool = 0,
    Block = 1
}