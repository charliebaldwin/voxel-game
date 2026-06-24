using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "ItemDataObject", menuName = "Scriptable Objects/ItemDataObject")]
public class ItemDataObject : ScriptableObject
{
    public ItemData Data = new ItemData();
    
    //public ItemDataStruct DataStruct = new ItemDataStruct();


    //public string Name = "Item Name";
    //public ItemType Category;

    //// block settings
    //public int blockID = 0;
    //public BlockID Block;

    //// tool settings
    //public byte ToolDamage = 1;
    //public float ToolUseTime = 1f;

    //// rendering settings
    //public Sprite sprite;
    //public Mesh mesh;
    //public Material material;    
}


