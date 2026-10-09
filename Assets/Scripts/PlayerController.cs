using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float gravity = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    private CharacterController controller;
    private Camera playerCamera;
    private float verticalRotation;
    private float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (kb == null || ms == null) return;

        // Look
        Vector2 lookDelta = ms.delta.ReadValue() * mouseSensitivity * 0.01f;
        transform.Rotate(0, lookDelta.x, 0);

        verticalRotation = Mathf.Clamp(verticalRotation - lookDelta.y, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        // Move
        Vector2 moveInput = Vector2.zero;
        if (kb.wKey.isPressed) moveInput.y = 1;
        if (kb.sKey.isPressed) moveInput.y = -1;
        if (kb.aKey.isPressed) moveInput.x = -1;
        if (kb.dKey.isPressed) moveInput.x = 1;
        moveInput = moveInput.normalized;

        Vector3 move = transform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y));
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * walkSpeed * Time.deltaTime);

        if (controller.isGrounded)
            verticalVelocity = 0;
    }
}
