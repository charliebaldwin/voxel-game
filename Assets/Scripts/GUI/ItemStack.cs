using UnityEngine;

[CreateAssetMenu(fileName = "ItemStack", menuName = "Scriptable Objects/ItemStack")]
public class ItemStack : ScriptableObject
{
    public ItemID ItemID;
    public int Count = 1;

    public int SetCount(int newCount)
    {
        int max = ItemRegistry.LookupItem(ItemID).StackSize;
        if (newCount < max)
        {
            Count = newCount;
            return 0;
        }
        else
        {
            Count = max;
            int remainder = newCount % max;
            return remainder;
        }
    }
    public ItemStack(ItemID id, int count)
    {
        ItemID = id;
        Count = count;
    }
}
