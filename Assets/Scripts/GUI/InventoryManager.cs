using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public PlayerInput input;
    public PlayerView viewController;
    public CanvasGroup canvasGroup;
    public GameObject hotbarCursor;
    public Item equippedItem;
    public Item nullItem;
    public RadialMenu radial;
    public TextMeshProUGUI itemNameText;
    public GameObject itemTilePrefab;

    public List<ItemStack> InitialStacks = new List<ItemStack>(60);

    private ItemTile mouseTile;
    private InventoryCell lastCell;
    private Vector3 mousePos;
    public bool hasTile = false;
    private int hotbarSlot;

    private bool hotbarDirty = false;

    private ItemTile hoveredTile;
    public RectTransform hoverWindow;


    public List<InventoryCell> InventoryCells;
    public List<InventoryCell> HotbarCells;

    private void Awake()
    {
        InventoryCells = GetComponentsInChildren<InventoryCell>().ToList<InventoryCell>();
        nullItem = new Item();
        equippedItem = nullItem;
        
    }
    private void Start()
    {
        LoadInitialStacks();
    }

    private void LoadInitialStacks()
    {
        InitialStacks.Add(new ItemStack(ItemID.Tool_Pickaxe_Iron, 1));
        for (int i=0; i < InitialStacks.Count; i++)
        {
            if (InitialStacks[i] != null)
            {
                Item itemData = ItemRegistry.LookupItem(InitialStacks[i].ItemID);
                Debug.Log(itemData.Name);

                ItemTile newItemTile = Instantiate(itemTilePrefab).GetComponent<ItemTile>();

                newItemTile.Stack = InitialStacks[i];
                newItemTile.Item = itemData;
                newItemTile.transform.SetParent(InventoryCells[i].transform, false);
                newItemTile.transform.localPosition = Vector3.zero;

                InventoryCells[i].FindTile();
                newItemTile.InitializeTile();
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

        if (cellTile.Item.ItemID != mouseTile.Item.ItemID)
        {
            mouseTile = null;
            PickupTile(lastCell, cellTile);
            return tileToDrop;
        } else
        {
            tileToDrop.SetCount(tileToDrop.GetCount() + cellTile.GetCount());
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


    }

    public void Close()
    {
        //Debug.Log("inventory close");
        if (mouseTile != null)
            lastCell.ClickCell();
        canvasGroup.alpha = 0;
        hoverWindow.GetComponent<CanvasGroup>().alpha = 0f;
        hoveredTile = null;

    }
    public void Open()
    {
        //Debug.Log("inventory open");
        canvasGroup.alpha = 1;

    }

    public void UpdateEquippedItem()
    {
        ItemTile tile = HotbarCells[hotbarSlot].GetTile();

        if (tile != null && equippedItem != null) {
            itemNameText.text = tile.Item.Name;
           // Debug.Log($"equipped: {equippedItem.Name}, new: {tile.Item.Name} (from slot {hotbarSlot})");
            if (equippedItem.Name != tile.Item.Name)
            {
                equippedItem = tile.Item;
                viewController.UpdateEquippedItem(equippedItem);
            }
        } else
        {
            itemNameText.text = "";
           // Debug.Log($"equipped: {equippedItem.Name}, new: nullItem (from slot {hotbarSlot})"); ;
            equippedItem = nullItem;
            viewController.UpdateEquippedItem(equippedItem);
        }

    }

    public Item SelectHotbarSlot(int hotbarIndex)
    {
        if (hotbarIndex < HotbarCells.Count-1)
        {
            hotbarCursor.transform.position = HotbarCells[hotbarIndex].transform.position;
            hotbarSlot = hotbarIndex;
            hotbarDirty = true;
            return equippedItem;
        }
        return nullItem;
    }

    public void StartCellHover(InventoryCell cell)
    {
        hoverWindow.GetComponent<CanvasGroup>().alpha = 1.0f;
        hoveredTile = cell.GetTile();
    }
    public void EndCellHover(InventoryCell cell)
    {
        hoverWindow.GetComponent<CanvasGroup>().alpha = 0f;
        hoveredTile = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (mouseTile != null) { 
            mouseTile.transform.position = mousePos;
        }
        if (hoveredTile != null)
        {
            hoverWindow.transform.position = mousePos;
        }
    }
}
