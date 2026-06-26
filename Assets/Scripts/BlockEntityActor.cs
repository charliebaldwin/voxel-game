using UnityEngine;

public class BlockEntityActor : MonoBehaviour
{
    public BlockEntityData Data;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void LoadEntity(Vector3Int worldPosition)
    {
        meshFilter.sharedMesh = Data.EntityMesh;
        meshRenderer.material = Data.EntityMaterial;
        transform.position = worldPosition - 0.5f * Vector3.up;
    }
}
