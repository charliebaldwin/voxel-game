using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;
using static VoxelHelper;
//using VInspector.Libs;

public class PlayerController : MonoBehaviour
{

    [Foldout("References")]
    public CharacterController CharController;
    public Transform CameraPivot;
    public Camera Camera;
    public PlayerViewmodel Viewmodel;
    public InventoryManager Inventory;
    public VoxelCursor VoxelCursor;
    public GUIController GUIController;
    [EndFoldout]
    private VoxelWorld world;

    [Header("Movement")]
    public float walkSpeed = 1f;
    public float sprintSpeed = 2f;
    public float jumpForce = 10f;
    public float airborneControl = 0.5f;
    public float gravityForce = -9.8f;
    [ShowInInspector] private bool grounded;
    public float friction = 0.1f;


    [Header("Camera")]
    public float lookSens = 1f;
    [Range(0f, 3f)]
    public float cameraLeanAmount = 1f;
    [Range(0f, 1f)][HideIf("cameraLeanAmount", 0f)]
    public float cameraLeanSpeed = 0.1f;
    [EndIf]
    [Range(0f, 1f)]
    public float handRotateAmount = 1f;


    [Header("Targeting")]
    public int AimDistance = 20;
    public LayerMask targetAimLayers;
    public float targetAimRange = 4f;
    public bool aimingAtTarget = false;




    private float currentMoveSpeed = 0f;
    private float currentCameraLean = 0f;
    private Vector3 cameraEuler;

    private Vector3 motion = Vector3.zero;
    //private float gravity = 0f;

    private Vector2 moveInput;
    private Vector3 velocity;
    private Vector3 forward;
    private OrthoNormal facingXZOrtho;

    private Item equippedItem;



    private void Awake()
    {
        world = VoxelWorld.Instance;
        CharController = GetComponent<CharacterController>();
        Camera = GetComponentInChildren<Camera>();
        cameraEuler = CameraPivot.localEulerAngles;
        currentMoveSpeed = walkSpeed;
    }
    void Start()
    {
        world = VoxelWorld.Instance;
        equippedItem = ItemRegistry.LookupItem(ItemID.NullItem);
    }

    void Update()
    {
        grounded = CharController.isGrounded;
        if (!CharController.isGrounded)
        {
            velocity.y += Time.deltaTime * gravityForce;
        }
        velocity.x = 0f;
        velocity.z = 0f;

        velocity += moveInput.x * currentMoveSpeed * transform.right;
        velocity += moveInput.y * currentMoveSpeed * transform.forward;

        CharController.Move(Time.deltaTime * velocity);

        if (CharController.isGrounded)
        {
            velocity.y = Time.deltaTime * gravityForce;
        }



        currentCameraLean = Mathf.Lerp(currentCameraLean, -1f * cameraLeanAmount * moveInput.x, cameraLeanSpeed);
        cameraEuler.y = 0f;
        cameraEuler.z = currentCameraLean;
        CameraPivot.localEulerAngles = cameraEuler;


        forward = CameraPivot.forward;
        OrthoNormal tempFacing = new OrthoNormal(forward);
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            facingXZOrtho = new OrthoNormal(tempFacing.x, (sbyte)0, (sbyte)0);
        }
        else
        {
            facingXZOrtho = new OrthoNormal((sbyte)0, (sbyte)0, tempFacing.z);
        }

