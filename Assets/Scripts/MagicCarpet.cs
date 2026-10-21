using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MagicCarpet : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    private VoxelWorld world;

    [SerializeField] private GameObject planePrefab;
    private MagicCarpetPlane[] Planes;

    public int RaycastDistance = 2;

    private Vector3 lastPos = Vector3.zero;

    private void Awake()
    {
        world = VoxelWorld.Instance;
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
    }
}
