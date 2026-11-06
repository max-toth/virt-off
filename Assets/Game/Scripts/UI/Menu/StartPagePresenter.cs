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

        private readonly TextField _nickname;
        private readonly DropdownField _server;
        private readonly TextField _address;
        private readonly Label _error;
        private readonly GameSettings _settings;
        private readonly List<KeyValuePair<string, ServerEndpoint>> _presets;

        /// <param name="mainServer">Сервер из ServerConfig.</param>
        /// <param name="play">Получает выбранный сервер; возвращает false, если подключиться не удалось.</param>
        public StartPagePresenter(VisualElement root, GameSettings settings, ServerEndpoint mainServer,
            Func<ServerEndpoint, bool> play, Action showSettings, Action quit)
        {
            _settings = settings;
            _nickname = root.Q<TextField>("nicknameField");
            _server = root.Q<DropdownField>("serverDropdown");
            _address = root.Q<TextField>("serverAddressField");
            _error = root.Q<Label>("startError");

            _presets = new List<KeyValuePair<string, ServerEndpoint>>
            {
                new($"Основной ({mainServer})", mainServer),
                new($"Локальный ({ServerEndpoint.Local})", ServerEndpoint.Local)
            };

            var choices = new List<string>();
            foreach (var preset in _presets) choices.Add(preset.Key);
            choices.Add(CustomServerChoice);
            _server.choices = choices;
            _server.RegisterValueChangedCallback(evt => OnServerChosen(evt.newValue));

            _nickname.maxLength = GameSettings.MaxNicknameLength;
            root.Q<Button>("playButton").clicked += () => Play(play);
            root.Q<Button>("startSettingsButton").clicked += showSettings;
            root.Q<Button>("startQuitButton").clicked += quit;
        }

        /// <summary>Заполняет поля сохранёнными ником и сервером.</summary>
        public void Load()
        {
            _nickname.SetValueWithoutNotify(_settings.Nickname);
            ShowError(null);

            var saved = _settings.ServerAddress;
            if (string.IsNullOrEmpty(saved)) saved = _presets[0].Value.ToString();
            _address.SetValueWithoutNotify(saved);
            _server.SetValueWithoutNotify(FindPreset(saved) ?? CustomServerChoice);
            _address.SetVisible(_server.value == CustomServerChoice);
        }

        private void OnServerChosen(string choice)
        {
            foreach (var preset in _presets)
            {
                if (preset.Key == choice) _address.SetValueWithoutNotify(preset.Value.ToString());
            }

            _address.SetVisible(choice == CustomServerChoice);
            ShowError(null);
        }

        private void Play(Func<ServerEndpoint, bool> play)
        {
            if (!ServerEndpoint.TryParse(_address.value, out var endpoint))
            {
                ShowError("Адрес сервера: хост или хост:порт, например 127.0.0.1:7777");
                return;
            }

            _settings.Nickname = _nickname.value;
            _settings.ServerAddress = endpoint.ToString();
            _settings.Save();

            if (!play(endpoint)) ShowError($"Не удалось найти сервер {endpoint.Host}");
        }

        private string FindPreset(string address)
        {
            foreach (var preset in _presets)
            {
                if (preset.Value.ToString() == address) return preset.Key;
            }

            return null;
        }

        private void ShowError(string text)
        {
            _error.text = text ?? string.Empty;
            _error.SetVisible(text != null);
        }
    }
}
