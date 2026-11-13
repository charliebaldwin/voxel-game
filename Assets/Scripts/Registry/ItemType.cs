using System.Collections.Generic;
using System.ComponentModel;

public class ItemType
{
    public string ID;
    public string Name;

    public List<ItemComponent> Components;
    public ItemDisplayAssets DisplayAssets;

    public ItemType()
    {
        ID = "null-item";
        Name = "Null Item";
        Components = new List<ItemComponent>();
        DisplayAssets = new ItemDisplayAssets();
    }
}
