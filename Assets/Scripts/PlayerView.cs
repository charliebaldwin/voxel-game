using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector.Libs;
using static UnityEditor.Progress;
using static VoxelHelper;
public class PlayerView : MonoBehaviour
{

    private const float DEFAULT_TOOL_USE_TIME = 0.5f;
    private const byte DEFAULT_TOOL_DAMAGE = 1;
    public const int NUM_HOTBAR_SLOTS = 10;

    public LayerMask mask;
    public float AimDistance = 5f;
    public int Steps = 300;
    

    public GameObject VoxelCursor;
    public InventoryManager PlayerInventory;
    public Material UICubeMat;
    public Animator HandAnimator;
    public MeshFilter ItemMeshFilter;
    public MeshRenderer ItemMeshRenderer;
    public Material NullMaterial;
    public Mesh NullMesh;

    private Vector3 debugRayStart;
    private Vector3 debugRayEnd;
    private Vector3 hitVoxPos;
    private bool didHitVox = false;
    private List<Vector3> colliderEnterPoints = new List<Vector3>();
    private List<Vector3> colliderExitPoints = new List<Vector3>();

    private ItemType currentItemType;
    private Item heldItem;

    private byte placedBlockShape = 1;
    private int placedBlockType = 1;
    private int hotbarSlot = 0;
    private float toolUseTime = DEFAULT_TOOL_USE_TIME;
    private byte toolDamage = DEFAULT_TOOL_DAMAGE;
    private bool primaryDown = false;
    private bool secondaryDown = false;
    public static bool usingTool = false;

    private IEnumerator cr_toolUse = null;

    private VoxelHitInfo lastHitInfo;

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

