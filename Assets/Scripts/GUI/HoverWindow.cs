using TMPro;
using UnityEngine;

public class HoverWindow : MonoBehaviour
{
    public CanvasGroup CanvasGroup;
    public TextMeshProUGUI LabelText;
    public void SetItemText(Item item)
    {
        LabelText.text = item.Name;
    }
    public void HideWindow()
    {
        CanvasGroup.alpha = 0f;
    }
    public void ShowWindow()
    {
        CanvasGroup.alpha = 1f;
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
