using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField]
    private List<ItemData> items;

    private int hotbarIndex = 0;

    public HotbarUI Hotbar;

    public ItemData SetSlot(int index)
    {
        hotbarIndex = index;
        Hotbar.SetSlot(index);
        if (items.Count > index)
        {
            return items[hotbarIndex];
        }
        else
        {
            return null;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
