using UnityEngine;

namespace Game.Scripts.Settings
{
    /// <summary>Хранение настроек в PlayerPrefs.</summary>
    public sealed class GameSettingsStorage
    {
        private const string MicDeviceKey = "settings.micDevice";
        private const string MicGainKey = "settings.micGain";
        private const string MasterVolumeKey = "settings.masterVolume";
        private const string VoiceVolumeKey = "settings.voiceVolume";
        private const string MouseSensitivityKey = "settings.mouseSensitivity";
        private const string InvertYKey = "settings.invertY";
        private const string FieldOfViewKey = "settings.fov";
        private const string VSyncKey = "settings.vsync";
        private const string QualityKey = "settings.quality";
        private const string NicknameKey = "profile.nickname";
        private const string ServerAddressKey = "profile.serverAddress";

        public bool HasMouseSensitivity => PlayerPrefs.HasKey(MouseSensitivityKey);

        public void Load(GameSettings settings)
        {
            settings.MicDevice = PlayerPrefs.GetString(MicDeviceKey, string.Empty);
            settings.MicGain = PlayerPrefs.GetFloat(MicGainKey, GameSettings.DefaultMicGain);
            settings.MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, GameSettings.DefaultMasterVolume);
            settings.VoiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, GameSettings.DefaultVoiceVolume);
            settings.MouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, GameSettings.DefaultMouseSensitivity);
            settings.InvertY = GetBool(InvertYKey, false);
            settings.FieldOfView = PlayerPrefs.GetFloat(FieldOfViewKey, GameSettings.DefaultFieldOfView);
            settings.VSync = GetBool(VSyncKey, true);
            settings.QualityLevel = PlayerPrefs.GetInt(QualityKey, GameSettings.ProjectQualityLevel);
            settings.Nickname = PlayerPrefs.GetString(NicknameKey, string.Empty);
            settings.ServerAddress = PlayerPrefs.GetString(ServerAddressKey, string.Empty);
        }

        public void Save(GameSettings settings)
        {
            PlayerPrefs.SetString(MicDeviceKey, settings.MicDevice);
            PlayerPrefs.SetFloat(MicGainKey, settings.MicGain);
            PlayerPrefs.SetFloat(MasterVolumeKey, settings.MasterVolume);
            PlayerPrefs.SetFloat(VoiceVolumeKey, settings.VoiceVolume);
            PlayerPrefs.SetFloat(MouseSensitivityKey, settings.MouseSensitivity);
            SetBool(InvertYKey, settings.InvertY);
            PlayerPrefs.SetFloat(FieldOfViewKey, settings.FieldOfView);
            SetBool(VSyncKey, settings.VSync);
            PlayerPrefs.SetInt(QualityKey, settings.QualityLevel);
            PlayerPrefs.SetString(NicknameKey, settings.Nickname);
            PlayerPrefs.SetString(ServerAddressKey, settings.ServerAddress);
            PlayerPrefs.Save();
        }

        private static bool GetBool(string key, bool defaultValue) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;

        private static void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
    }
}
