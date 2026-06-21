using TMPro;
using UnityEngine;

public class ItemTile : MonoBehaviour
{
    public int ItemCount = 1;
    public int ItemID = 0;
    public ItemData ItemData;
    public TextMeshProUGUI countText;

    private void Awake()
    {
        //countText = GetComponent<TextMeshProUGUI>();
    }

    public void SetCount(int newCount)
    {
        ItemCount = newCount;
        countText.text = ItemCount.ToString();
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
