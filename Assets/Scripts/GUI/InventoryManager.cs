using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public PlayerInput input;
    public PlayerView viewController;
    public CanvasGroup canvasGroup;
    public GameObject hotbarCursor;
    public ItemData equippedItem;
    public ItemData nullItem;
    public RadialMenu radial;
    public TextMeshProUGUI itemNameText;
    public GameObject itemTilePrefab;

    public List<ItemData> InitialItems = new List<ItemData>(60);

    private ItemTile mouseTile;
    private InventoryCell lastCell;
    private Vector3 mousePos;
    public bool hasTile = false;
    private int hotbarSlot;

    private bool hotbarDirty = false;


    public List<InventoryCell> InventoryCells;
    public List<InventoryCell> HotbarCells;

    private void Awake()
    {
        InventoryCells = GetComponentsInChildren<InventoryCell>().ToList<InventoryCell>();
        equippedItem = nullItem;
        LoadInitialItems();
    }

    private void LoadInitialItems()
    {
        for (int i=0; i < InitialItems.Count; i++)
        {
            if (InitialItems[i] != null)
            {
                ItemTile newItem = Instantiate(itemTilePrefab).GetComponent<ItemTile>();
                newItem.ItemData = InitialItems[i];
                newItem.transform.parent = InventoryCells[i].transform;
                newItem.transform.localPosition = Vector3.zero;
                InventoryCells[i].FindTile();
                newItem.InitializeTile();
            }
        }
    }

    public void LateUpdate()
    {
        if (hotbarDirty)
        {
            UpdateEquippedItem();
            hotbarDirty = false;
        }
    }
    public bool PickupTile(InventoryCell cell, ItemTile tile)
    {

        if (mouseTile == null)
        {
            lastCell = cell;
            mouseTile = tile;
            mouseTile.transform.position = mousePos;
            mouseTile.transform.SetParent(transform);
            hasTile = true;
            hotbarDirty = true;
            return true;
        }
        else
        {
            hotbarDirty = true;
            return false;
        }
    }

    public ItemTile DropTileOnCell(InventoryCell cell)
    {
        if (hasTile)
        {
            ItemTile droppedTile = mouseTile;
            mouseTile = null;
            hasTile = false;
            hotbarDirty = true;

            return droppedTile;
        }
        else
        {
            hotbarDirty = true;
            return null;
        }
    }

    public ItemTile SwapTile(InventoryCell cell, ItemTile cellTile)
    {

        ItemTile tileToDrop = mouseTile;

        if (cellTile.ItemData.blockID != mouseTile.ItemData.blockID)
        {
            mouseTile = null;
            PickupTile(lastCell, cellTile);
            return tileToDrop;
        } else
        {
            tileToDrop.AddCount(mouseTile.ItemCount);
            mouseTile = null;
            cell.ClearCell();
            return tileToDrop;
        }
    }

    public void OnMousePos(InputAction.CallbackContext context)
    {
        Vector2 pos2D = context.ReadValue<Vector2>();
        //Debug.Log(pos2D);
        mousePos = new Vector3(pos2D.x, pos2D.y, -5f);
<<<<<<< Updated upstream
        radial.SetAngle(pos2D);
=======

       // radial.SetAngle(pos2D);
>>>>>>> Stashed changes
    }

    public void Close()
    {
        //Debug.Log("inventory close");
        DropTileOnCell(lastCell);
        input.enabled = false;
        canvasGroup.alpha = 0;

    }
    public void Open()
    {
        //Debug.Log("inventory open");
        canvasGroup.alpha = 1;
        input.enabled = true;
    }

    public void UpdateEquippedItem()
    {
        ItemTile tile = HotbarCells[hotbarSlot].GetTile();

        if (tile != null) {
            itemNameText.text = tile.ItemData.itemName;
            Debug.Log($"equipped: {equippedItem.itemName}, new: {tile.ItemData.itemName} (from slot {hotbarSlot})");
            if (equippedItem != tile.ItemData)
            {
                equippedItem = tile.ItemData;
                viewController.UpdateEquippedItem(equippedItem);
            }
        } else
        {
            itemNameText.text = "";
            Debug.Log($"equipped: {equippedItem.itemName}, new: nullItem (from slot {hotbarSlot})"); ;
            equippedItem = nullItem;
            viewController.UpdateEquippedItem(equippedItem);
        }

    }

    public ItemData SelectHotbarSlot(int hotbarIndex)
    {
        if (hotbarIndex < HotbarCells.Count-1)
        {
            hotbarCursor.transform.position = HotbarCells[hotbarIndex].transform.position;
            hotbarSlot = hotbarIndex;
            hotbarDirty = true;
            return equippedItem;
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        if (mouseTile != null) { 
            mouseTile.transform.position = mousePos;
        }
    }
}
