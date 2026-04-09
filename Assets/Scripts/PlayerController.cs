using System.Collections;
using System.Collections.Generic;
using VInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [Foldout("References")]
    public CharacterController charController;
    public Camera playerCam;
    public Transform cameraPivot;
    public Transform handPivot;


    [Foldout("Movement")]
    public float walkSpeed = 1f;
    public float sprintSpeed = 2f;
    public float jumpForce = 10f;
    public float airborneControl = 0.5f;
    public float gravityForce = -9.8f;
    [ShowInInspector] private bool grounded;
    public float friction = 0.1f;


    [Foldout("Camera")]
    public float lookSens = 1f;
    [Range(0f, 3f)]
    public float cameraLeanAmount = 1f;
    [Range(0f, 1f)][HideIf("cameraLeanAmount", 0f)]
    public float cameraLeanSpeed = 0.1f;
    [EndIf]
    [Range(0f, 1f)]
    public float handRotateAmount = 1f;


    [Foldout("Targeting")]
    public LayerMask targetAimLayers;
    public float targetAimRange = 4f;
    public bool aimingAtTarget = false;

    [Foldout("Key Bindings")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [EndFoldout]


    private float currentMoveSpeed = 0f;
    private float currentCameraLean = 0f;
    private Vector3 cameraEuler;

    private Vector3 motion = Vector3.zero;
    private float gravity = 0f;

    private Vector2 moveInput;
    private Vector3 velocity;

    void Start()
    {
        charController = GetComponent<CharacterController>();
        playerCam = GetComponentInChildren<Camera>();
        //Cursor.lockState = CursorLockMode.Locked;
        cameraEuler = cameraPivot.localEulerAngles;
        currentMoveSpeed = walkSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        Debug.Log($"movement: {moveInput}");
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        currentMoveSpeed = Mathf.Lerp(walkSpeed, sprintSpeed, context.ReadValue<float>());
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (charController.isGrounded)
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

    public void ToggleCursorLock(InputAction.CallbackContext context)
    {
        Cursor.lockState = 1 - Cursor.lockState;

    }

    void Update()
    {
        //currentMoveSpeed = walkSpeed;
        grounded = charController.isGrounded;

        if (!charController.isGrounded)
        {
            velocity.y += Time.deltaTime * gravityForce; 
        }

        velocity.x = 0f;
        velocity.z = 0f;

        velocity += moveInput.x * currentMoveSpeed * transform.right;
        velocity += moveInput.y * currentMoveSpeed * transform.forward;

        charController.Move(Time.deltaTime * velocity);

        if (charController.isGrounded)
        {
            velocity.y = Time.deltaTime * gravityForce;
        }

        currentCameraLean = Mathf.Lerp(currentCameraLean, -1f * cameraLeanAmount * moveInput.x, cameraLeanSpeed);
        cameraEuler.y = 0f;
        cameraEuler.z = currentCameraLean;
        cameraPivot.localEulerAngles = cameraEuler;

    }


    private Vector3 GetMovementInput()
    {
        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");
        float yInput = (Input.GetKey(KeyCode.Space) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftControl) ? -1f : 0f);
        Vector3 inputAxis = new Vector3(xInput, yInput, zInput);

        return Vector3.ClampMagnitude(inputAxis, 1f);
    }
    private Vector3 GetAimInput()
    {
        float yawInput = lookSens * Input.GetAxis("Mouse X");
        float pitchInput = lookSens * Input.GetAxis("Mouse Y");

        currentCameraLean = Mathf.Lerp(currentCameraLean, -1f * cameraLeanAmount * yawInput, cameraLeanSpeed);

        transform.Rotate(transform.up, yawInput);
        float camEulerX = cameraPivot.transform.localEulerAngles.x;
        camEulerX = camEulerX - pitchInput;

        return new Vector3(camEulerX, 0f, currentCameraLean);

    }

    private float ComputeGravity()
    {
        if (grounded)
        {
            gravity = Time.deltaTime * gravityForce;
            
        } 
        else
        {
            gravity = gravity + Time.deltaTime * gravityForce;
            gravity = Mathf.Clamp(gravity, -100f, 100f);
        }

        return gravity;
    }

    public GameObject FindAimTarget()
    {
        RaycastHit hit;
        Ray aimRay = new Ray(playerCam.transform.position, playerCam.transform.forward);
        aimingAtTarget = Physics.Raycast(aimRay, out hit, targetAimRange, targetAimLayers);
        if (aimingAtTarget) {
            return hit.collider.gameObject;
        } else
        {
            return null;
        }
    }

    [Button(name="press me", size = 50, color = "black")]
    private void ButtonFunction()
    {
        Debug.Log("Pressed inspector button!");
    }

    private void OnDrawGizmos()
    {
        Vector3 lineVector = motion;
        lineVector.y = 0f;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + lineVector);

        if (playerCam == null) playerCam = GetComponentInChildren<Camera>();
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerCam.transform.position, playerCam.transform.position + playerCam.transform.forward * targetAimRange);
    }

}
