using System;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    internal sealed class SliderBinding
    {
        private readonly Slider _slider;
        private readonly Label _value;
        private readonly Func<float, string> _format;

        public SliderBinding(VisualElement root, string sliderName, string valueName, Func<float, string> format, Action<float> changed)
        {
            _slider = root.Q<Slider>(sliderName);
            _value = root.Q<Label>(valueName);
            _format = format;
            _slider.RegisterValueChangedCallback(evt =>
            {
                changed(evt.newValue);
                _value.text = _format(evt.newValue);
            });
        }

        public static string Percent(float value) => $"{value * 100f:F0}%";

        public void Show(float value)
        {
            _slider.SetValueWithoutNotify(value);
            _value.text = _format(value);
        }
    }
}
