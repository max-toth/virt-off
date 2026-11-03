using Game.Scripts.Settings;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    internal sealed class ControlsSettingsPresenter
    {
        private readonly SliderBinding _sensitivity;
        private readonly Toggle _invertY;

        public ControlsSettingsPresenter(VisualElement root)
        {
            _sensitivity = new SliderBinding(root, "sensitivitySlider", "sensitivityValue",
                value => value.ToString("F1"), value => GameSettings.MouseSensitivity = value);
            _invertY = root.Q<Toggle>("invertYToggle");
            _invertY.RegisterValueChangedCallback(evt => GameSettings.InvertY = evt.newValue);
        }

        public void Load()
        {
            _sensitivity.Show(GameSettings.MouseSensitivity);
            _invertY.SetValueWithoutNotify(GameSettings.InvertY);
        }
    }
}
