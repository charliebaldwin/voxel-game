using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VInspector;

public class ItemRegistry : MonoBehaviour
{
    public static ItemRegistry Instance;
    public List<ItemDataObject> ItemDataObjects;
    public Dictionary<ItemID, ItemData> Items;
    public string IDOFolderPath = "Items";

    private void Awake()
    {
        PopulateDictionary();
    }

    [Button]
    private void LoadObjectsFromPath()
    {
        ItemDataObject[] idoArray = Resources.LoadAll<ItemDataObject>(IDOFolderPath);
        ItemDataObjects = new List<ItemDataObject>(idoArray.Length);
        foreach (ItemDataObject ido in idoArray)
        {
            Debug.Log($"Loading IDO {ido.Data.Name} with ID {ido.Data.ItemID}");
            ItemDataObjects.Add(ido);
        }
    }

    [Button]
    private void PopulateDictionary()
    {
        Items = new Dictionary<ItemID, ItemData>();
        foreach (ItemDataObject ido in ItemDataObjects)
        {
            Debug.Log($"ItemDict: adding item - {ido.Data.Name} with ID {ido.Data.ItemID}");
            ItemData data = ido.Data;
            Items.Add(data.ItemID, data);

            Debug.Log(Items[data.ItemID]);
        }
    }
}
