using UnityEngine;

namespace Game.Scripts.Settings
{
    internal static class GameSettingsStorage
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

        public static bool HasMouseSensitivity => PlayerPrefs.HasKey(MouseSensitivityKey);

        public static void Load()
        {
            GameSettings.MicDevice = PlayerPrefs.GetString(MicDeviceKey, string.Empty);
            GameSettings.MicGain = PlayerPrefs.GetFloat(MicGainKey, GameSettings.DefaultMicGain);
            GameSettings.MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, GameSettings.DefaultMasterVolume);
            GameSettings.VoiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, GameSettings.DefaultVoiceVolume);
            GameSettings.MouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, GameSettings.DefaultMouseSensitivity);
            GameSettings.InvertY = GetBool(InvertYKey, false);
            GameSettings.FieldOfView = PlayerPrefs.GetFloat(FieldOfViewKey, GameSettings.DefaultFieldOfView);
            GameSettings.VSync = GetBool(VSyncKey, true);
            GameSettings.QualityLevel = PlayerPrefs.GetInt(QualityKey, GameSettings.ProjectQualityLevel);
        }

        public static void Save()
        {
            PlayerPrefs.SetString(MicDeviceKey, GameSettings.MicDevice);
            PlayerPrefs.SetFloat(MicGainKey, GameSettings.MicGain);
            PlayerPrefs.SetFloat(MasterVolumeKey, GameSettings.MasterVolume);
            PlayerPrefs.SetFloat(VoiceVolumeKey, GameSettings.VoiceVolume);
            PlayerPrefs.SetFloat(MouseSensitivityKey, GameSettings.MouseSensitivity);
            SetBool(InvertYKey, GameSettings.InvertY);
            PlayerPrefs.SetFloat(FieldOfViewKey, GameSettings.FieldOfView);
            SetBool(VSyncKey, GameSettings.VSync);
            PlayerPrefs.SetInt(QualityKey, GameSettings.QualityLevel);
            PlayerPrefs.Save();
        }

        private static bool GetBool(string key, bool defaultValue) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;

        private static void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
    }
}