        DebugPanel.PlayerPosition = transform.position;
        DebugPanel.PlayerForward = forward;
        DebugPanel.PlayerForwardOrtho = facingXZOrtho;

        


    }

    private void FixedUpdate()
    {
        RaycastWorld();
        RaycastEntities();
        SetHitDebugInfo();
    }



    #region INPUT HANDLERS

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        //Debug.Log($"movement: {moveInput}");
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        currentMoveSpeed = Mathf.Lerp(walkSpeed, sprintSpeed, context.ReadValue<float>());
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (CharController.isGrounded)
        {
            velocity.y = jumpForce;
        }
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 aimDelta = context.ReadValue<Vector2>();

            cameraEuler.x -= aimDelta.y * lookSens;
            cameraEuler.x = Mathf.Clamp(cameraEuler.x, -90f, 90f);
            currentCameraLean = Mathf.Lerp(currentCameraLean, -1f * cameraLeanAmount * aimDelta.x, 6f * Time.deltaTime * cameraLeanSpeed);
            transform.Rotate(transform.up, aimDelta.x * lookSens);
        }
    }

    //private bool primaryDown = false;
    public void OnPrimary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked)
        {
            //primaryDown = true;
            int strength = equippedItem.Strength;
            if (equippedItem.Type != ItemType.Tool) 
            {
                strength = 1;
            }
            DamageVoxel(strength);
        }
        else if (context.canceled)
        {
            //primaryDown = false;
        }
    }

    //private bool secondaryDown = false;
    public void OnSecondary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked)
        {
            //secondaryDown = true;
            if (equippedItem.Type == ItemType.Block)
            {
                PlaceVoxel(equippedItem.BlockID);
            }
        }
        else if (context.canceled)
        {
            //secondaryDown = false;
        }
    }

    public void OnTertiary(InputAction.CallbackContext context)
    {
        if (context.started && Cursor.lockState == CursorLockMode.Locked)
        {
            ExplodeWorld();
        }
    }

    private BlockShape heldBlockShape = BlockShape.Solid;
    public void OnNumKey(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            int num = Mathf.RoundToInt(context.ReadValue<float>());
            heldBlockShape = (BlockShape)num;
        }
    }

    public CanvasGroup tempPopup;
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started && hitBlockEntity != null)
        {
            Debug.Log($"{hitBlockEntity.name}");
            GUIController.OpenPopup();
            
        }
    }
    #endregion

    #region WORLD INTERACTION

    private VoxelHitInfo lastHitInfo = new VoxelHitInfo();
    private void RaycastWorld()
    {
        lastHitInfo = world.VoxelTraversal(CameraPivot.position, CameraPivot.forward, AimDistance);
        VoxelCursor.MoveCursor(lastHitInfo, equippedItem.Type);
    }

    private BlockEntityActor hitBlockEntity = null;
    private void RaycastEntities()
    {
        RaycastHit hit;
        if (Physics.Raycast(CameraPivot.position, CameraPivot.forward, out hit, 10f, 1 << 7))
        {
            hitBlockEntity = hit.collider.GetComponent<BlockEntityActor>();
        } else
        {
            hitBlockEntity = null;
        }
    }

    private void DamageVoxel(int damage)
    {
        Viewmodel.PlayHitAnimation(1f / equippedItem.UseTime);
        if (lastHitInfo.didHit)
        {
            world.DamageVoxel(lastHitInfo.voxelPos, lastHitInfo, (byte)damage);
        }
    }

    private const bool DO_ROTATE = false;
    private void PlaceVoxel(BlockID id)
    {
        if (lastHitInfo.didHit)
        {
            BlockData block = BlockRegistry.LookupBlock(id);
            OrthoNormal up = OrthoNormal.up;
            OrthoNormal fwd = OrthoNormal.forward;

            if (DO_ROTATE)
            {

                if (block.CanChangeUpAxis || heldBlockShape != BlockShape.Solid)
                {
                    up = OrthoNormal.FromVector(lastHitInfo.hitNormal);
                }
                fwd = (up == OrthoNormal.forward || up == OrthoNormal.back) ? OrthoNormal.right : OrthoNormal.forward;
                //OrthoNormal fwd = OrthoNormal.forward;

                if (up.IsEqual(OrthoNormal.up))
                {
                    fwd = facingXZOrtho;
                }
                else if (up.IsEqual(OrthoNormal.forward) || up.IsEqual(OrthoNormal.back))
                {
                    fwd = OrthoNormal.right;
                }
                if (fwd.IsEqual(OrthoNormal.back) && up.IsEqual(OrthoNormal.up))
                {
                    //up = OrthoNormal.down;
                    //fwd = fwd.Flip();
                }
            }

            // TEMP
            //up = OrthoNormal.up;
            //fwd = OrthoNormal.forward;


            world.AddVoxel(lastHitInfo.voxelPos + lastHitInfo.hitNormal, new Voxel(id, 0, heldBlockShape, up, fwd));
            Debug.Log($"placed: up={up}, fwd={fwd}");
            //Debug.Log($"placed block, ID={id}, shape={heldBlockShape}");
        }
    }

    private void ExplodeWorld()
    {
        if (lastHitInfo.didHit)
        {
            world.Explode(lastHitInfo.voxelPos, 3f);
        }
    }

    #endregion

    #region INVENTORY INTERACTION

    public void SetEquippedItem(ItemID id)
    {
        equippedItem = ItemRegistry.LookupItem(id);
        Viewmodel.SetItemModel(equippedItem);
    }

    #endregion



    private void OnDrawGizmos()
    {
        Vector3 lineVector = motion;
        lineVector.y = 0f;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + lineVector);

        if (Camera == null) Camera = GetComponentInChildren<Camera>();
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Camera.transform.position, Camera.transform.position + Camera.transform.forward * targetAimRange);
    }

    private void SetHitDebugInfo()
    {
        DebugOverlay.AddSpacer(0);
        DebugOverlay.SetDebugValue("Player Position", transform.position);
        DebugOverlay.SetDebugValue("Forward", forward);
        DebugOverlay.SetDebugValue("Forward Ortho", facingXZOrtho);
        //DebugPanel.LastHitInfo = hitData;

        DebugOverlay.AddSpacer(1);
        if (lastHitInfo.didHit)
        {
            DebugOverlay.SetDebugValue("Block ID", lastHitInfo.blockID);
            DebugOverlay.SetDebugValue("Voxel World Pos", lastHitInfo.voxelPos);
            DebugOverlay.SetDebugValue("Voxel Local Pos", lastHitInfo.localVoxelPos);
            DebugOverlay.SetDebugValue("Chunk", lastHitInfo.chunkPos);
            DebugOverlay.SetDebugValue("Hit Position", lastHitInfo.hitPos);
            DebugOverlay.SetDebugValue("Hit Face", NormalVectorToText(lastHitInfo.hitNormal));
            DebugOverlay.SetDebugValue("Distance", lastHitInfo.distance);
        }
        else
        {
            DebugOverlay.SetDebugValue("Block ID", "None");
            DebugOverlay.SetDebugValue("Voxel World Pos", "-");
            DebugOverlay.SetDebugValue("Voxel Local Pos", "-");
            DebugOverlay.SetDebugValue("Chunk", "-");
            DebugOverlay.SetDebugValue("Hit Position", "-");
            DebugOverlay.SetDebugValue("Hit Face", "-");
            DebugOverlay.SetDebugValue("Distance", "-");
        }
    }

}
