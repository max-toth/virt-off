using System;
using System.Collections.Generic;
using Game.Scripts.Net.Config;
using Game.Scripts.Settings;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    /// <summary>Стартовая страница: ник и сервер. «Зайти» отдаёт выбранный адрес; подключается вызывающий.</summary>
    internal sealed class StartPagePresenter
    {
        private const string CustomServerChoice = "Другой адрес";
        private const string InvalidAddressError = "Адрес сервера: хост или хост:порт, например 127.0.0.1:7777";

        private readonly TextField _nickname;
        private readonly DropdownField _server;
        private readonly TextField _address;
        private readonly Label _error;
        private readonly GameSettings _settings;
        private readonly Func<ServerEndpoint, bool> _play;
        private readonly List<ServerPreset> _presets;

        /// <param name="mainServer">Сервер из ServerConfig.</param>
        /// <param name="play">Получает выбранный сервер; возвращает false, если подключиться не удалось.</param>
        public StartPagePresenter(VisualElement root, GameSettings settings, ServerEndpoint mainServer,
            Func<ServerEndpoint, bool> play, Action showSettings, Action quit)
        {
            _settings = settings;
            _play = play;
            _nickname = root.Q<TextField>("nicknameField");
            _server = root.Q<DropdownField>("serverDropdown");
            _address = root.Q<TextField>("serverAddressField");
            _error = root.Q<Label>("startError");

            _presets = new List<ServerPreset>
            {
                new($"Основной ({mainServer})", mainServer),
                new($"Локальный ({ServerEndpoint.Local})", ServerEndpoint.Local)
            };

            var choices = _presets.ConvertAll(preset => preset.Label);
            choices.Add(CustomServerChoice);
            _server.choices = choices;
            _server.RegisterValueChangedCallback(evt => OnServerChosen(evt.newValue));

            _nickname.maxLength = GameSettings.MaxNicknameLength;
            root.Q<Button>("playButton").clicked += Play;
            root.Q<Button>("startSettingsButton").clicked += showSettings;
            root.Q<Button>("startQuitButton").clicked += quit;
        }

        /// <summary>Заполняет поля сохранёнными ником и сервером.</summary>
        public void Load()
        {
            _nickname.SetValueWithoutNotify(_settings.Nickname);
            ShowError(null);

            var saved = _settings.ServerAddress;
            if (string.IsNullOrEmpty(saved)) saved = _presets[0].Address;

            var preset = _presets.Find(p => p.Address == saved);
            _address.SetValueWithoutNotify(saved);
            _server.SetValueWithoutNotify(preset?.Label ?? CustomServerChoice);
            _address.SetVisible(preset == null);
        }

        private void OnServerChosen(string choice)
        {
            var preset = _presets.Find(p => p.Label == choice);
            if (preset != null) _address.SetValueWithoutNotify(preset.Address);

            _address.SetVisible(preset == null);
            ShowError(null);
        }

        private void Play()
        {
            if (!ServerEndpoint.TryParse(_address.value, out var endpoint))
            {
                ShowError(InvalidAddressError);
                return;
            }

            _settings.Nickname = _nickname.value;
            _settings.ServerAddress = endpoint.ToString();
            _settings.Save();

            if (!_play(endpoint)) ShowError($"Не удалось найти сервер {endpoint.Host}");
        }

        private void ShowError(string text)
        {
            _error.text = text ?? string.Empty;
            _error.SetVisible(text != null);
        }

        private sealed class ServerPreset
        {
            public ServerPreset(string label, ServerEndpoint endpoint)
            {
                Label = label;
                Address = endpoint.ToString();
            }

            /// <summary>Пункт выпадающего списка.</summary>
            public string Label { get; }

            /// <summary>Адрес «хост:порт», как его показывает поле ввода.</summary>
            public string Address { get; }
        }
    }
}
