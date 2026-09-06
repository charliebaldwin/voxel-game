using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemStack itemStack;

    public void ConsumePickup(out ItemStack stack)
    {
        stack = itemStack;
        Destroy(gameObject);
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
