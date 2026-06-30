using UnityEngine;
using UnityEngine.EventSystems;
using Evo.UI;

public class InventoryCell : MonoBehaviour
{
    [SerializeField] private ItemTile tile;
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private Tooltip tooltip;
    [SerializeField] private RectTransform tileContainer;
    private void Awake()
    {
        FindTile();
        tooltip = GetComponentInChildren<Tooltip>();
    }
    public void FindTile()
    {
        if (tile == null)
        {
            tile = gameObject.GetComponentInChildren<ItemTile>();
        }
        UpdateTooltip();
    }

    public void ClickCell()
    {
        //Debug.Log($"clicked {gameObject.name}");
        if (tile != null)
        { 
            if (inventory.PickupTile(this, tile))
            {
                tile = null;
                //Debug.Log("tile picked up by Inventory");

            }
            else
            {
                tile = inventory.SwapTile(this, tile);
                //Debug.Log("tile swapped with Inventory");
            }
        }
        else
        {
            tile = inventory.DropTileOnCell(this);
           // Debug.Log("Inventory tile dropped on cell");
        }
        if (tile != null)
        {
            tile.transform.SetParent(tileContainer);
            tile.transform.localPosition = Vector3.zero;
        }
        UpdateTooltip();
    }

    public ItemTile GetTile()
    {
        return tile;
    }

    public void ClearCell()
    {
        Destroy(tile.gameObject);
        tile = null;
        UpdateTooltip();
    }


    private void UpdateTooltip()
    {
        if (tile == null )
        {
            tooltip.enabled = false;
        } else
        {
            Item item = tile.GetItemData();
            tooltip.enabled = true;
            tooltip.tooltipPreset = inventory.GetTooltipPreset(item.Rarity).gameObject;
            tooltip.title = item.Name;
            tooltip.description = item.Tooltip;
            tooltip.icon = item.TooltipIcon;
        }
    }

    public void OnHoverBegin()
    {
        Debug.Log("hover started");
        if (tile != null)
            inventory.StartCellHover(this);
    }
    public void OnHoverEnd()
    {
        if (tile != null)
            inventory.EndCellHover(this);
    }

}
