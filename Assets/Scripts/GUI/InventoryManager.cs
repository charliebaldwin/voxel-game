using Evo.UI;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
//using VInspector.Libs;

public class InventoryManager : MonoBehaviour
{
    public PlayerController Player;
    public CanvasGroup canvasGroup;
    public GameObject hotbarCursor;
    public Item equippedItem;
    public Item nullItem;
    public RadialMenu radial;
    public TextMeshProUGUI itemNameText;
    public GameObject itemTilePrefab;

    public List<ItemStack> InitialStacks = new List<ItemStack>(60);
    public List<ItemStack> InitialHotbarStacks = new List<ItemStack>(9);

    private ItemTile mouseTile;
    private InventoryCell lastCell;
    private Vector3 mousePos;
    public bool hasTile = false;
    private int hotbarSlot;

    private bool hotbarDirty = false;

    private ItemTile hoveredTile;
    public RectTransform hoverWindow;
    public HoverWindow HoverWindow;


    public List<InventoryCell> InventoryCells;
    public List<InventoryCell> HotbarCells;

    public List<TooltipPreset> tooltipPresets;
    public List<StylerPreset> tooltipStylerPresets;

    public const int NUM_HOTBAR_SLOTS = 10;

    public bool IsOpen = false;


    private void Awake()
    {
        InventoryCells = GetComponentsInChildren<InventoryCell>().ToList<InventoryCell>();
        nullItem = new Item();
        equippedItem = nullItem;
        
    }
    private void Start()
    {
        LoadInitialStacks();
        Close();
    }

    private void LoadInitialStacks()
    {
        //InitialStacks.Add(new ItemStack(ItemID.Tool_Pickaxe_Iron, 1));
        for (int i=0; i < InitialStacks.Count; i++)
        {
            if (InitialStacks[i] != null)
            {
                Item itemData = ItemRegistry.LookupItem(InitialStacks[i].ItemID);
                Debug.Log(itemData.Name);

                ItemTile newItemTile = Instantiate(itemTilePrefab).GetComponent<ItemTile>();
                newItemTile.Stack = InitialStacks[i];

                InventoryCells[i].GiveTile(newItemTile);
            }
        }
        for (int i = 0; i < InitialHotbarStacks.Count; i++)
        {
            if (InitialHotbarStacks[i] != null)
            {
                Item itemData = ItemRegistry.LookupItem(InitialStacks[i].ItemID);
                Debug.Log(itemData.Name);

                ItemTile newItemTile = Instantiate(itemTilePrefab).GetComponent<ItemTile>();
                newItemTile.Stack = InitialStacks[i];

                HotbarCells[i].GiveTile(newItemTile);
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

        if (cellTile.GetItemData().ItemID != mouseTile.GetItemData().ItemID)
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
    public void SetMousePos(Vector3 pos)
    {
        mousePos = pos;
    }
    public void OnScroll(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log($"scroll={context.ReadValue<float>()}");
            int delta = Mathf.RoundToInt(context.ReadValue<float>());
            int newHotbarSlot = hotbarSlot + delta;
            newHotbarSlot = Mathf.Clamp(newHotbarSlot, 0, NUM_HOTBAR_SLOTS - 1);
            hotbarSlot = newHotbarSlot;
            SelectHotbarSlot(newHotbarSlot);
            //Debug.Log($"slot={hotbarSlot}");
        }
    }

    public void Close()
    {
        //Debug.Log("inventory close");
        if (mouseTile != null)
            lastCell.ClickCell();
        canvasGroup.alpha = 0;
        IsOpen = false;
        //HoverWindow.HideWindow();
       // hoveredTile = null;

    }
    public void Open()
    {
        //Debug.Log("inventory open");
        canvasGroup.alpha = 1;
        IsOpen=true; 
    }

    public void UpdateEquippedItem()
    {
        ItemTile tile = HotbarCells[hotbarSlot].GetTile();

        if (tile != null && equippedItem != null) {
            itemNameText.text = tile.GetItemData().Name;
            if (equippedItem.Name != tile.GetItemData().Name)
            {
                equippedItem = tile.GetItemData();
                Player.SetEquippedItem(equippedItem.ItemID);
                //viewController.UpdateEquippedItem(equippedItem);
            }
        } else
        {
            itemNameText.text = "";
            equippedItem = nullItem;
            Player.SetEquippedItem(equippedItem.ItemID);
            //viewController.UpdateEquippedItem(equippedItem);
        }

    }

    public Item SelectHotbarSlot(int hotbarIndex)
    {
        Debug.Log($"hotbar index = {hotbarIndex}");
        if (hotbarIndex < HotbarCells.Count-1)
        {
            hotbarCursor.transform.position = HotbarCells[hotbarIndex].transform.position;
            hotbarSlot = hotbarIndex;
            hotbarDirty = true;
            UpdateEquippedItem();
            Debug.Log($"hotbar slot = {hotbarSlot}");

            return equippedItem;
        }
        return nullItem;
    }

    public void StartCellHover(InventoryCell cell)
    {
        //hoveredTile = cell.GetTile();
        //HoverWindow.SetItemText(hoveredTile.GetItemData());
       // HoverWindow.ShowWindow();

    }
    public void EndCellHover(InventoryCell cell)
    {
       // HoverWindow.HideWindow();
       // hoveredTile = null;
    }

    public TooltipPreset GetTooltipPreset(ItemRarity rarity)
    {
        return tooltipPresets[(int)rarity];
    }

    // Update is called once per frame
    void Update()
    {
        if (mouseTile != null) { 
            mouseTile.transform.position = mousePos;
        }
        if (hoveredTile != null)
        {
           // hoverWindow.transform.position = mousePos + Vector3.forward * 0.3f;
        }
    }
}
