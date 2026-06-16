using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector.Libs;

public class PlayerView : MonoBehaviour
{

    private const float DEFAULT_TOOL_USE_TIME = 0.5f;
    private const byte DEFAULT_TOOL_DAMAGE = 1;
    public const int NUM_HOTBAR_SLOTS = 10;

    public LayerMask mask;
    public float AimDistance = 5f;
    public int Steps = 300;
    

    public GameObject VoxelCursor;
    public Inventory PlayerInventory;
    public Material UICubeMat;
    public Animator HandAnimator;
    public Material HeldItemMat;
    public Texture2D NullTex;

    private Vector3 debugRayStart;
    private Vector3 debugRayEnd;
    private Vector3 hitVoxPos;
    private bool didHitVox = false;
    private List<Vector3> colliderEnterPoints = new List<Vector3>();
    private List<Vector3> colliderExitPoints = new List<Vector3>();

    private ItemType currentItemType;
    private byte placedBlockShape = 1;
    private int placedBlockType = 1;
    private int hotbarSlot = 0;
    private float toolUseTime = DEFAULT_TOOL_USE_TIME;
    private byte toolDamage = DEFAULT_TOOL_DAMAGE;
    private bool primaryDown = false;
    public static bool usingTool = false;

    private IEnumerator toolUseCoroutine = null;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetTool(hotbarSlot);
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
            primaryDown = true;
            if (!usingTool)
            {
                toolUseCoroutine = UseToolTimer();

                StartCoroutine(toolUseCoroutine);
            }

        }
        else if (context.canceled)
        {
            //Debug.Log("canceled primary");
            primaryDown = false;
           // if (toolUseCoroutine != null) StopCoroutine(toolUseCoroutine);
        }
    }
    public void OnSecondary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked && currentItemType == ItemType.Block)
        {
           // DoRaycast3(2);
            if (!usingTool)
            {
                toolUseCoroutine = PlaceBlockTimer();

                StartCoroutine(toolUseCoroutine);
            }
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
            placedBlockShape = (byte)math.clamp(placedBlockType, 1, 2);

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
            SetTool(hotbarSlot);
        }
    }

    private void SetTool(int hotbarSlot)
    {
        ItemData slotItem = PlayerInventory.SetSlot(hotbarSlot);
        HandAnimator.SetTrigger("Equip");

        if (slotItem != null)
        {
            currentItemType = slotItem.type;
            placedBlockType = slotItem.blockID;
            toolDamage = slotItem.toolDamage;
            toolUseTime = slotItem.toolUseTime;
            HeldItemMat.SetTexture("_Tex_Color", slotItem.mainTex);
            HeldItemMat.SetTexture("_Tex_MetalSmooth", slotItem.metalSmoothTex);
            HeldItemMat.SetTexture("_Tex_Emission", slotItem.emissiveTex);


        }
        else
        {
            toolDamage = 1;
            toolUseTime = DEFAULT_TOOL_USE_TIME;
            HeldItemMat.SetTexture("_Tex_Color", NullTex);
            HeldItemMat.SetTexture("_Tex_MetalSmooth", NullTex);
            HeldItemMat.SetTexture("_Tex_Emission", NullTex);

        }
    }


    // Update is called once per frame
    void Update()
    {
        DoRaycast3(0);

    }

    private IEnumerator UseToolTimer()
    {
        usingTool = true;
        float duration = toolUseTime;
        DoRaycast3(1);
        HandAnimator.SetTrigger("Hit");
        HandAnimator.SetFloat("ToolSpeed", 1f / duration);
        yield return new WaitForSeconds(duration);
        if (primaryDown)
        {
            toolUseCoroutine = UseToolTimer();
            StartCoroutine(toolUseCoroutine);
        }
        else
        {
            usingTool = false;
        }
    }
    private IEnumerator PlaceBlockTimer()
    {
        usingTool = true;
        float duration = toolUseTime;
        DoRaycast3(2);
        HandAnimator.SetTrigger("Hit");
        HandAnimator.SetFloat("ToolSpeed", 1f / duration);
        yield return new WaitForSeconds(duration);
        if (primaryDown)
        {
            toolUseCoroutine = UseToolTimer();
            StartCoroutine(toolUseCoroutine);
        }
        else
        {
            usingTool = false;
        }
    }



    private void DoRaycast3(int mode)
    {
        //VoxelHitData hitData = VoxelWorld.Instance.VoxelRaycast(transform.position, transform.forward, Distance, 300);
        //print(hitData.blockID);
        VoxelHitInfo hitData = VoxelWorld.Instance.VoxelTraversal(transform.position, transform.forward, AimDistance.CeilToInt());
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
                    Vector3 normal = new Vector3(hitData.hitNormal.x, hitData.hitNormal.y, hitData.hitNormal.z);
                    VoxelWorld.Instance.DamageVoxel(hitData.voxelPos, hitData.hitPos + 0.2f*normal, toolDamage);
                    break;
                case 2:
                    if (currentItemType == ItemType.Block)
                    {
                        VoxelWorld.Instance.AddVoxel(hitData.voxelPos + hitData.hitNormal, new VoxelData(placedBlockType, 0, 0, placedBlockShape));
                    }
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
