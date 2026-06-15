using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainer;
    private int selectedSlot = 0;

    public void SetSlot(int index)
    {
        selectedSlot = index;
        foreach (Image slot in slotContainer.GetComponentsInChildren<Image>())
        {
            if (slot.transform.parent == slotContainer)
            {
                if (slot.transform.GetSiblingIndex() == selectedSlot)
                {
                    slot.color = Color.white;
                }
                else
                {
                    slot.color = Color.gray4;
                }
            }
        }
    }
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
