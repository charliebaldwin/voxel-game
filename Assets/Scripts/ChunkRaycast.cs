using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector.Libs;

public class ChunkRaycast : MonoBehaviour
{
    public LayerMask mask;
    public float Distance = 20f;
    public int Steps = 300;
    

    public GameObject VoxelCursor;
    public Inventory PlayerInventory;
    public Material UICubeMat;
    public Animator HandAnimator;
    public Material HeldItemMat;

    private Vector3 debugRayStart;
    private Vector3 debugRayEnd;
    private Vector3 hitVoxPos;
    private bool didHitVox = false;
    private List<Vector3> colliderEnterPoints = new List<Vector3>();
    private List<Vector3> colliderExitPoints = new List<Vector3>();

    private int placedBlockType = 1;
    private int hotbarSlot = 0;
    private byte toolDamage = 1;
    public const int NUM_HOTBAR_SLOTS = 10;

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

    public void OnPrimary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked)
        {
            DoRaycast3(1);
            HandAnimator.SetTrigger("Hit");
        }
    }
    public void OnSecondary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked)
        {
            DoRaycast3(2);
        }
    }
    public void OnTertiary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked)
        {
            DoRaycast3(3);
        }
    }
    public void OnNumKey(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            placedBlockType = Mathf.RoundToInt(context.ReadValue<float>());
            UICubeMat.SetInteger("_BlockIndex", placedBlockType);
        }
    }
    public void OnScroll(InputAction.CallbackContext context)
    {
        if (context.started) 
        {
            hotbarSlot += context.ReadValue<float>().RoundToInt();
            hotbarSlot = hotbarSlot.Clamp(0, NUM_HOTBAR_SLOTS - 1);
            Debug.Log(hotbarSlot);
            ItemData slotItem = PlayerInventory.SetSlot(hotbarSlot);
            HandAnimator.SetTrigger("Equip");

            if ( slotItem != null)
            {
                toolDamage = slotItem.toolDamage;
                HeldItemMat.SetTexture("_Tex_Color", slotItem.mainTex);
                HeldItemMat.SetTexture("_Tex_MetalSmooth", slotItem.metalSmoothTex);

            }
            else
            {
                toolDamage = 1;
                HeldItemMat.SetTexture("_Tex_Color", null);
                HeldItemMat.SetTexture("_Tex_MetalSmooth", null);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        DoRaycast3(0);

    }


    private void DoRaycast3(int mode)
    {
        //VoxelHitData hitData = VoxelWorld.Instance.VoxelRaycast(transform.position, transform.forward, Distance, 300);
        //print(hitData.blockID);
        VoxelHitInfo hitData = VoxelWorld.Instance.VoxelTraversal(transform.position, transform.forward, 30);
        if (hitData.didHit)
        {
            VoxelCursor.SetActive(true);
            switch (mode)
            {
                case 0:
                    VoxelCursor.transform.position = hitData.voxelPos;
                    VoxelCursor.transform.forward = hitData.hitNormal;
                    break;
                case 1:
                    VoxelWorld.Instance.DamageVoxel(hitData.voxelPos, toolDamage);
                    break;
                case 2:
                    VoxelWorld.Instance.AddVoxel(hitData.voxelPos + hitData.hitNormal, placedBlockType);
                    //Debug.Log($"normal: {hitData.hitNormal}"); 
                    break;
                case 3:
                    VoxelWorld.Instance.Explode(hitData.voxelPos, 3f);
                    break;
            }
        } 
        else
        {
            VoxelCursor.SetActive(false);
        }
    }
}
