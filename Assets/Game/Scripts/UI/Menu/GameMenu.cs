using System;
using Game.Scripts.Player;
using Game.Scripts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Game.Scripts.UI.Menu
{
    // Меню паузы по Esc: игроки в лобби, возврат на старт, настройки, выход.
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
        private SettingsPresenter _settings;
        private bool _settingsShown;

        public static bool IsOpen { get; private set; }

        public static event Action<bool> OpenChanged;

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

            _lobby = new LobbyListPresenter(root, playerRowTemplate);
            _settings = new SettingsPresenter(root, ShowMain);
            _menuRoot.SetVisible(IsOpen);
        }

        private void OnDisable() => _lobby.Dispose();

        private void OnDestroy() => IsOpen = false;

        private void OnApplicationQuit() => GameSettings.Save();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) OnEscape();
            if (!IsOpen) return;

            _lobby.Update();
            _settings.Update();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            IsOpen = false;
            OpenChanged = null;
        }

        private static void Quit()
        {
            GameSettings.Save();
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
            _settings.Load();
            _respawnButton.SetEnabled(PlayerController.Local != null);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Close()
        {
            SetOpen(false);
            if (PlayerController.ControlsActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            GameSettings.Save();
        }

        private void SetOpen(bool open)
        {
            IsOpen = open;
            _menuRoot.SetVisible(open);
            OpenChanged?.Invoke(open);
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
            var player = PlayerController.Local;
            if (player != null) player.Respawn();
            Close();
        }
    }
}
