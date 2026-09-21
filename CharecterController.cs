using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Assign your Main Camera here (the one Cinemachine drives).")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f; // how fast the mesh turns to face camera yaw
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController controller;
    private InputSystem_Actions controls; // generated class from the project-wide Input Actions asset

    private Vector2 moveInput;
    private float verticalVelocity;
    private bool jumpPressed;
    private bool isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        // Only the local player needs input + a camera reference.
        if (!IsOwner)
        {
            enabled = false; // skip Update() entirely for remote clones
            return;
        }

        controls = new InputSystem_Actions();
        controls.Player.Enable();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Player.Jump.performed += ctx => jumpPressed = true;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && controls != null)
            controls.Player.Disable();
    }

    private void Update()
    {
        GroundCheck();
        Move();
        ApplyGravityAndJump();
    }

    private void GroundCheck()
    {
        Vector3 spherePos = transform.position + Vector3.up * (controller.radius - 0.05f);
        isGrounded = Physics.CheckSphere(spherePos, controller.radius + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
    }

    private void Move()
    {
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);

        if (input.sqrMagnitude > 0.01f && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * input.z + camRight * input.x;
            controller.Move(moveDir * moveSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ApplyGravityAndJump()
    {
        if (jumpPressed && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        jumpPressed = false;

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}