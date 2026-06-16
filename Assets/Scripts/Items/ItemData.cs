using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName = "Item Name";
    public ItemType type;
    public byte toolDamage = 1;
    public float toolUseTime = 1f;
    public int blockID = 0;
    public Texture2D mainTex;
    public Texture2D metalSmoothTex;
    public Texture2D emissiveTex;
  
}


public enum ItemType : byte
{
    Tool = 0,
    Block = 1
}