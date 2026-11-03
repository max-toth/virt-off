using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly PacketSubscription Subscription = new(OnPacket);
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

        internal static void RemoveExpired()
        {
            var now = Time.unscaledTime;
            if (now < _nextExpiryCheck) return;
            _nextExpiryCheck = now + ExpiryCheckInterval;

            var expired = PlayersById.Values
                .Where(player => now - player.LastSeen > TimeoutSeconds)
                .Select(player => player.Id)
                .ToList();

            foreach (var id in expired)
            {
                PlayersById.Remove(id);
                NetDiagnostics.Log($"[Lobby] {id} left");
                PlayerLeft?.Invoke(id);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Subscription.Unbind();
            _nextExpiryCheck = 0f;
            PlayersById.Clear();
            PlayerJoined = null;
            PlayerLeft = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Subscribe() => Subscription.Bind(GameNet.Instance);

        private static void OnPacket(string localId, byte[] data)
        {
            if (data.Length < 2 || (data[0] != PacketType.Voice && data[0] != PacketType.Position)) return;

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
