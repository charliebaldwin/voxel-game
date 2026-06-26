using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "Item_", menuName = "Scriptable Objects/ItemDataObject")]
public class ItemDataObject : ScriptableObject
{
    public Item Data = new Item();

}


