using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ItemTile : MonoBehaviour
{
    public int ItemCount = 1;
    public int ItemIDInt = 0;
    public ItemStack Stack;
    public TextMeshProUGUI countText;
    public Image ItemImage;

    private void Awake()
    {
        //ItemImage = GetComponent<Image>();
        //countText = GetComponent<TextMeshProUGUI>();
    }

    public void InitializeTile()
    {
        ItemImage.sprite = GetItemData().GUIIcon;
        //Debug.Log($"initializing tile with item {Item.Name}");
        UpdateCountText();
    }

    public void SetCount(int newCount)
    {
        int remainder = Stack.SetCount(newCount);
        //Debug.Log($"new count={newCount}, Remainder={remainder}");
        UpdateCountText();
    }
    public int GetCount()
    {
        return Stack.Count;
    }
    public void UpdateCountText()
    {
        int count = Stack.Count;
        countText.text = count == 1 ? "" : count.ToString();
    }
    public void AddCount (int count)
    {
        SetCount(Stack.Count + count);
    }

    public Item GetItemData()
    {
        return ItemRegistry.LookupItem(Stack.ItemID);
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
