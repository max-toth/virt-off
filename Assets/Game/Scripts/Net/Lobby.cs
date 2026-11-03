using System;
using System.Collections.Generic;
using Game.Scripts.Common;
using Game.Scripts.Net.Models;
using Game.Scripts.Net.Packets;
using VContainer.Unity;

namespace Game.Scripts.Net
{
    /// <summary>Игроки в комнате. Списка от сервера нет: игрок появляется с первым пакетом голоса или позиции
    /// и уходит по PLAYER_LEFT, а если тот потерялся — когда долго нет пакетов.</summary>
    public sealed class Lobby : IInitializable, ITickable, IDisposable
    {
        private const float TimeoutSeconds = 5f;
        private const float ExpiryCheckInterval = 1f;

        private readonly PacketRouter _router;
        private readonly IClock _clock;
        private readonly Dictionary<string, LobbyPlayer> _playersById = new(StringComparer.Ordinal);
        private readonly List<string> _expired = new();
        private float _nextExpiryCheck;

        public Lobby(PacketRouter router, IClock clock)
        {
            _router = router;
            _clock = clock;
        }

        /// <summary>Новый игрок; аргумент — его id.</summary>
        public event Action<string> PlayerJoined;

        /// <summary>Игрок ушёл или от него долго нет пакетов; аргумент — его id.</summary>
        public event Action<string> PlayerLeft;

        public IReadOnlyCollection<LobbyPlayer> Players => _playersById.Values;

        public bool IsMuted(string id) => _playersById.TryGetValue(id, out var player) && player.Muted;

        public bool IsSpeaking(LobbyPlayer player) => player.IsSpeaking(_clock.UnscaledTime);

        public void MarkSpeaking(string id)
        {
            if (_playersById.TryGetValue(id, out var player)) player.MarkSpeaking(_clock.UnscaledTime);
        }

        public void Initialize()
        {
            _router.Subscribe(PacketType.Voice, OnPlayerPacket);
            _router.Subscribe(PacketType.Position, OnPlayerPacket);
            _router.Subscribe(PacketType.PlayerLeft, OnPlayerLeft);
        }

        public void Tick() => RemoveExpired();

        public void Dispose()
        {
            _router.Unsubscribe(PacketType.Voice, OnPlayerPacket);
            _router.Unsubscribe(PacketType.Position, OnPlayerPacket);
            _router.Unsubscribe(PacketType.PlayerLeft, OnPlayerLeft);
        }

        /// <summary>Убирает игроков, от которых давно нет пакетов. Проверка — не чаще раза в секунду.</summary>
        public void RemoveExpired()
        {
            var now = _clock.UnscaledTime;
            if (now < _nextExpiryCheck) return;
            _nextExpiryCheck = now + ExpiryCheckInterval;

            foreach (var player in _playersById.Values)
            {
                if (now - player.LastSeen > TimeoutSeconds) _expired.Add(player.Id);
            }

            foreach (var id in _expired) Remove(id);
            _expired.Clear();
        }

        // VOICE и POSITION начинаются с [sender:str].
        private void OnPlayerPacket(string localId, byte[] data)
        {
            var reader = new PacketReader(data);
            var id = reader.ReadString();
            if (reader.Failed || id == localId) return;

            if (!_playersById.TryGetValue(id, out var player))
            {
                player = new LobbyPlayer(id);
                _playersById.Add(id, player);
                NetDiagnostics.Log($"[Lobby] {id} joined");
                PlayerJoined?.Invoke(id);
            }

            player.LastSeen = _clock.UnscaledTime;
        }

        // PLAYER_LEFT: [type][id:str]
        private void OnPlayerLeft(string localId, byte[] data)
        {
            var reader = new PacketReader(data);
            var id = reader.ReadString();
            if (!reader.Failed) Remove(id);
        }

        private void Remove(string id)
        {
            if (!_playersById.Remove(id)) return;
            NetDiagnostics.Log($"[Lobby] {id} left");
            PlayerLeft?.Invoke(id);
        }
    }
}
