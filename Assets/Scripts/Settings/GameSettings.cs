using System;
using UnityEngine;

// Пользовательские настройки клиента. Хранятся в PlayerPrefs.
public static class GameSettings
{
    const string KeyMicDevice = "settings.micDevice";
    const string KeyMicGain = "settings.micGain";
    const string KeyMasterVolume = "settings.masterVolume";
    const string KeyVoiceVolume = "settings.voiceVolume";
    const string KeyMouseSensitivity = "settings.mouseSensitivity";
    const string KeyInvertY = "settings.invertY";
    const string KeyFieldOfView = "settings.fov";
    const string KeyVSync = "settings.vsync";
    const string KeyQuality = "settings.quality";

    public const float DefaultMicGain = 1f;
    public const float DefaultMasterVolume = 1f;
    public const float DefaultVoiceVolume = 1f;
    public const float DefaultMouseSensitivity = 2f;
    public const float DefaultFieldOfView = 60f;

    // Вызывается при смене выбранного микрофона
    public static event Action MicDeviceChanged;

    static string micDevice = "";

    // Пустая строка — первый доступный микрофон
    public static string MicDevice
    {
        get => micDevice;
        set
        {
            value ??= "";
            if (micDevice == value) return;
            micDevice = value;
            MicDeviceChanged?.Invoke();
        }
    }

    public static float MicGain = DefaultMicGain;           // 0..3
    public static bool MicMuted;                            // не сохраняется между запусками
    public static float MasterVolume = DefaultMasterVolume; // 0..1
    public static float VoiceVolume = DefaultVoiceVolume;   // 0..2, читается из аудиопотока
    public static float MouseSensitivity = DefaultMouseSensitivity;
    public static bool InvertY;
    public static float FieldOfView = DefaultFieldOfView;
    public static bool VSync = true;
    public static int QualityLevel = -1;                    // -1 — уровень проекта по умолчанию

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        MicDeviceChanged = null;
        Load();
        Apply();
    }

    // Микрофон, который реально будет использован: сохранённый, если он подключён, иначе первый
    public static string ResolveMicDevice()
    {
        var devices = Microphone.devices;
        if (devices.Length == 0) return null;
        if (!string.IsNullOrEmpty(micDevice) && Array.IndexOf(devices, micDevice) >= 0)
            return micDevice;
        return devices[0];
    }

    public static void Load()
    {
        micDevice = PlayerPrefs.GetString(KeyMicDevice, "");
        MicGain = PlayerPrefs.GetFloat(KeyMicGain, DefaultMicGain);
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, DefaultMasterVolume);
        VoiceVolume = PlayerPrefs.GetFloat(KeyVoiceVolume, DefaultVoiceVolume);
        MouseSensitivity = PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity);
        InvertY = PlayerPrefs.GetInt(KeyInvertY, 0) != 0;
        FieldOfView = PlayerPrefs.GetFloat(KeyFieldOfView, DefaultFieldOfView);
        VSync = PlayerPrefs.GetInt(KeyVSync, 1) != 0;
        QualityLevel = PlayerPrefs.GetInt(KeyQuality, -1);
    }

    public static void Save()
    {
        PlayerPrefs.SetString(KeyMicDevice, micDevice);
        PlayerPrefs.SetFloat(KeyMicGain, MicGain);
        PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
        PlayerPrefs.SetFloat(KeyVoiceVolume, VoiceVolume);
        PlayerPrefs.SetFloat(KeyMouseSensitivity, MouseSensitivity);
        PlayerPrefs.SetInt(KeyInvertY, InvertY ? 1 : 0);
        PlayerPrefs.SetFloat(KeyFieldOfView, FieldOfView);
        PlayerPrefs.SetInt(KeyVSync, VSync ? 1 : 0);
        PlayerPrefs.SetInt(KeyQuality, QualityLevel);
        PlayerPrefs.Save();
    }

    // Применяет глобальные настройки движка. Остальное читают компоненты сами.
    public static void Apply()
    {
        AudioListener.volume = MasterVolume;
        // Уровень качества сбрасывает vSyncCount, поэтому он применяется первым
        if (QualityLevel >= 0 && QualityLevel < QualitySettings.names.Length
            && QualitySettings.GetQualityLevel() != QualityLevel)
            QualitySettings.SetQualityLevel(QualityLevel, true);
        QualitySettings.vSyncCount = VSync ? 1 : 0;
    }

    public static void ResetToDefaults()
    {
        MicDevice = "";
        MicGain = DefaultMicGain;
        MasterVolume = DefaultMasterVolume;
        VoiceVolume = DefaultVoiceVolume;
        MouseSensitivity = DefaultMouseSensitivity;
        InvertY = false;
        FieldOfView = DefaultFieldOfView;
        VSync = true;
        QualityLevel = -1;
        Apply();
    }
}
