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
    private List<Vector3> colliderEnterPoints = new List<Vector3>();
    private List<Vector3> colliderExitPoints = new List<Vector3>();
    private VoxelHitInfo lastHitInfo;

    // Held Item
    private ItemType currentItemType;
    private Item heldItem;
    // held block
    private BlockID heldBlockID = BlockID.Air;
    private BlockShape heldBlockShape = BlockShape.Solid;
    private float blockPlaceTime = DEFAULT_TOOL_USE_TIME;
    // held tool
    private float toolUseTime = DEFAULT_TOOL_USE_TIME;
    private int toolDamage = DEFAULT_TOOL_DAMAGE;

    // action status
    private bool primaryDown = false;
    private bool secondaryDown = false;
    public static bool usingTool = false;
    private int hotbarSlot = 0;
    private IEnumerator CO_useTool = null;




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


        if (lastHitInfo.didHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(debugRayEnd, lastHitInfo.hitPos);
            Gizmos.DrawSphere(lastHitInfo.hitPos, 1f);
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
                CO_useTool = UseToolTimer();
                StartCoroutine(CO_useTool);
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
                CO_useTool = PlaceBlockTimer();

                StartCoroutine(CO_useTool);
            }
        } else if (context.canceled)
        {
            secondaryDown = false;
            usingTool = false;
            StopCoroutine(CO_useTool);
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
            //placedBlockType = Mathf.RoundToInt(context.ReadValue<float>());
            //placedBlockShape = (byte)math.clamp(placedBlockType, 1, 2);

            //UICubeMat.SetInteger("_BlockIndex", placedBlockType);
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
                BlockData block = BlockRegistry.LookupBlock(heldItem.BlockID);

                heldBlockID = block.BlockID;
            }
            else if (heldItem.Type == ItemType.Tool)
            {
                currentItemType = ItemType.Tool;
                toolDamage = heldItem.Strength;
                toolUseTime = heldItem.UseTime;
            }
            else
            {
                currentItemType = ItemType.Null;
                toolDamage = 1;
                toolUseTime = DEFAULT_TOOL_USE_TIME;
                heldBlockID = 0;
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
        float duration = heldItem.UseTime;

        DoPrimary((byte)heldItem.Strength);

        HandAnimator.SetTrigger("Hit");
        HandAnimator.SetFloat("ToolSpeed", 1f / duration);

        yield return new WaitForSeconds(duration);
        if (primaryDown)
        {
            CO_useTool = UseToolTimer();
            StartCoroutine(CO_useTool);
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
            CO_useTool = PlaceBlockTimer();
            StartCoroutine(CO_useTool);
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
                ////byte o = VoxelHelper.NormalToOrientation(Vector3Int.up);
                //World().AddVoxel(lastHitInfo.voxelPos + lastHitInfo.hitNormal, new Voxel(heldBlockID, 0, o, heldBlockShape));

                OrthoNormal up = OrthoNormal.FromVector(lastHitInfo.hitNormal);
                OrthoNormal fwd = (up == OrthoNormal.forward || up == OrthoNormal.back) ? OrthoNormal.right : OrthoNormal.forward;
                World().AddVoxel(lastHitInfo.voxelPos + lastHitInfo.hitNormal, new Voxel(heldBlockID, 0, heldBlockShape, up, fwd));

                Debug.Log($"placed block, ID={heldBlockID}, shape={heldBlockShape}");

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
                    VoxelWorld.Instance.DamageVoxel(lastHitInfo.voxelPos, lastHitInfo, (byte)toolDamage);
                    break;
                case 2:
                    if (currentItemType == ItemType.Block)
                    {
                        byte o = VoxelHelper.NormalToOrientation(lastHitInfo.hitNormal);
                        VoxelWorld.Instance.AddVoxel(lastHitInfo.voxelPos + lastHitInfo.hitNormal, new Voxel(heldBlockID, 0, o, heldBlockShape));
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
