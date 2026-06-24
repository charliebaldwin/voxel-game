using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTile : MonoBehaviour
{
    public int ItemCount = 1;
    public int ItemID = 0;
    public ItemData Item;
    public TextMeshProUGUI countText;
    private Image tileImage;

    private void Awake()
    {
        tileImage = GetComponent<Image>();
        //countText = GetComponent<TextMeshProUGUI>();
    }

    public void InitializeTile()
    {
        tileImage.sprite = Item.GUIIcon;
        SetCount(ItemCount);
    }

    public void SetCount(int newCount)
    {
        ItemCount = newCount;
        countText.text = ItemCount == 1 ? "" : ItemCount.ToString();
    }
    public void AddCount (int count)
    {
        SetCount(ItemCount + count);
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
