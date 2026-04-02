using System.Collections;
using System.Collections.Generic;
using VInspector;
using UnityEngine;

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

    private Vector3 motion = Vector3.zero;
    private float gravity = 0f;

    void Start()
    {
        charController = GetComponent<CharacterController>();
        playerCam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector3 walkInput = GetMovementInput();

        bool sprinting = Input.GetKey(sprintKey);
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, sprinting ? sprintSpeed : walkSpeed, 0.1f);

        grounded = charController.isGrounded;

        Vector3 newMotion = Vector3.zero;
        newMotion += walkInput.x * currentMoveSpeed * transform.right;
        newMotion += walkInput.z * currentMoveSpeed * transform.forward;


        // FLYING
        newMotion += walkInput.y * currentMoveSpeed * transform.up;

        motion = Vector3.Lerp(motion, newMotion, grounded ? 1f : airborneControl);
        motion.y = ComputeGravity();



        charController.Move(Time.deltaTime * motion);

        currentCameraLean = Mathf.Lerp(currentCameraLean, -1f * cameraLeanAmount * walkInput.x, cameraLeanSpeed);
        cameraPivot.transform.localEulerAngles = GetAimInput();

        Quaternion inverseQ = Quaternion.Inverse(cameraPivot.transform.localRotation);
        handPivot.transform.localRotation = Quaternion.Slerp(inverseQ, Quaternion.identity, handRotateAmount);

        motion = Vector3.zero;
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
        if (Input.GetKeyDown(jumpKey) && grounded)
        {
            gravity = jumpForce;
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
