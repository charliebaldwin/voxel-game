using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    private CapsuleCollider collider;
    private PlayerController playerController;
    private InventoryManager inventory;
    private void Awake()
    {
        collider = GetComponent<CapsuleCollider>();
        playerController = GetComponent<PlayerController>();
        inventory = playerController.Inventory;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        ItemPickup item;
        if (other.TryGetComponent<ItemPickup>(out item))
        {
            ItemStack stack;
            item.ConsumePickup(out stack);
            Debug.Log($"item: {stack.ItemID} x{stack.Count}");
            inventory.AddItemStack(stack);
        }
    }
}
