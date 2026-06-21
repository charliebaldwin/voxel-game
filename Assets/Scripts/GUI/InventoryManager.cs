using NUnit.Framework;
using System.Collections.Generic;
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

    private ItemTile mouseTile;
    private InventoryCell lastCell;
    private Vector3 mousePos;
    public bool hasTile = false;
    private int hotbarSlot;

    private bool hotbarDirty = false;
    

    public List<InventoryCell> HotbarCells;

    private void Awake()
    {
        equippedItem = nullItem;
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

        if (cellTile.ItemID != mouseTile.ItemID)
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
        mousePos = new Vector3(pos2D.x, pos2D.y, -5f);
        radial.SetAngle(pos2D);
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
            Debug.Log($"equipped: {equippedItem.itemName}, new: {tile.ItemData.itemName} (from slot {hotbarSlot})");
            if (equippedItem != tile.ItemData)
            {
                equippedItem = tile.ItemData;
                viewController.UpdateEquippedItem(equippedItem);
            }
        } else
        {
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
