using UnityEngine;

public class PlayerViewmodel : MonoBehaviour
{
    [Header("References")]
    public Animator ViewmodelAnimator;
    public MeshFilter ItemMeshFilter;
    public MeshRenderer ItemMeshRenderer;

    [Header("Animation IDs")]
    public string EquipTrigger = "Equip";
    public string HitTrigger = "Hit";
    public string HitSpeed = "ToolSpeed";


    private ItemID lastItem = ItemID.NullItem;

    public void SetItemModel(ItemID id)
    {
        Item item = ItemRegistry.LookupItem(id);
        SetItemModel(item);   
    }
    public void SetItemModel(Item item)
    {
        if (item.ItemID == ItemID.NullItem)
        {
            ItemMeshFilter.mesh.Clear();
        }
        else
        {
            ItemMeshFilter.mesh = item.ViewmodelMesh;
            ItemMeshRenderer.material = item.ViewmodelMat;
            if (item.Type == ItemType.Block)
            {
                ItemMeshRenderer.material.SetInt("_TextureIndex", (int)item.BlockID);
            }
        }

        if (item.ItemID != lastItem)
        {
            ViewmodelAnimator.SetTrigger(EquipTrigger);
        }
        lastItem = item.ItemID;
    }

    public void PlayHitAnimation(float speed)
    {
        ViewmodelAnimator.SetFloat(HitSpeed, speed);
        ViewmodelAnimator.SetTrigger(HitTrigger);
    }
}
