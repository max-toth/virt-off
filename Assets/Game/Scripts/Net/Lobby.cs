using System;
using System.Collections.Generic;
using Game.Scripts.Net.Models;
using Game.Scripts.Net.Packets;
using UnityEngine;

namespace Game.Scripts.Net
{
    /// <summary>Игроки в комнате. Сервер не присылает список: игрок считается присутствующим, пока от него приходят пакеты голоса или позиции.</summary>
    public static class Lobby
    {
        private const float TimeoutSeconds = 5f;
        private const float ExpiryCheckInterval = 1f;

        private static readonly Dictionary<string, LobbyPlayer> PlayersById = new(StringComparer.Ordinal);
        private static readonly List<string> Expired = new();
        private static PacketRouter _router;
        private static float _nextExpiryCheck;

        /// <summary>Новый игрок; аргумент — его id.</summary>
        public static event Action<string> PlayerJoined;

        /// <summary>От игрока долго нет пакетов; аргумент — его id.</summary>
        public static event Action<string> PlayerLeft;

        public static IReadOnlyCollection<LobbyPlayer> Players => PlayersById.Values;

        public static bool IsMuted(string id) => PlayersById.TryGetValue(id, out var player) && player.Muted;

        public static void MarkSpeaking(string id)
        {
            if (PlayersById.TryGetValue(id, out var player)) player.MarkSpeaking();
        }

        public static void Initialize(PacketRouter router)
        {
            Unsubscribe();
            _router = router;
            router.Subscribe(PacketType.Voice, OnPlayerPacket);
            router.Subscribe(PacketType.Position, OnPlayerPacket);
        }

        /// <summary>Отписывается от пакетов, забывает игроков и подписчиков событий.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Shutdown()
        {
            Unsubscribe();
            _nextExpiryCheck = 0f;
            PlayersById.Clear();
            PlayerJoined = null;
            PlayerLeft = null;
        }

        internal static void RemoveExpired()
        {
            var now = Time.unscaledTime;
            if (now < _nextExpiryCheck) return;
            _nextExpiryCheck = now + ExpiryCheckInterval;

            foreach (var player in PlayersById.Values)
            {
                if (now - player.LastSeen > TimeoutSeconds) Expired.Add(player.Id);
            }

            foreach (var id in Expired)
            {
                PlayersById.Remove(id);
                NetDiagnostics.Log($"[Lobby] {id} left");
                PlayerLeft?.Invoke(id);
            }

            Expired.Clear();
        }

        private static void Unsubscribe()
        {
            if (_router == null) return;
            _router.Unsubscribe(PacketType.Voice, OnPlayerPacket);
            _router.Unsubscribe(PacketType.Position, OnPlayerPacket);
            _router = null;
        }

        // VOICE и POSITION начинаются с [sender:str].
        private static void OnPlayerPacket(string localId, byte[] data)
        {
            var reader = new PacketReader(data);
            var id = reader.ReadString();
            if (reader.Failed || id == localId) return;

            if (!PlayersById.TryGetValue(id, out var player))
            {
                player = new LobbyPlayer(id);
                PlayersById.Add(id, player);
                NetDiagnostics.Log($"[Lobby] {id} joined");
                PlayerJoined?.Invoke(id);
            }

            player.LastSeen = Time.unscaledTime;
        }
    }
}
