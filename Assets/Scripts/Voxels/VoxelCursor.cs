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

            //VoxelCursor.SetActive(true);
        }
        else
        {
            //VoxelCursor.SetActive(false);
        }
        //MeshRenderer.material.SetColor("_Color", Color.red);

        switch (itemType)
        {
            case ItemType.Block:
                MeshRenderer.material.SetColor("_Color", BlockColor);
                break;
            case ItemType.Tool:
                MeshRenderer.material.SetColor("_Color", ToolColor);
                break;
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
