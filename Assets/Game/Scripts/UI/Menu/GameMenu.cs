using Game.Scripts.Bootstrap;
using Game.Scripts.Controls;
using Game.Scripts.Net;
using Game.Scripts.Net.Config;
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
        private const string StartTitle = "Virt-Off";

        [SerializeField] private VisualTreeAsset playerRowTemplate;

        private VisualElement _menuRoot;
        private VisualElement _mainPage;
        private VisualElement _settingsPage;
        private VisualElement _startPage;
        private VisualElement _quitConfirm;
        private Label _title;
        private Button _quitButton;
        private Button _respawnButton;
        private LobbyListPresenter _lobby;
        private SettingsPresenter _settingsPresenter;
        private StartPagePresenter _startPresenter;
        private bool _settingsShown;
        private bool _inStartMode;
        private GameplayInput _input;
        private GameSettings _settings;
        private PlayerController _player;
        private Lobby _lobbyService;
        private INetTransport _transport;
        private VoiceManager _voice;
        private PlayerNames _names;
        private NetworkSession _session;
        private StartupOptions _startup;
        private ServerEndpoint _mainServer;

        /// <summary>Меню открыто; игровой ввод в это время заблокирован через GameplayInput.</summary>
        public bool IsOpen { get; private set; }

        [Inject]
        public void Construct(GameplayInput input, GameSettings settings, PlayerController player, Lobby lobby,
            INetTransport transport, VoiceManager voice, PlayerNames names, NetworkSession session, StartupOptions startup,
            ServerEndpoint mainServer)
        {
            _names = names;
            _session = session;
            _startup = startup;
            _mainServer = mainServer;
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
            _startPage = root.Q("startPage");
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

            _lobby = new LobbyListPresenter(root, playerRowTemplate, _lobbyService, _transport, _settings, _voice, _names);
            _settingsPresenter = new SettingsPresenter(root, ShowHome, _settings, _voice);
            _startPresenter = new StartPagePresenter(_startPage, _settings, _mainServer, Play, ShowSettings, Quit);
            _menuRoot.SetVisible(IsOpen);
        }

        private void Start()
        {
            if (_startup.ShowStartMenu && !_session.IsStarted) OpenStart();
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
            else if (_settingsShown) ShowHome();
            else if (!_inStartMode) Close();
        }

        // До подключения меню не закрывается: играть можно только после выбора сервера.
        private void OpenStart()
        {
            _inStartMode = true;
            SetOpen(true);
            ShowHome();
            _settingsPresenter.Load();
            _startPresenter.Load();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private bool Play(ServerEndpoint endpoint)
        {
            if (!_session.Connect(endpoint)) return false;

            _inStartMode = false;
            Close();
            return true;
        }

        private void ShowHome()
        {
            if (_inStartMode) ShowStart();
            else ShowMain();
        }

        private void ShowStart()
        {
            _settingsShown = false;
            _title.text = StartTitle;
            _startPage.SetVisible(true);
            _mainPage.SetVisible(false);
            _settingsPage.SetVisible(false);
            _quitConfirm.SetVisible(false);
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
            _startPage.SetVisible(false);
            _mainPage.SetVisible(true);
            _settingsPage.SetVisible(false);
            SetQuitConfirmation(false);
        }

        private void ShowSettings()
        {
            _settingsShown = true;
            _title.text = SettingsTitle;
            _startPage.SetVisible(false);
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
