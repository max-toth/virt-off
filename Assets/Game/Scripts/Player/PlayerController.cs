using Game.Scripts.Controls;
using Game.Scripts.Settings;
using UnityEngine;
using VContainer;

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
        [Tooltip("Расстояние от глаз до камеры в виде от третьего лица (клавиша V), м")]
        [SerializeField] private float thirdPersonDistance = 3f;

        [Header("Physics")]
        [Tooltip("Как быстро гасится отброс от удара на земле")]
        [SerializeField] private float knockbackDamping = 5f;

        [Header("Respawn")]
        [Tooltip("Насколько ниже точки старта игрок может упасть, прежде чем его вернёт на старт")]
        [SerializeField] private float fallResetDepth = 30f;

        private CharacterController _controller;
        private Camera _camera;
        private Transform _view;
        private PlayerLook _look;
        private ThirdPersonCamera _thirdPerson;
        private Nameplate _ownNameplate;
        private PlayerCrouch _crouch;
        private PlayerMotor _motor;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private GameSettings _settings;
        private GameplayInput _input;
        private PlayerInputReader _inputReader;

        /// <summary>Управление включено; меню по нему решает, захватывать ли курсор после закрытия.</summary>
        public bool ControlsActive { get; private set; }

        public bool IsCrouching => _crouch.IsCrouching;

        /// <summary>Глаза игрока: отсюда целятся и подбирают предметы. В виде от третьего лица камера позади них.</summary>
        public Transform View => _view != null ? _view : _view = CreateView();

        [Inject]
        public void Construct(GameSettings settings, GameplayInput input, PlayerInputReader inputReader)
        {
            _settings = settings;
            _input = input;
            _inputReader = inputReader;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _camera = GetComponentInChildren<Camera>();
            transform.GetPositionAndRotation(out _spawnPosition, out _spawnRotation);

            var view = View;
            _look = new PlayerLook(transform, view, maxLookAngle);
            _crouch = new PlayerCrouch(_controller, view, crouchHeight, crouchTransitionSpeed);
            _motor = new PlayerMotor(_controller, gravity, jumpHeight, knockbackDamping);
            _thirdPerson = new ThirdPersonCamera(view, _camera.transform, thirdPersonDistance);

            // Свой ник виден только со стороны.
            _ownNameplate = Nameplate.Create(transform, _camera.transform);
            _ownNameplate.SetVisible(false);
            RefreshOwnNameplate();
            _settings.NicknameChanged += RefreshOwnNameplate;

            _settings.ApplyDefaultMouseSensitivity(mouseSensitivity);

            SetupKinematicBody();
        }

        private void OnEnable()
        {
            ControlsActive = true;
            if (_input.IsBlocked) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.NicknameChanged -= RefreshOwnNameplate;
        }

        private void OnDisable()
        {
            ControlsActive = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (!_inputReader.TryRead(out var input)) return;

            _camera.fieldOfView = _settings.FieldOfView;

            if (transform.position.y < _spawnPosition.y - fallResetDepth)
            {
                Respawn();
                return;
            }

            if (_input.IsBlocked)
            {
                _motor.Move(Vector3.zero);
                return;
            }

            _look.Update(input.Look);
            _crouch.Update(input.Crouch);
            if (input.ToggleView) ToggleView();

            var isRunning = input.Run && !IsCrouching && input.Move.y > 0f;
            var speed = IsCrouching ? crouchSpeed : isRunning ? runSpeed : walkSpeed;

            if (input.Jump && !IsCrouching) _motor.Jump();
            _motor.Move(transform.TransformDirection(new Vector3(input.Move.x, 0f, input.Move.y)) * speed);
        }

        private void LateUpdate() => _thirdPerson.Update();

        private void ToggleView()
        {
            _thirdPerson.Toggle();
            _ownNameplate.SetVisible(_thirdPerson.IsActive);
        }

        private void RefreshOwnNameplate()
        {
            var nickname = _settings.Nickname;
            _ownNameplate.Text = nickname.Length > 0 ? nickname : "Вы";
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

        // Камера уходит в дочерние к точке глаз: взгляд и присед двигают глаза, а вид от третьего лица — только камеру.
        // Создаётся по первому запросу: PlayerCombat получает глаза при внедрении, до Awake.
        private Transform CreateView()
        {
            var cameraTransform = GetComponentInChildren<Camera>().transform;
            var view = new GameObject("View").transform;
            view.SetParent(cameraTransform.parent, false);
            view.SetLocalPositionAndRotation(cameraTransform.localPosition, cameraTransform.localRotation);
            cameraTransform.SetParent(view, false);
            cameraTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return view;
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
