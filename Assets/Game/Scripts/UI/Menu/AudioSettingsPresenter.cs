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
        private readonly GameSettings _settings;
        private readonly VoiceManager _voice;

        public AudioSettingsPresenter(VisualElement root, GameSettings settings, VoiceManager voice)
        {
            _settings = settings;
            _voice = voice;
            _micDropdown = root.Q<DropdownField>("micDropdown");
            _noMicLabel = root.Q<Label>("noMicLabel");
            _levelBar = root.Q<ProgressBar>("micLevelBar");
            _mutedToggle = root.Q<Toggle>("micMutedToggle");
            _testButton = root.Q<Button>("micTestButton");
            _testHint = root.Q<Label>("micTestHint");

            _micDropdown.RegisterValueChangedCallback(evt => _settings.MicDevice = evt.newValue);
            _mutedToggle.RegisterValueChangedCallback(evt => _settings.MicMuted = evt.newValue);
            _testButton.clicked += ToggleMicTest;
            root.Q<Button>("micRefreshButton").clicked += ShowDevices;
            root.Q<Button>("osSoundButton").clicked += OpenOsSoundSettings;

            _gain = new SliderBinding(root, "micGainSlider", "micGainValue", SliderBinding.Percent, value => _settings.MicGain = value);
            _masterVolume = new SliderBinding(root, "masterVolumeSlider", "masterVolumeValue", SliderBinding.Percent, value =>
            {
                _settings.MasterVolume = value;
                _settings.Apply();
            });
            _voiceVolume = new SliderBinding(root, "voiceVolumeSlider", "voiceVolumeValue", SliderBinding.Percent, value => _settings.VoiceVolume = value);
        }

        public void Load()
        {
            ShowDevices();
            _gain.Show(_settings.MicGain);
            _masterVolume.Show(_settings.MasterVolume);
            _voiceVolume.Show(_settings.VoiceVolume);
        }

        public void Update()
        {
            var level = Mathf.Clamp01(_voice.MicLevel);
            _levelBar.value = level;
            _levelBar.EnableInClassList(WarningClass, level is > WarningLevel and <= DangerLevel);
            _levelBar.EnableInClassList(DangerClass, level > DangerLevel);

            _mutedToggle.SetValueWithoutNotify(_settings.MicMuted);

            var testAvailable = _voice.IsReady;
            _testButton.SetVisible(testAvailable);
            _testHint.SetVisible(!testAvailable);
            if (testAvailable) _testButton.text = _voice.IsTesting ? StopTestText : StartTestText;
        }

        private void ShowDevices()
        {
            var devices = Microphone.devices;
            var hasDevices = devices.Length > 0;
            _micDropdown.choices = new List<string>(devices);
            _micDropdown.SetValueWithoutNotify(_settings.ResolveMicDevice() ?? string.Empty);
            _micDropdown.SetEnabled(hasDevices);
            _noMicLabel.SetVisible(!hasDevices);
        }

        private void ToggleMicTest()
        {
            if (_voice.IsTesting) _voice.StopMicTest();
            else _voice.StartMicTest();
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
