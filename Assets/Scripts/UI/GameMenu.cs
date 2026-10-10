using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Меню паузы (IMGUI): кто в лобби, возврат на старт, настройки, выход.
// Создаётся автоматически, открывается по Esc.
public class GameMenu : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    enum Page { Main, Settings }

    const int WindowId = 0x5E77;
    static readonly string[] Tabs = { "Звук", "Управление", "Графика" };
    static readonly string[] ScreenModes = { "В окне", "Полный экран" };
    static readonly Color SpeakingColor = new Color(0.3f, 0.9f, 0.3f);
    static readonly Color SilentColor = new Color(0.45f, 0.45f, 0.45f);
    static readonly Color MutedColor = new Color(0.9f, 0.3f, 0.3f);

    // Размер окна в «логических» пикселях, на экране умножается на UiScale
    const float WindowWidth = 600;
    const float WindowHeight = 540;
    const float ButtonHeight = 40;

    private Page page;
    private int tab;
    private bool confirmQuit;
    private Rect windowRect = new Rect(0, 0, WindowWidth, WindowHeight);
    private Vector2 playersScroll;
    private Vector2 micScroll;
    private string[] micDevices = new string[0];

    private List<Resolution> resolutions = new List<Resolution>();
    private int resolutionIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Application.isBatchMode) return;
        var go = new GameObject("GameMenu");
        DontDestroyOnLoad(go);
        go.AddComponent<GameMenu>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (!IsOpen) Open();
            else if (page != Page.Main) page = Page.Main;
            else Close();
        }
    }

    void Open()
    {
        IsOpen = true;
        page = Page.Main;
        confirmQuit = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshMicDevices();
        RefreshResolutions();
        float scale = UiScale;
        windowRect = new Rect(0, 0, WindowWidth, WindowHeight);
        windowRect.x = (Screen.width / scale - windowRect.width) / 2;
        windowRect.y = (Screen.height / scale - windowRect.height) / 2;
    }

    void Close()
    {
        IsOpen = false;
        if (PlayerController.ControlsActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        GameSettings.Save();
    }

    void OnApplicationQuit() => GameSettings.Save();

    void OnDestroy() => IsOpen = false;

    void RefreshMicDevices() => micDevices = Microphone.devices;

    void RefreshResolutions()
    {
        resolutions.Clear();
        foreach (var r in Screen.resolutions)
            if (!resolutions.Exists(x => x.width == r.width && x.height == r.height))
                resolutions.Add(r);

        resolutionIndex = resolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
        if (resolutionIndex < 0) resolutionIndex = resolutions.Count - 1;
    }

    void OnGUI()
    {
        GUI.depth = -100;

        if (!IsOpen)
        {
            GUI.Label(new Rect(10, Screen.height - 25, 300, 20), "Esc — меню");
            return;
        }

        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

        // Масштабируем всё меню (текст, слайдеры, кнопки) под размер экрана
        float scale = UiScale;
        var prevMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        string title = page == Page.Main ? "Меню" : "Настройки";
        windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow, title);

        // Не даём окну уехать за пределы экрана
        float w = Screen.width / scale, h = Screen.height / scale;
        windowRect.x = Mathf.Clamp(windowRect.x, 0, Mathf.Max(0, w - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Mathf.Max(0, h - windowRect.height));

        GUI.matrix = prevMatrix;
    }

    // 1.0 при высоте экрана 600px, 1.8 при 1080p. Окно не выходит за 90% экрана.
    float UiScale
    {
        get
        {
            float scale = Mathf.Max(1f, Screen.height / 600f);
            float fit = Mathf.Min(Screen.width * 0.9f / WindowWidth, Screen.height * 0.9f / WindowHeight);
            return Mathf.Max(0.5f, Mathf.Min(scale, fit));
        }
    }

    void DrawWindow(int id)
    {
        if (page == Page.Main) DrawMainPage();
        else DrawSettingsPage();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    void DrawMainPage()
    {
        var nm = GameNet.Instance;
        bool connected = nm != null && nm.IsConnected;
        GUILayout.Label(connected ? "● Подключено к серверу" : "○ Подключение к серверу…");
        GUILayout.Space(6);

        // Кто в лобби
        var others = Lobby.Players.OrderBy(p => p.Id).ToList();
        GUILayout.Label($"В лобби: {(connected ? others.Count + 1 : 0)}");

        playersScroll = GUILayout.BeginScrollView(playersScroll, GUI.skin.box, GUILayout.Height(200));
        if (connected)
        {
            DrawSelfRow(nm.LocalId);
            foreach (var p in others)
                DrawPlayerRow(p);
            if (others.Count == 0)
                GUILayout.Label("Кроме вас, в лобби пока никого нет");
        }
        else
        {
            GUILayout.Label("Список появится после подключения");
        }
        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace();

        // Действия
        if (GUILayout.Button("Продолжить", GUILayout.Height(ButtonHeight)))
            Close();

        GUI.enabled = PlayerController.Local != null;
        if (GUILayout.Button("Вернуться на старт (если провалились или застряли)", GUILayout.Height(ButtonHeight)))
        {
            PlayerController.Local.Respawn();
            Close();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Настройки", GUILayout.Height(ButtonHeight)))
            page = Page.Settings;

        if (!confirmQuit)
        {
            if (GUILayout.Button("Выход", GUILayout.Height(ButtonHeight)))
                confirmQuit = true;
        }
        else
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Выйти из приложения?", GUILayout.Height(ButtonHeight));
            if (GUILayout.Button("Да, выйти", GUILayout.Height(ButtonHeight), GUILayout.Width(140)))
                Quit();
            if (GUILayout.Button("Отмена", GUILayout.Height(ButtonHeight), GUILayout.Width(120)))
                confirmQuit = false;
            GUILayout.EndHorizontal();
        }
    }

    void DrawSelfRow(string localId)
    {
        var vm = VoiceManager.Instance;
        bool muted = GameSettings.MicMuted;
        bool speaking = !muted && vm != null && vm.IsReady && vm.MicLevel > 0.05f;

        GUILayout.BeginHorizontal();
        DrawDot(muted ? MutedColor : (speaking ? SpeakingColor : SilentColor));
        GUILayout.Label($"Вы (игрок {localId})");
        GUILayout.FlexibleSpace();
        GUILayout.Label(muted ? "микрофон выключен" : (speaking ? "говорите" : ""));
        if (GUILayout.Button(muted ? "Включить микрофон" : "Выключить микрофон", GUILayout.Width(170)))
            GameSettings.MicMuted = !muted;
        GUILayout.EndHorizontal();
    }

    void DrawPlayerRow(Lobby.Player p)
    {
        GUILayout.BeginHorizontal();
        DrawDot(p.Muted ? MutedColor : (p.IsSpeaking ? SpeakingColor : SilentColor));
        GUILayout.Label($"Игрок {p.Id}");
        GUILayout.FlexibleSpace();
        GUILayout.Label(p.Muted ? "заглушён" : (p.IsSpeaking ? "говорит" : ""));
        if (GUILayout.Button(p.Muted ? "Включить звук" : "Заглушить", GUILayout.Width(170)))
            p.Muted = !p.Muted;
        GUILayout.EndHorizontal();
    }

    static void DrawDot(Color color)
    {
        var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12), GUILayout.Height(20));
        rect = new Rect(rect.x, rect.y + 5, 10, 10);
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;
    }

    static void Quit()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void DrawSettingsPage()
    {
        tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(28));
        GUILayout.Space(10);

        GUI.changed = false;
        switch (tab)
        {
            case 0: DrawAudioTab(); break;
            case 1: DrawControlsTab(); break;
            case 2: DrawGraphicsTab(); break;
        }
        if (GUI.changed) GameSettings.Apply();

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Сбросить по умолчанию", GUILayout.Height(28)))
            GameSettings.ResetToDefaults();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Назад", GUILayout.Width(120), GUILayout.Height(28)))
        {
            GameSettings.Save();
            page = Page.Main;
        }
        GUILayout.EndHorizontal();
    }

    void DrawAudioTab()
    {
        // Микрофон
        GUILayout.BeginHorizontal();
        GUILayout.Label("Микрофон", GUILayout.Width(150));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Обновить список", GUILayout.Width(140)))
            RefreshMicDevices();
        GUILayout.EndHorizontal();

        if (micDevices.Length == 0)
        {
            GUILayout.Label("Микрофоны не найдены");
        }
        else
        {
            string current = GameSettings.ResolveMicDevice();
            micScroll = GUILayout.BeginScrollView(micScroll, GUILayout.Height(90));
            foreach (var device in micDevices)
            {
                bool selected = device == current;
                if (GUILayout.Toggle(selected, device) && !selected)
                    GameSettings.MicDevice = device;
            }
            GUILayout.EndScrollView();
        }

        var vm = VoiceManager.Instance;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Уровень", GUILayout.Width(150));
        DrawLevelMeter(vm != null ? vm.MicLevel : 0f);
        GUILayout.EndHorizontal();

        GameSettings.MicGain = Slider("Усиление микрофона", GameSettings.MicGain, 0f, 3f, $"{GameSettings.MicGain * 100:F0}%");

        GUILayout.BeginHorizontal();
        GameSettings.MicMuted = GUILayout.Toggle(GameSettings.MicMuted, " Выключить микрофон (M)");
        GUILayout.FlexibleSpace();
        if (vm != null && vm.IsReady)
        {
            if (GUILayout.Button(vm.IsTesting ? "Остановить и прослушать" : "Проверить микрофон", GUILayout.Width(200)))
            {
                if (vm.IsTesting) vm.StopMicTest();
                else vm.StartMicTest();
            }
        }
        else
        {
            GUILayout.Label("Проверка доступна после подключения к серверу");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(12);

        // Вывод
        GUILayout.BeginHorizontal();
        GUILayout.Label("Наушники / динамики", GUILayout.Width(150));
        GUILayout.Label("Системное устройство по умолчанию");
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Настройки звука ОС", GUILayout.Width(160)))
            OpenOsSoundSettings();
        GUILayout.EndHorizontal();

        GameSettings.MasterVolume = Slider("Общая громкость", GameSettings.MasterVolume, 0f, 1f, $"{GameSettings.MasterVolume * 100:F0}%");
        GameSettings.VoiceVolume = Slider("Громкость голосов", GameSettings.VoiceVolume, 0f, 2f, $"{GameSettings.VoiceVolume * 100:F0}%");
    }

    void DrawControlsTab()
    {
        GameSettings.MouseSensitivity = Slider("Чувствительность мыши", GameSettings.MouseSensitivity, 0.1f, 30f, GameSettings.MouseSensitivity.ToString("F1"));
        GameSettings.InvertY = GUILayout.Toggle(GameSettings.InvertY, " Инвертировать ось Y");

        GUILayout.Space(16);
        GUILayout.Label("Клавиши:");
        GUILayout.Label("WASD — движение\nShift — бег\nПробел — прыжок\nCtrl (удерживать) — присесть\nМышь — обзор\nЛКМ — удар кулаком\nПКМ — лещ\nE — взять предмет (стул и т.п.), ЛКМ — бросить\nM — вкл/выкл микрофон\nR (удерживать) — проверка микрофона\nU / J — громкость голосов\nEsc — меню");
    }

    void DrawGraphicsTab()
    {
        // Режим экрана
        GUILayout.BeginHorizontal();
        GUILayout.Label("Режим экрана", GUILayout.Width(150));
        int mode = Screen.fullScreenMode == FullScreenMode.Windowed ? 0 : 1;
        int newMode = GUILayout.Toolbar(mode, ScreenModes);
        if (newMode != mode)
            Screen.fullScreenMode = newMode == 0 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
        GUILayout.EndHorizontal();

        // Разрешение
        if (resolutions.Count > 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Разрешение", GUILayout.Width(150));
            if (GUILayout.Button("<", GUILayout.Width(30)))
                resolutionIndex = Mathf.Max(0, resolutionIndex - 1);
            var r = resolutions[resolutionIndex];
            GUILayout.Label($"{r.width} × {r.height}", GUILayout.Width(110));
            if (GUILayout.Button(">", GUILayout.Width(30)))
                resolutionIndex = Mathf.Min(resolutions.Count - 1, resolutionIndex + 1);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Применить", GUILayout.Width(100)))
                Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
            GUILayout.EndHorizontal();
        }

        // Качество
        var names = QualitySettings.names;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Качество", GUILayout.Width(150));
        int quality = QualitySettings.GetQualityLevel();
        int newQuality = GUILayout.SelectionGrid(quality, names, Mathf.Min(names.Length, 3));
        if (newQuality != quality) GameSettings.QualityLevel = newQuality;
        GUILayout.EndHorizontal();

        GameSettings.VSync = GUILayout.Toggle(GameSettings.VSync, " Вертикальная синхронизация");
        GameSettings.FieldOfView = Mathf.Round(Slider("Поле зрения", GameSettings.FieldOfView, 50f, 100f, $"{GameSettings.FieldOfView:F0}°"));
    }

    static float Slider(string label, float value, float min, float max, string valueText)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        value = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
        GUILayout.Label(valueText, GUILayout.Width(50));
        GUILayout.EndHorizontal();
        return value;
    }

    static void DrawLevelMeter(float level)
    {
        var rect = GUILayoutUtility.GetRect(100, 14, GUILayout.ExpandWidth(true));
        rect.y += 4;
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        level = Mathf.Clamp01(level);
        GUI.color = level > 0.9f ? Color.red : (level > 0.6f ? Color.yellow : Color.green);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * level, rect.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    static void OpenOsSoundSettings()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        Application.OpenURL("ms-settings:sound");
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        Application.OpenURL("x-apple.systempreferences:com.apple.preference.sound");
#endif
    }
}
