using UnityEngine;

public class VoxelCursor : MonoBehaviour
{
    public Material CursorMaterial;
    public MeshRenderer MeshRenderer;
    public Color BlockColor;
    public Color ToolColor;

    public void MoveCursor(VoxelHitInfo hit, ItemCategory itemType)
    {

        if (hit.didHit)
        {
            transform.position = hit.voxelPos;
            if (hit.hitNormal != Vector3Int.zero)
            {
                transform.forward = hit.hitNormal;
            }

            if (hit.voxel.Shape == BlockShape.HalfSlab)
                transform.position += Vector3.down * 0.5f;
            if (itemType == ItemCategory.Block)
                transform.position += hit.hitNormal;

            switch (itemType)
            {
                case ItemCategory.Block:
                    MeshRenderer.enabled = true;
                    MeshRenderer.material.SetColor("_Color", BlockColor);
                    break;
                case ItemCategory.Tool:
                    MeshRenderer.enabled = true;
                    MeshRenderer.material.SetColor("_Color", ToolColor);
                    break;
                case ItemCategory.Null:
                    MeshRenderer.enabled = false;
                    break;
            }
        }
        else
        {
            MeshRenderer.enabled = false;
        }
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