    #region Input Handlers
    public void OnPrimary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked)
        {
            primaryDown = true;
            if (!usingTool)
            {
                cr_toolUse = UseToolTimer();
                StartCoroutine(cr_toolUse);
            }
        }
        else if (context.canceled)
        {
            primaryDown = false;
        }
    }
    public void OnSecondary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked && currentItemType == ItemType.Block)
        {
            secondaryDown = true;
            if (!usingTool)
            {
                cr_toolUse = PlaceBlockTimer();

                StartCoroutine(cr_toolUse);
            }
        } else if (context.canceled)
        {
            secondaryDown = false;
        }
    }
    public void OnTertiary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked)
        {
            DoTertiary();
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
            //Debug.Log($"slot={hotbarSlot}");
            PlayerInventory.SelectHotbarSlot(hotbarSlot);
        }
    }
    #endregion

    public void UpdateEquippedItem(Item item)
    {
        if (heldItem != null && heldItem.ItemID != item.ItemID)
            HandAnimator.SetTrigger("Equip");
        heldItem = item;

    }

    public void UpdateItemModel()
    {
        // currentItemType = heldItem.Type;
        if (heldItem != null)
        {
            if (heldItem.Type == ItemType.Block)
            {
                currentItemType = ItemType.Block;
                BlockData block = new BlockData(heldItem);
                placedBlockType = (int)block.BlockID;
            }
            else if (heldItem.Type == ItemType.Tool)
            {
                currentItemType = ItemType.Tool;
                ToolData tool = new ToolData(heldItem);
                toolDamage = (byte)tool.Strength;
                toolUseTime = tool.UseTime;
                Debug.Log($"Tool: {tool.Strength}, {tool.UseTime}");
            }
            else
            {
                currentItemType = ItemType.Null;
                toolDamage = 1;
                toolUseTime = DEFAULT_TOOL_USE_TIME;
                placedBlockType = 0;
            }
            ItemMeshFilter.mesh = heldItem.ViewmodelMesh;
            ItemMeshRenderer.material = heldItem.ViewmodelMat;
        }
    }


    // Update is called once per frame
    void Update()
    {
        RaycastWorld();
        //DoRaycast3(0);
        //HandAnimator = GetComponent<Animator>();

    }

    private IEnumerator UseToolTimer()
    {
        usingTool = true;
        ToolData tool = new ToolData(heldItem);
        float duration = tool.UseTime;

        DoPrimary((byte)tool.Strength);

        HandAnimator.SetTrigger("Hit");
        HandAnimator.SetFloat("ToolSpeed", 1f / duration);

        yield return new WaitForSeconds(duration);
        if (primaryDown)
        {
            cr_toolUse = UseToolTimer();
            StartCoroutine(cr_toolUse);
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
        DoSecondary();
        HandAnimator.SetTrigger("Hit");
        HandAnimator.SetFloat("ToolSpeed", 1f / duration);
        yield return new WaitForSeconds(duration);
        if (secondaryDown)
        {
            cr_toolUse = PlaceBlockTimer();
            StartCoroutine(cr_toolUse);
        }
        else
        {
            usingTool = false;
        }
    }

    private void RaycastWorld()
    {
        lastHitInfo = VoxelWorld.Instance.VoxelTraversal(transform.position, transform.forward, AimDistance.CeilToInt());
        if (lastHitInfo.didHit)
        {
            VoxelCursor.transform.position = lastHitInfo.voxelPos;
            VoxelCursor.transform.forward = lastHitInfo.hitNormal;
            VoxelCursor.SetActive(true);
        }
        else
            VoxelCursor.SetActive(false);

    }

    private void DoPrimary(byte damage)
    {
        if (lastHitInfo.didHit)
        {
            Vector3 normal = new Vector3(lastHitInfo.hitNormal.x, lastHitInfo.hitNormal.y, lastHitInfo.hitNormal.z);
            World().DamageVoxel(lastHitInfo.voxelPos, lastHitInfo, damage);
        }
    }
    private void DoSecondary()
    {
        if (lastHitInfo.didHit)
        {
            if (currentItemType == ItemType.Block)
            {
                //byte o = VoxelHelper.NormalToOrientation(lastHitInfo.hitNormal);
                byte o = VoxelHelper.NormalToOrientation(Vector3Int.up);
                World().AddVoxel(lastHitInfo.voxelPos + lastHitInfo.hitNormal, new Voxel(placedBlockType, 0, o, placedBlockShape));
            }
        }
    }
    private void DoTertiary()
    {
        if (lastHitInfo.didHit)
        {
            World().Explode(lastHitInfo.voxelPos, 3f);
        }

    }

    private void DoRaycast3(int mode)
    {
        //VoxelHitData hitData = VoxelWorld.Instance.VoxelRaycast(transform.position, transform.forward, Distance, 300);
        //print(hitData.blockID);
        //VoxelHitInfo hitData = VoxelWorld.Instance.VoxelTraversal(transform.position, transform.forward, AimDistance.CeilToInt());
        if (lastHitInfo.didHit)
        {
            VoxelCursor.SetActive(true);
            switch (mode)
            {
                case 0:
                    VoxelCursor.transform.position = lastHitInfo.voxelPos;
                    VoxelCursor.transform.forward = lastHitInfo.hitNormal;
                    break;
                case 1:
                    Vector3 normal = new Vector3(lastHitInfo.hitNormal.x, lastHitInfo.hitNormal.y, lastHitInfo.hitNormal.z);
                    VoxelWorld.Instance.DamageVoxel(lastHitInfo.voxelPos, lastHitInfo, toolDamage);
                    break;
                case 2:
                    if (currentItemType == ItemType.Block)
                    {
                        byte o = VoxelHelper.NormalToOrientation(lastHitInfo.hitNormal);
                        VoxelWorld.Instance.AddVoxel(lastHitInfo.voxelPos + lastHitInfo.hitNormal, new Voxel(placedBlockType, 0, o, placedBlockShape));
                    }
                    //Debug.Log($"normal: {hitData.hitNormal}"); 
                    break;
                case 3:
                    VoxelWorld.Instance.Explode(lastHitInfo.voxelPos, 3f);
                    break;
            }
        } 
        else
        {
            VoxelCursor.SetActive(false);
        }
    }
}
