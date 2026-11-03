using System;
using UnityEngine;

namespace Game.Scripts.Settings
{
    /// <summary>Настройки клиента. Save сохраняет их в PlayerPrefs, Apply применяет громкость и графику к движку; остальное компоненты читают сами.</summary>
    public static class GameSettings
    {
        public const float DefaultMicGain = 1f;
        public const float DefaultMasterVolume = 1f;
        public const float DefaultVoiceVolume = 1f;
        public const float DefaultMouseSensitivity = 2f;
        public const float DefaultFieldOfView = 60f;
        public const int ProjectQualityLevel = -1;

        private const float MaxVoiceVolume = 2f;

        private static string _micDevice = string.Empty;
        private static float _voiceVolume = DefaultVoiceVolume;

        public static event Action MicDeviceChanged;

        /// <summary>Выбранный микрофон; пустая строка — первый доступный.</summary>
        public static string MicDevice
        {
            get => _micDevice;
            set
            {
                value ??= string.Empty;
                if (_micDevice == value) return;
                _micDevice = value;
                MicDeviceChanged?.Invoke();
            }
        }

        public static float MicGain { get; set; } = DefaultMicGain;
        public static bool MicMuted { get; set; }
        public static float MasterVolume { get; set; } = DefaultMasterVolume;

        public static float VoiceVolume
        {
            get => _voiceVolume;
            set => _voiceVolume = Mathf.Clamp(value, 0f, MaxVoiceVolume);
        }

        public static float MouseSensitivity { get; set; } = DefaultMouseSensitivity;
        public static bool InvertY { get; set; }
        public static float FieldOfView { get; set; } = DefaultFieldOfView;
        public static bool VSync { get; set; } = true;
        public static int QualityLevel { get; set; } = ProjectQualityLevel;

        public static void Save() => GameSettingsStorage.Save();

        /// <summary>Значение из сцены используется, пока игрок не сохранил свою чувствительность.</summary>
        public static void ApplyDefaultMouseSensitivity(float value)
        {
            if (!GameSettingsStorage.HasMouseSensitivity) MouseSensitivity = value;
        }

        /// <summary>Сохранённый микрофон, если он подключён, иначе первый доступный.</summary>
        /// <returns>null — микрофонов нет.</returns>
        public static string ResolveMicDevice()
        {
            var devices = Microphone.devices;
            if (devices.Length == 0) return null;
            return !string.IsNullOrEmpty(_micDevice) && Array.IndexOf(devices, _micDevice) >= 0 ? _micDevice : devices[0];
        }

        public static void Apply()
        {
            AudioListener.volume = MasterVolume;

            // Смена уровня качества сбрасывает vSyncCount, поэтому применяется первой.
            if (QualityLevel >= 0 && QualityLevel < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != QualityLevel)
                QualitySettings.SetQualityLevel(QualityLevel, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
        }

        public static void ResetToDefaults()
        {
            MicDevice = string.Empty;
            MicGain = DefaultMicGain;
            MasterVolume = DefaultMasterVolume;
            VoiceVolume = DefaultVoiceVolume;
            MouseSensitivity = DefaultMouseSensitivity;
            InvertY = false;
            FieldOfView = DefaultFieldOfView;
            VSync = true;
            QualityLevel = ProjectQualityLevel;
            Apply();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            MicDeviceChanged = null;
            MicMuted = false;
            GameSettingsStorage.Load();
            Apply();
        }
    }
}
