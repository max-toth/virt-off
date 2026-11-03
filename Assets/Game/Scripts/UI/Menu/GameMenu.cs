using Game.Scripts.Controls;
using Game.Scripts.Net;
using Game.Scripts.Player;
using Game.Scripts.Settings;
using Game.Scripts.Voice;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using Cursor = UnityEngine.Cursor;

namespace Game.Scripts.UI.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class GameMenu : MonoBehaviour
    {
        private const string MainTitle = "Меню";
        private const string SettingsTitle = "Настройки";

        [SerializeField] private VisualTreeAsset playerRowTemplate;

        private VisualElement _menuRoot;
        private VisualElement _mainPage;
        private VisualElement _settingsPage;
        private VisualElement _quitConfirm;
        private Label _title;
        private Button _quitButton;
        private Button _respawnButton;
        private LobbyListPresenter _lobby;
        private SettingsPresenter _settingsPresenter;
        private bool _settingsShown;
        private GameplayInput _input;
        private GameSettings _settings;
        private PlayerController _player;
        private Lobby _lobbyService;
        private INetTransport _transport;
        private VoiceManager _voice;

        /// <summary>Меню открыто; игровой ввод в это время заблокирован через GameplayInput.</summary>
        public bool IsOpen { get; private set; }

        [Inject]
        public void Construct(GameplayInput input, GameSettings settings, PlayerController player, Lobby lobby,
            INetTransport transport, VoiceManager voice)
        {
            _input = input;
            _settings = settings;
            _player = player;
            _lobbyService = lobby;
            _transport = transport;
            _voice = voice;
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _menuRoot = root.Q("menuRoot");
            _mainPage = root.Q("mainPage");
            _settingsPage = root.Q("settingsPage");
            _quitConfirm = root.Q("quitConfirm");
            _title = root.Q<Label>("titleLabel");
            _quitButton = root.Q<Button>("quitButton");
            _respawnButton = root.Q<Button>("respawnButton");

            root.Q<Button>("resumeButton").clicked += Close;
            root.Q<Button>("settingsButton").clicked += ShowSettings;
            root.Q<Button>("quitCancelButton").clicked += () => SetQuitConfirmation(false);
            root.Q<Button>("quitConfirmButton").clicked += Quit;
            _quitButton.clicked += () => SetQuitConfirmation(true);
            _respawnButton.clicked += Respawn;

            _lobby = new LobbyListPresenter(root, playerRowTemplate, _lobbyService, _transport, _settings, _voice);
            _settingsPresenter = new SettingsPresenter(root, ShowMain, _settings, _voice);
            _menuRoot.SetVisible(IsOpen);
        }

        private void OnDisable() => _lobby.Dispose();

        private void OnDestroy()
        {
            IsOpen = false;
            _input?.SetBlocked(false);
        }

        private void OnApplicationQuit() => _settings.Save();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) OnEscape();
            if (!IsOpen) return;

            _lobby.Update();
            _settingsPresenter.Update();
        }

        private void Quit()
        {
            _settings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnEscape()
        {
            if (!IsOpen) Open();
            else if (_settingsShown) ShowMain();
            else Close();
        }

        private void Open()
        {
            SetOpen(true);
            ShowMain();
            _settingsPresenter.Load();
            _respawnButton.SetEnabled(_player != null);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Close()
        {
            SetOpen(false);
            if (_player.ControlsActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            _settings.Save();
        }

        private void SetOpen(bool open)
        {
            IsOpen = open;
            _input.SetBlocked(open);
            _menuRoot.SetVisible(open);
        }

        private void ShowMain()
        {
            _settingsShown = false;
            _title.text = MainTitle;
            _mainPage.SetVisible(true);
            _settingsPage.SetVisible(false);
            SetQuitConfirmation(false);
        }

        private void ShowSettings()
        {
            _settingsShown = true;
            _title.text = SettingsTitle;
            _mainPage.SetVisible(false);
            _settingsPage.SetVisible(true);
        }

        private void SetQuitConfirmation(bool visible)
        {
            _quitConfirm.SetVisible(visible);
            _quitButton.SetVisible(!visible);
        }

        private void Respawn()
        {
            _player.Respawn();
            Close();
        }
    }
}
