using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PlayerViewmodel : MonoBehaviour
{
    [Header("References")]
    public Animator ViewmodelAnimator;
    public MeshFilter ItemMeshFilter;
    public MeshRenderer ItemMeshRenderer;

    [Header("Materials")]
    public Material BlockMat;

    [Header("Animation IDs")]
    public string EquipTrigger = "Equip";
    public string HitTrigger = "Hit";
    public string HitSpeed = "ToolSpeed";

    private Vector2Int atlasSize;

    private ItemID lastItem = ItemID.NullItem;

    private void Awake()
    {
        atlasSize = Vector2Int.zero;
    }
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
            //ItemMeshRenderer.material = item.ViewmodelMat;
            if (item.Type == ItemType.Block)
            {
                ItemMeshRenderer.material = BlockMat;
                List<int> textureIndices = BlockRegistry.LookupBlockTextures(item.BlockID);
                Debug.Log(item.BlockID);

                if (atlasSize == Vector2Int.zero)
                {
                    atlasSize = BlockRegistry.GetAtlasSize();
                    Debug.Log($"atlas: {atlasSize.x}x{atlasSize.y}");
                }
                if (textureIndices.Count > 0)
                {
                    ItemMeshRenderer.material.SetInt("_TextureIndex_NX", textureIndices[0]);
                    ItemMeshRenderer.material.SetInt("_TextureIndex_PX", textureIndices[1]);
                    ItemMeshRenderer.material.SetInt("_TextureIndex_NY", textureIndices[2]);
                    ItemMeshRenderer.material.SetInt("_TextureIndex_PY", textureIndices[3]);
                    ItemMeshRenderer.material.SetInt("_TextureIndex_NZ", textureIndices[4]);
                    ItemMeshRenderer.material.SetInt("_TextureIndex_PZ", textureIndices[5]);

                    ItemMeshRenderer.material.SetInt("_Rows", atlasSize.y);
                    ItemMeshRenderer.material.SetInt("_Columns", atlasSize.x);
                }
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
