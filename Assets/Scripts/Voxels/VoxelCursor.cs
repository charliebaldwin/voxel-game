using UnityEngine;

public class VoxelCursor : MonoBehaviour
{
    public Material CursorMaterial;
    public MeshRenderer MeshRenderer;
    public Color BlockColor;
    public Color ToolColor;

    public void MoveCursor(VoxelHitInfo hit, ItemType itemType)
    {

        if (hit.didHit)
        {
            transform.position = hit.voxelPos;
            transform.forward = hit.hitNormal;

            if (hit.voxel.Shape == BlockShape.HalfSlab)
                transform.position += Vector3.down * 0.5f;
            if (itemType == ItemType.Block)
                transform.position += hit.hitNormal;

            switch (itemType)
            {
                case ItemType.Block:
                    MeshRenderer.enabled = true;
                    MeshRenderer.material.SetColor("_Color", BlockColor);
                    break;
                case ItemType.Tool:
                    MeshRenderer.enabled = true;
                    MeshRenderer.material.SetColor("_Color", ToolColor);
                    break;
                case ItemType.Null:
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
