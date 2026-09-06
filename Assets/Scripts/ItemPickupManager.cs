using UnityEngine;

public class ItemPickupManager : MonoBehaviour
{

    public GameObject itemPickupPrefab;


    public void SpawnItemPickup(Vector3 worldPosition, ItemID id, int count)
    {
        ItemPickup newItem = Instantiate(itemPickupPrefab, worldPosition, Quaternion.identity).GetComponent<ItemPickup>();
        newItem.itemStack = new ItemStack(id, count);
        newItem.transform.SetParent(transform, true);

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
