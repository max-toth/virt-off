using System;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Net;
using Game.Scripts.Net.Models;
using Game.Scripts.Settings;
using Game.Scripts.Voice;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    internal sealed class LobbyListPresenter : IDisposable
    {
        private const float SpeakingMicLevel = 0.05f;

        private readonly Label _connection;
        private readonly Label _count;
        private readonly Label _hint;
        private readonly ScrollView _list;
        private readonly VisualTreeAsset _rowTemplate;
        private readonly PlayerRowView _self;
        private readonly List<KeyValuePair<LobbyPlayer, PlayerRowView>> _others = new();
        private readonly Lobby _lobby;
        private readonly INetTransport _transport;
        private readonly GameSettings _settings;
        private readonly VoiceManager _voice;
        private readonly PlayerNames _names;
        private bool _connected;
        private bool _dirty = true;

        public LobbyListPresenter(VisualElement root, VisualTreeAsset rowTemplate, Lobby lobby, INetTransport transport,
            GameSettings settings, VoiceManager voice, PlayerNames names)
        {
            _names = names;
            _lobby = lobby;
            _transport = transport;
            _settings = settings;
            _voice = voice;
            _connection = root.Q<Label>("connectionLabel");
            _count = root.Q<Label>("lobbyCountLabel");
            _hint = root.Q<Label>("playerListHint");
            _list = root.Q<ScrollView>("playerList");
            _rowTemplate = rowTemplate;
            _self = new PlayerRowView(rowTemplate, () => _settings.MicMuted = !_settings.MicMuted);

            _lobby.PlayerJoined += OnLobbyChanged;
            _lobby.PlayerLeft += OnLobbyChanged;
            _names.Changed += OnNamesChanged;
        }

        public void Dispose()
        {
            _lobby.PlayerJoined -= OnLobbyChanged;
            _lobby.PlayerLeft -= OnLobbyChanged;
            _names.Changed -= OnNamesChanged;
        }

        public void Update()
        {
            var connected = _transport.IsConnected;
            if (_dirty || connected != _connected) Rebuild(connected);
            if (!connected) return;

            ShowSelf();
            foreach (var (player, view) in _others) ShowOther(player, view);
        }

        private void OnLobbyChanged(string id) => _dirty = true;

        private void OnNamesChanged() => _dirty = true;

        private void Rebuild(bool connected)
        {
            _dirty = false;
            _connected = connected;
            _list.Clear();
            _others.Clear();

            _connection.text = connected ? "● Подключено к серверу" : "○ Подключение к серверу…";
            if (!connected)
            {
                _count.text = "В лобби: 0";
                ShowHint("Список появится после подключения");
                return;
            }

            var ownName = _names.Get(_transport.LocalId) ?? _settings.Nickname;
            _self.SetName(ownName.Length > 0 ? $"Вы — {ownName}" : $"Вы (игрок {_transport.LocalId})");
            _list.Add(_self.Root);

            foreach (var player in _lobby.Players.OrderBy(player => _names.DisplayName(player.Id), StringComparer.CurrentCultureIgnoreCase))
            {
                var view = new PlayerRowView(_rowTemplate, () => player.Muted = !player.Muted);
                view.SetName(_names.DisplayName(player.Id));
                _list.Add(view.Root);
                _others.Add(new KeyValuePair<LobbyPlayer, PlayerRowView>(player, view));
            }

            _count.text = $"В лобби: {_others.Count + 1}";
            ShowHint(_others.Count == 0 ? "Кроме вас, в лобби пока никого нет" : null);
        }

        private void ShowHint(string text)
        {
            _hint.text = text ?? string.Empty;
            _hint.SetVisible(text != null);
        }

        private void ShowSelf()
        {
            var muted = _settings.MicMuted;
            var speaking = !muted && _voice.IsReady && _voice.MicLevel > SpeakingMicLevel;
            _self.Show(muted, speaking,
                muted ? "микрофон выключен" : speaking ? "говорите" : string.Empty,
                muted ? "Включить микрофон" : "Выключить микрофон");
        }

        private void ShowOther(LobbyPlayer player, PlayerRowView view)
        {
            var speaking = _lobby.IsSpeaking(player);
            view.Show(player.Muted, speaking,
                player.Muted ? "заглушён" : speaking ? "говорит" : string.Empty,
                player.Muted ? "Включить звук" : "Заглушить");
        }
    }
}
