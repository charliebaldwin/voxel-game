using UnityEngine;

public class BlockEntityActor : MonoBehaviour
{
    public BlockEntityData Data;
    public Vector3Int VoxelPosition;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }
    public void SetPosition(Vector3Int worldPosition)
    {
        VoxelPosition = worldPosition;
        transform.position = worldPosition - 0.5f * Vector3.up;
    }
    public void LoadEntity()
    {
        meshFilter.sharedMesh = Data.EntityMesh;
        meshRenderer.enabled = true;
        meshRenderer.material = Data.EntityMaterial;

    }
    public void UnloadEntity()
    {
        meshRenderer.enabled = false;
        
    }
}
