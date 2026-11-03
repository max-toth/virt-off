using System;
using System.Collections.Generic;
using Game.Scripts.Net.Models;
using Game.Scripts.Net.Packets;
using UnityEngine;

namespace Game.Scripts.Net
{
    public static class Lobby
    {
        private const float TimeoutSeconds = 5f;

        private static readonly Dictionary<string, LobbyPlayer> PlayersById = new(StringComparer.Ordinal);
        private static readonly List<string> Expired = new();
        private static readonly PacketSubscription Subscription = new(OnPacket);

        public static event Action<string> PlayerJoined;
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
            Expired.Clear();
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var player in PlayersById.Values)
            {
                if (now - player.LastSeen > TimeoutSeconds) Expired.Add(player.Id);
            }

            foreach (var id in Expired)
            {
                PlayersById.Remove(id);
                NetDiagnostics.Log($"[Lobby] {id} left");
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                PlayerLeft?.Invoke(id);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Subscription.Unbind();
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
