using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;

public class ItemRegistry : MonoBehaviour
{
    public static ItemRegistry Instance;
    public List<ItemDataObject> ItemDataObjects;
    public Dictionary<ItemID, Item> Items;
    public string IDOFolderPath = "Items";
    public static Item TestStaticItem;

    public List<Item> ItemList;
    public Dictionary<ItemID, Item> ItemDict = new Dictionary<ItemID, Item>();

    private void Awake()
    {
        SetInstance();
        LoadObjectsFromPath();
        PopulateDictionary();
    }
    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    public static Item LookupItem(ItemID id)
    {
        //Debug.Log($"looking up {id}");
        Item result = Instance.Items[id];
        return result;
    }
    public static ItemID GetBlockItem(BlockID blockID)
    {
        Debug.Log($"searching for item with blockID {blockID}");
        foreach(Item item in Instance.Items.Values)
        {
            if (item.Type == ItemType.Block)
            {
                if (item.BlockID == blockID)
                {
                    return item.ItemID;
                }
            }
        }
        Debug.Log($"Did not find item with blockID {blockID}");
        return ItemID.NullItem;
    }

    [Button]
    private void LoadObjectsFromPath()
    {
        ItemDataObject[] idoArray = Resources.LoadAll<ItemDataObject>(IDOFolderPath);
        ItemDataObjects = new List<ItemDataObject>(idoArray.Length);
        foreach (ItemDataObject ido in idoArray)
        {
            //Debug.Log($"Loading IDO {ido.Data.Name} with ID {ido.Data.ItemID}");
            ItemDataObjects.Add(ido);
        }
    }

    [Button]
    private void PopulateDictionary()
    {
        Items = new Dictionary<ItemID, Item>();
        foreach (ItemDataObject ido in ItemDataObjects)
        {
            //Debug.Log($"ItemDict: adding item - {ido.Data.Name} with ID {ido.Data.ItemID}");
            Item data = ido.Data;
            Items.Add(data.ItemID, data);

            //Debug.Log(Items[data.ItemID]);
        }
    }
}
