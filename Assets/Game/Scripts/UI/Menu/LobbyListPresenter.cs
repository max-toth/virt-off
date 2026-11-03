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
        private bool _connected;
        private bool _dirty = true;

        public LobbyListPresenter(VisualElement root, VisualTreeAsset rowTemplate)
        {
            _connection = root.Q<Label>("connectionLabel");
            _count = root.Q<Label>("lobbyCountLabel");
            _hint = root.Q<Label>("playerListHint");
            _list = root.Q<ScrollView>("playerList");
            _rowTemplate = rowTemplate;
            _self = new PlayerRowView(rowTemplate, () => GameSettings.MicMuted = !GameSettings.MicMuted);

            Lobby.PlayerJoined += OnLobbyChanged;
            Lobby.PlayerLeft += OnLobbyChanged;
        }

        public void Dispose()
        {
            Lobby.PlayerJoined -= OnLobbyChanged;
            Lobby.PlayerLeft -= OnLobbyChanged;
        }

        public void Update()
        {
            var connected = GameNet.TryGetConnected(out var manager);
            if (_dirty || connected != _connected) Rebuild(connected, manager);
            if (!connected) return;

            ShowSelf();
            foreach (var (player, view) in _others) ShowOther(player, view);
        }

        private void OnLobbyChanged(string id) => _dirty = true;

        private void Rebuild(bool connected, INetTransport manager)
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

            _self.SetName($"Вы (игрок {manager.LocalId})");
            _list.Add(_self.Root);

            foreach (var player in Lobby.Players.OrderBy(player => player.Id, StringComparer.Ordinal))
            {
                var view = new PlayerRowView(_rowTemplate, () => player.Muted = !player.Muted);
                view.SetName($"Игрок {player.Id}");
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
            var muted = GameSettings.MicMuted;
            var speaking = !muted && VoiceManager.TryGetInstance(out var voice) && voice.IsReady && voice.MicLevel > SpeakingMicLevel;
            _self.Show(muted, speaking,
                muted ? "микрофон выключен" : speaking ? "говорите" : string.Empty,
                muted ? "Включить микрофон" : "Выключить микрофон");
        }

        private static void ShowOther(LobbyPlayer player, PlayerRowView view)
        {
            view.Show(player.Muted, player.IsSpeaking,
                player.Muted ? "заглушён" : player.IsSpeaking ? "говорит" : string.Empty,
                player.Muted ? "Включить звук" : "Заглушить");
        }
    }
}
