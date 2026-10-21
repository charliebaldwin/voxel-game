using UnityEngine;

public class MagicCarpet : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    private VoxelWorld world;

    [SerializeField] private GameObject planeNX;
    [SerializeField] private GameObject planePX;
    [SerializeField] private GameObject planeNY;
    [SerializeField] private GameObject planePY;
    [SerializeField] private GameObject planeNZ;
    [SerializeField] private GameObject planePZ;

    public int RaycastDistance = 2;

    private Vector3 lastPos = Vector3.zero;

    private void Awake()
    {
        world = VoxelWorld.Instance;
    }


    void SetPlane(GameObject plane, VoxelHitInfo hit)
    {
        if (hit.didHit && hit.voxel.Shape == BlockShape.Solid)
        {
            plane.SetActive(true);
            plane.transform.position = hit.voxelPos;
        }
        else
        {
            plane.SetActive(false);
        }
    }

    void Update()
    {
        if (world == null) world = VoxelWorld.Instance;


        Vector3 pos = player.transform.position;
        if (pos != lastPos)
        {
            VoxelHitInfo hitNX = world.VoxelTraversal(pos + 0.5f * Vector3.up + 0.5f * Vector3.right, Vector3.right, RaycastDistance);
            VoxelHitInfo hitPX = world.VoxelTraversal(pos + 0.5f * Vector3.up - 0.5f * Vector3.right, Vector3.left, RaycastDistance);
            VoxelHitInfo hitNZ = world.VoxelTraversal(pos + 0.5f * Vector3.up + 0.5f * Vector3.forward, Vector3.forward, RaycastDistance);
            VoxelHitInfo hitPZ = world.VoxelTraversal(pos + 0.5f * Vector3.up - 0.5f * Vector3.forward, Vector3.back, RaycastDistance);

            VoxelHitInfo hitNY = world.VoxelTraversal(pos + 1.5f * Vector3.up, Vector3.up, RaycastDistance);
            VoxelHitInfo hitPY = world.VoxelTraversal(pos + 0.0f * Vector3.up, Vector3.down, RaycastDistance);

            SetPlane(planeNX, hitNX);
            SetPlane(planePX, hitPX);
            SetPlane(planeNY, hitNY);
            SetPlane(planePY, hitPY);
            SetPlane(planeNZ, hitNZ);
            SetPlane(planePZ, hitPZ);
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 pos = player.transform.position;
        Gizmos.DrawRay(pos + 0.5f * Vector3.up, RaycastDistance * Vector3.left);
        Gizmos.DrawRay(pos + 0.5f * Vector3.up, RaycastDistance * Vector3.right);
        Gizmos.DrawRay(pos + 1.5f * Vector3.up, RaycastDistance * Vector3.up);
        Gizmos.DrawRay(pos + 0.0f * Vector3.up, RaycastDistance * Vector3.down);
        Gizmos.DrawRay(pos + 0.5f * Vector3.up, RaycastDistance * Vector3.back);
        Gizmos.DrawRay(pos + 0.5f * Vector3.up, RaycastDistance * Vector3.forward);
    }
}
