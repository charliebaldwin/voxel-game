using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MagicCarpet : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    private VoxelWorld world;

    [SerializeField] private GameObject planePrefab;

    //[SerializeField] private GameObject planeNX;
    //[SerializeField] private GameObject planePX;
    //[SerializeField] private GameObject planeNY;
    //[SerializeField] private GameObject planePY;
    //[SerializeField] private GameObject planeNZ;
    //[SerializeField] private GameObject planePZ;

    public List<Vector3> PlaneOrigins = new List<Vector3>();
    public List<Vector3> PlaneDirections = new List<Vector3>();
    private MagicCarpetPlane[] Planes;

    public int RaycastDistance = 2;

    private Vector3 lastPos = Vector3.zero;

    private void Awake()
    {
        world = VoxelWorld.Instance;

        //foreach (MagicCarpetPlane p in Planes)
        //{
        //    Destroy(p.gameObject);
        //}
        //Planes = new List<MagicCarpetPlane>();

        //for (int i=0; i<PlaneOrigins.Count; i++)
        //{
        //    Vector3 origin = PlaneOrigins[i] + 0.5f * PlaneDirections[i];
        //    GameObject newPlane = Instantiate(planePrefab, origin, Quaternion.identity, this.transform);
        //    newPlane.transform.up = -1f * PlaneDirections[i];
        //    Planes.Add(newPlane);
        //}

        Planes = GetComponentsInChildren<MagicCarpetPlane>();
    }


    void SetPlane(MagicCarpetPlane plane, VoxelHitInfo hit)
    {
        if (hit.didHit && hit.voxel.Shape == BlockShape.Solid)
        {
            plane.gameObject.SetActive(true);
            plane.transform.position = hit.voxelPos + plane.GetOffset();
        }
        else
        {
            plane.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (world == null) world = VoxelWorld.Instance;


        Vector3 pos = player.transform.position;
        for (int i = 0; i < Planes.Length; i++)
        {
            VoxelHitInfo hit = world.VoxelTraversal(pos + Planes[i].GetOrigin(), Planes[i].GetDirection(), RaycastDistance);
            SetPlane(Planes[i], hit);
        }
        //if (pos != lastPos)
        //{

            
            //VoxelHitInfo hitNX = world.VoxelTraversal(pos + 0.5f * Vector3.up + 0.5f * Vector3.right, Vector3.right, RaycastDistance);
            //VoxelHitInfo hitPX = world.VoxelTraversal(pos + 0.5f * Vector3.up - 0.5f * Vector3.right, Vector3.left, RaycastDistance);
            //VoxelHitInfo hitNZ = world.VoxelTraversal(pos + 0.5f * Vector3.up + 0.5f * Vector3.forward, Vector3.forward, RaycastDistance);
            //VoxelHitInfo hitPZ = world.VoxelTraversal(pos + 0.5f * Vector3.up - 0.5f * Vector3.forward, Vector3.back, RaycastDistance);

            //VoxelHitInfo hitNY = world.VoxelTraversal(pos + 1.5f * Vector3.up, Vector3.up, RaycastDistance);
            //VoxelHitInfo hitPY = world.VoxelTraversal(pos + 0.0f * Vector3.up, Vector3.down, RaycastDistance);

            //SetPlane(planeNX, hitNX);
            //SetPlane(planePX, hitPX);
            //SetPlane(planeNY, hitNY);
            //SetPlane(planePY, hitPY);
            //SetPlane(planeNZ, hitNZ);
            //SetPlane(planePZ, hitPZ);
        //}
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
