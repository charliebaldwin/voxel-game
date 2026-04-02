using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ChunkRaycast : MonoBehaviour
{
    public LayerMask mask;
    public float Distance = 20f;
    public int Steps = 300;

    public GameObject Cursor;

    public Material UICubeMat;

    private Vector3 debugRayStart;
    private Vector3 debugRayEnd;
    private Vector3 hitVoxPos;
    private bool didHitVox = false;
    private List<Vector3> colliderEnterPoints = new List<Vector3>();
    private List<Vector3> colliderExitPoints = new List<Vector3>();

    private int placedBlockType = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(debugRayStart, debugRayEnd);

        Gizmos.color = Color.blue;
        foreach (Vector3 p in colliderEnterPoints)
        {
            Gizmos.DrawSphere(p, 0.25f);
        }

        Gizmos.color = Color.red;
        foreach (Vector3 p in colliderExitPoints)
        {
            Gizmos.DrawCube(p, new Vector3(0.5f, 0.5f, 0.5f));

        }


        if (didHitVox)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(debugRayEnd, hitVoxPos);
            Gizmos.DrawSphere(hitVoxPos, 1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DoRaycast3(1);
        } 
        else if ( Input.GetMouseButtonDown(1)) 
        {
            DoRaycast3(2);
        } else
        {
            DoRaycast3(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
            placedBlockType = 1;
        if (Input.GetKeyDown(KeyCode.Alpha2))
            placedBlockType = 2;
        if (Input.GetKeyDown(KeyCode.Alpha3))
            placedBlockType = 3;
        if (Input.GetKeyDown(KeyCode.Alpha4))
            placedBlockType = 4;
        if (Input.GetKeyDown(KeyCode.Alpha5))
            placedBlockType = 5;
        if (Input.GetKeyDown(KeyCode.Alpha6))
            placedBlockType = 6;
        if (Input.GetKeyDown(KeyCode.Alpha7))
            placedBlockType = 7;
        if (Input.GetKeyDown(KeyCode.Alpha8))
            placedBlockType = 8;
        if (Input.GetKeyDown(KeyCode.Alpha9))
            placedBlockType = 9;
        if (Input.GetKeyDown(KeyCode.Alpha0))
            placedBlockType = 10;

        UICubeMat.SetInteger("_Index", placedBlockType);

    }

    private void DoRaycast1()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit[] hits;
            hits = Physics.RaycastAll(new Ray(transform.position, transform.forward), 999f, mask);
            foreach (RaycastHit hit in hits)
            {
                hit.collider.gameObject.GetComponent<VoxelChunk>().VoxelRaycast(hit.point, transform.forward);
            }
        }
    }

    private void DoRaycast2(int mode)
    {


        colliderEnterPoints = new List<Vector3>();
        colliderExitPoints = new List<Vector3>();

        //Debug.Log("starting raycast");

        bool complete = false;
        int i = 0;
        Vector3 rayOrigin = transform.position;

        while (!complete && i < 10)
        {
            i++;
            RaycastHit hit;
            VoxelHitData voxelHitData;

            if (Physics.Raycast(new Ray(rayOrigin, transform.forward), out hit, 999f, mask))
            {
                debugRayStart = transform.position;
                debugRayEnd = hit.point;
                colliderEnterPoints.Add(hit.point);


                VoxelChunk hitChunk = hit.collider.gameObject.GetComponent<VoxelChunk>();
                voxelHitData = hitChunk.VoxelRaycast(hit.point, transform.forward);
                rayOrigin = voxelHitData.hitPos - 0.2f * transform.forward;

                if (voxelHitData.didHit)
                {
                    switch(mode)
                    {
                        case 0:
                            VoxelWorld.Instance.DestroyVoxel(voxelHitData.worldVoxelPos);
                            //hitChunk.BreakBlock(voxelHitData.worldVoxelPos);
                            break;
                        case 1:
                            //Debug.Log($"placing block at {voxelHitData.worldVoxelPos + voxelHitData.hitNormal} (normal: {voxelHitData.hitNormal})");
                            VoxelWorld.Instance.AddVoxel(voxelHitData.worldVoxelPos + voxelHitData.hitNormal, placedBlockType);
                            //hitChunk.PlaceBlock(voxelHitData.worldVoxelPos, voxelHitData.hitNormal, placedBlockType);
                            break;
                        default:
                            break;

                    }
                    // Debug.Log($"chunk {i} hit");
                    //Debug.Log($"hit normal: {voxelHitData.hitNormal}");
                    didHitVox = true;
                    hitVoxPos = voxelHitData.hitPos;
                    debugRayEnd = voxelHitData.hitPos;

                    complete = true;
                    break;
                }
                else
                {
                    colliderExitPoints.Add(voxelHitData.hitPos);
                    debugRayEnd = voxelHitData.hitPos;
                    //Debug.Log($"chunk {i} missed");
                }
            }
            else
            {
                Debug.Log($"raycast missed after {i} attempts");
                didHitVox = false;
                complete = true;
            }
        }
    }

    private void DoRaycast3(int mode)
    {
        VoxelHitData hitData = VoxelWorld.Instance.VoxelRaycast(transform.position, transform.forward, Distance, 300);
        //print(hitData.blockID);
        if (hitData.didHit)
        {
            Cursor.SetActive(true);
            switch (mode)
            {
                case 0:
                    Cursor.transform.position = hitData.worldVoxelPos;
                    Cursor.transform.forward = hitData.hitNormal;
                    break;
                case 1:
                    VoxelWorld.Instance.DestroyVoxel(hitData.worldVoxelPos);
                    break;
                case 2:
                    VoxelWorld.Instance.AddVoxel(hitData.worldVoxelPos + hitData.hitNormal, placedBlockType);
                    //Debug.Log($"normal: {hitData.hitNormal}"); 
                    break;
            }
        } 
        else
        {
            Cursor.SetActive(false);
        }
    }
}
