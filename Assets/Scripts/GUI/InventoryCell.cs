using UnityEngine;

public class InventoryCell : MonoBehaviour
{
    [SerializeField] private ItemTile tile;

    [SerializeField] private InventoryManager inventory;
    public void ClickCell()
    {
        //Debug.Log($"clicked {gameObject.name}");
        if (tile != null)
        { 
            if (inventory.PickupTile(this, tile))
            {
                tile = null;
                Debug.Log("tile picked up by Inventory");

            }
            else
            {
                tile = inventory.SwapTile(this, tile);
                Debug.Log("tile swapped with Inventory");
            }
        }
        else
        {
            tile = inventory.DropTileOnCell(this);
            Debug.Log("Inventory tile dropped on cell");
        }
        if (tile != null)
        {
            tile.transform.SetParent(transform);
            tile.transform.localPosition = Vector3.zero;
        }
    }

    public ItemTile GetTile()
    {
        return tile;
    }

    public void ClearCell()
    {
        Destroy(tile.gameObject);
        tile = null;
    }
    public void FindTile()
    {
        if (tile == null)
        {
            tile = gameObject.GetComponentInChildren<ItemTile>();
        }
    }
    private void Awake()
    {
        FindTile();
    }
}
