using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using VInspector.Libs;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainer;
    [SerializeField] private Transform hotbarCursor;
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
                    Vector3 pos = slot.transform.position;
                    pos = new Vector3(pos.x.Ceil(), pos.y.Ceil(), pos.z.Ceil());
                    hotbarCursor.transform.position = pos;
                    //slot.color = Color.white;
                }
                else
                {
                    //slot.color = Color.gray4;
                }
            }
        }
    }
    void Start()
    {
        SetSlot(selectedSlot);
    }

    void Update()
    {
        
    }
}
