using UnityEngine;

public class BlockEntityActor : MonoBehaviour
{
    public BlockEntityData Data;
    public Voxel VoxelData;
    public Vector3Int VoxelPosition;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }
    public void SetPosition()
    {
        transform.position = VoxelPosition - 0.5f * Vector3.up;
        transform.rotation = Quaternion.FromToRotation(Vector3.forward, VoxelData.ForwardAxis.ToVector());
        //transform.forward = VoxelData.ForwardAxis.ToVector();
        //transform.up = VoxelData.UpAxis.ToVector();
        Debug.Log($"entity up={transform.up}, fwd={transform.forward}");
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
