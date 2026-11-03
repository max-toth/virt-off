using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    internal sealed class GraphicsSettingsPresenter
    {
        private const int WindowedIndex = 0;
        private const int FullScreenIndex = 1;

        private readonly DropdownField _screenMode;
        private readonly DropdownField _resolution;
        private readonly DropdownField _quality;
        private readonly Toggle _vsync;
        private readonly SliderBinding _fieldOfView;
        private readonly List<Resolution> _resolutions = new();
        private readonly GameSettings _settings;

        public GraphicsSettingsPresenter(VisualElement root, GameSettings settings)
        {
            _settings = settings;
            _screenMode = root.Q<DropdownField>("screenModeDropdown");
            _resolution = root.Q<DropdownField>("resolutionDropdown");
            _quality = root.Q<DropdownField>("qualityDropdown");
            _vsync = root.Q<Toggle>("vsyncToggle");

            _screenMode.RegisterValueChangedCallback(_ =>
                Screen.fullScreenMode = _screenMode.index == WindowedIndex ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow);
            _quality.RegisterValueChangedCallback(_ =>
            {
                _settings.QualityLevel = _quality.index;
                _settings.Apply();
            });
            _vsync.RegisterValueChangedCallback(evt =>
            {
                _settings.VSync = evt.newValue;
                _settings.Apply();
            });
            root.Q<Button>("applyResolutionButton").clicked += ApplyResolution;

            _fieldOfView = new SliderBinding(root, "fieldOfViewSlider", "fieldOfViewValue",
                value => $"{value:F0}°", value => _settings.FieldOfView = Mathf.Round(value));
        }

        public void Load()
        {
            var screenModeIndex = Screen.fullScreenMode == FullScreenMode.Windowed ? WindowedIndex : FullScreenIndex;
            _screenMode.SetValueWithoutNotify(_screenMode.choices[screenModeIndex]);

            var qualityNames = QualitySettings.names;
            _quality.choices = qualityNames.ToList();
            _quality.SetValueWithoutNotify(qualityNames[QualitySettings.GetQualityLevel()]);

            LoadResolutions();
            _vsync.SetValueWithoutNotify(_settings.VSync);
            _fieldOfView.Show(_settings.FieldOfView);
        }

        private void LoadResolutions()
        {
            _resolutions.Clear();
            foreach (var resolution in Screen.resolutions)
            {
                if (!_resolutions.Exists(added => added.width == resolution.width && added.height == resolution.height))
                    _resolutions.Add(resolution);
            }

            _resolution.choices = _resolutions.Select(resolution => $"{resolution.width} × {resolution.height}").ToList();
            _resolution.SetEnabled(_resolutions.Count > 0);
            if (_resolutions.Count == 0) return;

            var current = _resolutions.FindIndex(resolution => resolution.width == Screen.width && resolution.height == Screen.height);
            _resolution.SetValueWithoutNotify(_resolution.choices[current >= 0 ? current : _resolutions.Count - 1]);
        }

        private void ApplyResolution()
        {
            var index = _resolution.index;
            if (index < 0 || index >= _resolutions.Count) return;

            var resolution = _resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
        }
    }
}
