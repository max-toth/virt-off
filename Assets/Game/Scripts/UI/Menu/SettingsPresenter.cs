using System;
using Game.Scripts.Settings;
using Game.Scripts.Voice;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    internal sealed class SettingsPresenter
    {
        private const string SelectedTabClass = "tab--selected";

        private readonly Button[] _tabButtons;
        private readonly VisualElement[] _tabs;
        private readonly AudioSettingsPresenter _audio;
        private readonly ControlsSettingsPresenter _controls;
        private readonly GraphicsSettingsPresenter _graphics;
        private readonly GameSettings _settings;

        public SettingsPresenter(VisualElement root, Action back, GameSettings settings, VoiceManager voice)
        {
            _settings = settings;
            _tabButtons = new[] { root.Q<Button>("audioTabButton"), root.Q<Button>("controlsTabButton"), root.Q<Button>("graphicsTabButton") };
            _tabs = new[] { root.Q("audioTab"), root.Q("controlsTab"), root.Q("graphicsTab") };
            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var index = i;
                _tabButtons[i].clicked += () => SelectTab(index);
            }

            root.Q<Button>("resetSettingsButton").clicked += ResetToDefaults;
            root.Q<Button>("backButton").clicked += () =>
            {
                _settings.Save();
                back();
            };

            _audio = new AudioSettingsPresenter(root, settings, voice);
            _controls = new ControlsSettingsPresenter(root, settings);
            _graphics = new GraphicsSettingsPresenter(root, settings);
            SelectTab(0);
        }

        public void Load()
        {
            _audio.Load();
            _controls.Load();
            _graphics.Load();
        }

        public void Update() => _audio.Update();

        private void ResetToDefaults()
        {
            _settings.ResetToDefaults();
            Load();
        }

        private void SelectTab(int index)
        {
            for (var i = 0; i < _tabs.Length; i++)
            {
                _tabButtons[i].EnableInClassList(SelectedTabClass, i == index);
                _tabs[i].SetVisible(i == index);
            }
        }
    }
}
