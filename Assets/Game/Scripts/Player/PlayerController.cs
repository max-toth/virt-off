using Game.Scripts.Controls;
using Game.Scripts.Settings;
using UnityEngine;

namespace Game.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Jump")]
        [Tooltip("Высота прыжка в метрах")]
        [SerializeField] private float jumpHeight = 1f;

        [Header("Crouch")]
        [Tooltip("Высота CharacterController в присяде")]
        [SerializeField] private float crouchHeight = 1f;
        [Tooltip("Скорость перехода стоя/присед")]
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Look")]
        [SerializeField] private float maxLookAngle = 80f;
        [Tooltip("Чувствительность мыши по умолчанию (меняется в настройках)")]
        [SerializeField] private float mouseSensitivity = 2f;

        [Header("Physics")]
        [Tooltip("Как быстро гасится отброс от удара на земле")]
        [SerializeField] private float knockbackDamping = 5f;

        [Header("Respawn")]
        [Tooltip("Насколько ниже точки старта игрок может упасть, прежде чем его вернёт на старт")]
        [SerializeField] private float fallResetDepth = 30f;

        private CharacterController _controller;
        private Camera _camera;
        private PlayerLook _look;
        private PlayerCrouch _crouch;
        private PlayerMotor _motor;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        /// <summary>Свой игрок в сцене.</summary>
        public static PlayerController Local { get; private set; }
        /// <summary>Управление включено; меню по нему решает, захватывать ли курсор после закрытия.</summary>
        public static bool ControlsActive { get; private set; }

        public bool IsCrouching => _crouch.IsCrouching;

        private void Awake()
        {
            Local = this;
            _controller = GetComponent<CharacterController>();
            _camera = GetComponentInChildren<Camera>();
            transform.GetPositionAndRotation(out _spawnPosition, out _spawnRotation);

            var cameraTransform = _camera.transform;
            _look = new PlayerLook(transform, cameraTransform, maxLookAngle);
            _crouch = new PlayerCrouch(_controller, cameraTransform, crouchHeight, crouchTransitionSpeed);
            _motor = new PlayerMotor(_controller, gravity, jumpHeight, knockbackDamping);

            GameSettings.ApplyDefaultMouseSensitivity(mouseSensitivity);

            SetupKinematicBody();
        }

        private void OnEnable()
        {
            ControlsActive = true;
            if (GameplayInput.IsBlocked) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            ControlsActive = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            if (Local == this) Local = null;
        }

        private void Update()
        {
            if (!PlayerInputReader.TryRead(out var input)) return;

            _camera.fieldOfView = GameSettings.FieldOfView;

            if (transform.position.y < _spawnPosition.y - fallResetDepth)
            {
                Respawn();
                return;
            }

            if (GameplayInput.IsBlocked)
            {
                _motor.Move(Vector3.zero);
                return;
            }

            _look.Update(input.Look);
            _crouch.Update(input.Crouch);

            var isRunning = input.Run && !IsCrouching && input.Move.y > 0f;
            var speed = IsCrouching ? crouchSpeed : isRunning ? runSpeed : walkSpeed;

            if (input.Jump && !IsCrouching) _motor.Jump();
            _motor.Move(transform.TransformDirection(new Vector3(input.Move.x, 0f, input.Move.y)) * speed);
        }

        /// <summary>Отброс от удара, м/с.</summary>
        public void AddImpulse(Vector3 velocity) => _motor.AddImpulse(velocity);

        /// <summary>Тряска камеры, градусы.</summary>
        public void Shake(float degrees) => _look.AddShake(degrees);

        /// <summary>Возвращает игрока на точку старта.</summary>
        public void Respawn()
        {
            // CharacterController перезаписывает transform, пока включён.
            _controller.enabled = false;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            _crouch.Stand();
            _controller.enabled = true;
            _motor.Stop();
            _look.Reset();
        }

        // Кинематический Rigidbody: двигает CharacterController, но предметы отскакивают от игрока.
        private void SetupKinematicBody()
        {
            if (!TryGetComponent(out Rigidbody body)) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
        }
    }
}
