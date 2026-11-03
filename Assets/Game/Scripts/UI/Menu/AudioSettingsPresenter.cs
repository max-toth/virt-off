using System.Collections.Generic;
using Game.Scripts.Settings;
using Game.Scripts.Voice;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    internal sealed class AudioSettingsPresenter
    {
        private const float WarningLevel = 0.6f;
        private const float DangerLevel = 0.9f;
        private const string WarningClass = "level-meter--warning";
        private const string DangerClass = "level-meter--danger";
        private const string StartTestText = "Проверить микрофон";
        private const string StopTestText = "Остановить и прослушать";

        private readonly DropdownField _micDropdown;
        private readonly Label _noMicLabel;
        private readonly ProgressBar _levelBar;
        private readonly Toggle _mutedToggle;
        private readonly Button _testButton;
        private readonly Label _testHint;
        private readonly SliderBinding _gain;
        private readonly SliderBinding _masterVolume;
        private readonly SliderBinding _voiceVolume;

        public AudioSettingsPresenter(VisualElement root)
        {
            _micDropdown = root.Q<DropdownField>("micDropdown");
            _noMicLabel = root.Q<Label>("noMicLabel");
            _levelBar = root.Q<ProgressBar>("micLevelBar");
            _mutedToggle = root.Q<Toggle>("micMutedToggle");
            _testButton = root.Q<Button>("micTestButton");
            _testHint = root.Q<Label>("micTestHint");

            _micDropdown.RegisterValueChangedCallback(evt => GameSettings.MicDevice = evt.newValue);
            _mutedToggle.RegisterValueChangedCallback(evt => GameSettings.MicMuted = evt.newValue);
            _testButton.clicked += ToggleMicTest;
            root.Q<Button>("micRefreshButton").clicked += ShowDevices;
            root.Q<Button>("osSoundButton").clicked += OpenOsSoundSettings;

            _gain = new SliderBinding(root, "micGainSlider", "micGainValue", SliderBinding.Percent, value => GameSettings.MicGain = value);
            _masterVolume = new SliderBinding(root, "masterVolumeSlider", "masterVolumeValue", SliderBinding.Percent, value =>
            {
                GameSettings.MasterVolume = value;
                GameSettings.Apply();
            });
            _voiceVolume = new SliderBinding(root, "voiceVolumeSlider", "voiceVolumeValue", SliderBinding.Percent, value => GameSettings.VoiceVolume = value);
        }

        public void Load()
        {
            ShowDevices();
            _gain.Show(GameSettings.MicGain);
            _masterVolume.Show(GameSettings.MasterVolume);
            _voiceVolume.Show(GameSettings.VoiceVolume);
        }

        public void Update()
        {
            var hasVoice = VoiceManager.TryGetInstance(out var voice);
            var level = hasVoice ? Mathf.Clamp01(voice.MicLevel) : 0f;
            _levelBar.value = level;
            _levelBar.EnableInClassList(WarningClass, level > WarningLevel && level <= DangerLevel);
            _levelBar.EnableInClassList(DangerClass, level > DangerLevel);

            _mutedToggle.SetValueWithoutNotify(GameSettings.MicMuted);

            var testAvailable = hasVoice && voice.IsReady;
            _testButton.SetVisible(testAvailable);
            _testHint.SetVisible(!testAvailable);
            if (testAvailable) _testButton.text = voice.IsTesting ? StopTestText : StartTestText;
        }

        private void ShowDevices()
        {
            var devices = Microphone.devices;
            var hasDevices = devices.Length > 0;
            _micDropdown.choices = new List<string>(devices);
            _micDropdown.SetValueWithoutNotify(GameSettings.ResolveMicDevice() ?? string.Empty);
            _micDropdown.SetEnabled(hasDevices);
            _noMicLabel.SetVisible(!hasDevices);
        }

        private static void ToggleMicTest()
        {
            if (!VoiceManager.TryGetInstance(out var voice)) return;
            if (voice.IsTesting) voice.StopMicTest();
            else voice.StartMicTest();
        }

        private static void OpenOsSoundSettings()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Application.OpenURL("ms-settings:sound");
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Application.OpenURL("x-apple.systempreferences:com.apple.preference.sound");
#endif
        }
    }
}
