using Game.Scripts.Settings;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    internal sealed class ControlsSettingsPresenter
    {
        private readonly SliderBinding _sensitivity;
        private readonly Toggle _invertY;
        private readonly GameSettings _settings;

        public ControlsSettingsPresenter(VisualElement root, GameSettings settings)
        {
            _settings = settings;
            _sensitivity = new SliderBinding(root, "sensitivitySlider", "sensitivityValue",
                value => value.ToString("F1"), value => _settings.MouseSensitivity = value);
            _invertY = root.Q<Toggle>("invertYToggle");
            _invertY.RegisterValueChangedCallback(evt => _settings.InvertY = evt.newValue);
        }

        public void Load()
        {
            _sensitivity.Show(_settings.MouseSensitivity);
            _invertY.SetValueWithoutNotify(_settings.InvertY);
        }
    }
}
