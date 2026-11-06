using System;
using UnityEngine;

namespace Game.Scripts.Settings
{
    /// <summary>Настройки клиента. Загружаются при создании; Save сохраняет их, Apply применяет громкость и графику к движку,
    /// остальное компоненты читают сами.</summary>
    public sealed class GameSettings
    {
        public const float DefaultMicGain = 1f;
        public const float DefaultMasterVolume = 1f;
        public const float DefaultVoiceVolume = 1f;
        public const float DefaultMouseSensitivity = 2f;
        public const float DefaultFieldOfView = 60f;
        public const int ProjectQualityLevel = -1;
        public const int MaxNicknameLength = 24;

        private const float MaxVoiceVolume = 2f;

        private readonly GameSettingsStorage _storage;
        private string _micDevice = string.Empty;
        private string _nickname = string.Empty;
        private volatile float _voiceVolume = DefaultVoiceVolume;

        public GameSettings(GameSettingsStorage storage)
        {
            _storage = storage;
            _storage.Load(this);
            Apply();
        }

        public event Action MicDeviceChanged;

        /// <summary>Ник сменился: его нужно заново отправить серверу.</summary>
        public event Action NicknameChanged;

        /// <summary>Имя над персонажем, до MaxNicknameLength символов; пустая строка — другие видят «Игрок id».
        /// Не сбрасывается вместе с настройками.</summary>
        public string Nickname
        {
            get => _nickname;
            set
            {
                value = (value ?? string.Empty).Trim();
                if (value.Length > MaxNicknameLength) value = value.Substring(0, MaxNicknameLength).TrimEnd();
                if (_nickname == value) return;
                _nickname = value;
                NicknameChanged?.Invoke();
            }
        }

        /// <summary>Последний сервер из стартового меню, «хост:порт»; пустая строка — сервер из ServerConfig.</summary>
        public string ServerAddress { get; set; } = string.Empty;

        /// <summary>Выбранный микрофон; пустая строка — первый доступный.</summary>
        public string MicDevice
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

        public float MicGain { get; set; } = DefaultMicGain;
        public bool MicMuted { get; set; }
        public float MasterVolume { get; set; } = DefaultMasterVolume;

        /// <summary>Читается из аудиопотока.</summary>
        public float VoiceVolume
        {
            get => _voiceVolume;
            set => _voiceVolume = Mathf.Clamp(value, 0f, MaxVoiceVolume);
        }

        public float MouseSensitivity { get; set; } = DefaultMouseSensitivity;
        public bool InvertY { get; set; }
        public float FieldOfView { get; set; } = DefaultFieldOfView;
        public bool VSync { get; set; } = true;
        public int QualityLevel { get; set; } = ProjectQualityLevel;

        public void Save() => _storage.Save(this);

        /// <summary>Значение из сцены используется, пока игрок не сохранил свою чувствительность.</summary>
        public void ApplyDefaultMouseSensitivity(float value)
        {
            if (!_storage.HasMouseSensitivity) MouseSensitivity = value;
        }

        /// <summary>Сохранённый микрофон, если он подключён, иначе первый доступный.</summary>
        /// <returns>null — микрофонов нет.</returns>
        public string ResolveMicDevice()
        {
            var devices = Microphone.devices;
            if (devices.Length == 0) return null;
            return !string.IsNullOrEmpty(_micDevice) && Array.IndexOf(devices, _micDevice) >= 0 ? _micDevice : devices[0];
        }

        public void Apply()
        {
            AudioListener.volume = MasterVolume;

            // Смена уровня качества сбрасывает vSyncCount, поэтому применяется первой.
            if (QualityLevel >= 0 && QualityLevel < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != QualityLevel)
                QualitySettings.SetQualityLevel(QualityLevel, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
        }

        public void ResetToDefaults()
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
    }
}
