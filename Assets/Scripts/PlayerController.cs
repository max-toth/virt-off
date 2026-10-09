using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float gravity = -9.81f;

    [Header("Jump")]
    [Tooltip("Высота прыжка в метрах")]
    public float jumpHeight = 1f;

    [Header("Crouch")]
    [Tooltip("Высота CharacterController в присяде")]
    public float crouchHeight = 1f;
    [Tooltip("Скорость перехода стоя/присед")]
    public float crouchTransitionSpeed = 8f;

    [Header("Look")]
    public float maxLookAngle = 80f;
    [Tooltip("Чувствительность мыши по умолчанию (меняется в настройках)")]
    public float mouseSensitivity = 2f;

    [Header("Respawn")]
    [Tooltip("Насколько ниже точки старта игрок может упасть, прежде чем его вернёт на старт")]
    public float fallResetDepth = 30f;

    public static PlayerController Local { get; private set; }

    // Управление игроком включено (используется меню для захвата курсора)
    public static bool ControlsActive { get; private set; }

    public bool IsCrouching { get; private set; }
    public bool IsRunning { get; private set; }

    private CharacterController controller;
    private Camera playerCamera;
    private float verticalRotation;
    private float verticalVelocity;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private float standHeight;
    private Vector3 standCenter;
    private Vector3 standCameraPos;

    void Awake()
    {
        Local = this;
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        standHeight = controller.height;
        standCenter = controller.center;
        standCameraPos = playerCamera.transform.localPosition;

        if (!PlayerPrefs.HasKey(GameSettings.KeyMouseSensitivity))
            GameSettings.MouseSensitivity = mouseSensitivity;
    }

    void OnDestroy()
    {
        if (Local == this) Local = null;
    }

    // Вернуть игрока в начальную позицию (например, если провалился сквозь пол)
    public void Respawn()
    {
        // CharacterController перезаписывает transform, пока включён
        controller.enabled = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        IsCrouching = false;
        SetHeight(standHeight);
        controller.enabled = true;
        verticalVelocity = 0;
        verticalRotation = 0;
        playerCamera.transform.localRotation = Quaternion.identity;
        Debug.Log("[Player] Respawned");
    }

    void OnEnable()
    {
        ControlsActive = true;
        if (GameMenu.IsOpen) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        ControlsActive = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (kb == null || ms == null) return;

        playerCamera.fieldOfView = GameSettings.FieldOfView;

        if (transform.position.y < spawnPosition.y - fallResetDepth)
        {
            Respawn();
            return;
        }

        if (GameMenu.IsOpen)
        {
            IsRunning = false;
            Move(Vector3.zero);
            return;
        }

        // Look
        Vector2 lookDelta = ms.delta.ReadValue() * GameSettings.MouseSensitivity * 0.01f;
        if (GameSettings.InvertY) lookDelta.y = -lookDelta.y;
        transform.Rotate(0, lookDelta.x, 0);

        verticalRotation = Mathf.Clamp(verticalRotation - lookDelta.y, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        // Присед (удерживать Ctrl). Встать можно, только если над головой свободно.
        bool wantCrouch = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
        if (wantCrouch) IsCrouching = true;
        else if (IsCrouching && CanStandUp()) IsCrouching = false;
        UpdateCrouchHeight();

        // Move
        Vector2 moveInput = Vector2.zero;
        if (kb.wKey.isPressed) moveInput.y = 1;
        if (kb.sKey.isPressed) moveInput.y = -1;
        if (kb.aKey.isPressed) moveInput.x = -1;
        if (kb.dKey.isPressed) moveInput.x = 1;
        moveInput = moveInput.normalized;

        // Бег (Shift) — только вперёд и не в присяде
        bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        IsRunning = shift && !IsCrouching && moveInput.y > 0;
        float speed = IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed);

        // Прыжок (пробел) — с земли и не в присяде
        if (kb.spaceKey.wasPressedThisFrame && controller.isGrounded && !IsCrouching)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        Vector3 horizontal = transform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y)) * speed;
        Move(horizontal);
    }

    // Горизонтальная скорость в м/с + гравитация
    void Move(Vector3 horizontal)
    {
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = horizontal;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // Небольшая прижимающая скорость, чтобы isGrounded не мигал на ступеньках и склонах
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
    }

    bool CanStandUp()
    {
        float radius = controller.radius * 0.95f;
        Vector3 bottom = transform.position + controller.center + Vector3.up * (controller.height / 2f - radius);
        float distance = standHeight - controller.height;
        if (distance <= 0.01f) return true;
        return !Physics.SphereCast(bottom, radius, Vector3.up, out _, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    void UpdateCrouchHeight()
    {
        float target = IsCrouching ? crouchHeight : standHeight;
        if (Mathf.Approximately(controller.height, target)) return;
        SetHeight(Mathf.MoveTowards(controller.height, target, crouchTransitionSpeed * Time.deltaTime));
    }

    // Меняет высоту капсулы, не отрывая ноги от пола; камера опускается вместе с головой
    void SetHeight(float height)
    {
        float delta = standHeight - height;
        controller.height = height;
        controller.center = standCenter - Vector3.up * (delta / 2f);
        playerCamera.transform.localPosition = standCameraPos - Vector3.up * delta;
    }
}
