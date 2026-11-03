using System;
using System.Collections.Generic;
using Game.Scripts.Player.Animation;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    public static class RemotePlayers
    {
        private static readonly Dictionary<string, RemotePlayer> ById = new(StringComparer.Ordinal);

        /// <summary>Анимация чужого игрока по его id.</summary>
        public static bool TryGetWalk(string id, out ProceduralWalk walk)
        {
            if (id != null && ById.TryGetValue(id, out var player) && player.HasWalk)
            {
                walk = player.Walk;
                return true;
            }

            walk = null;
            return false;
        }

        internal static bool TryGet(string id, out RemotePlayer player) => ById.TryGetValue(id, out player);

        internal static void Add(string id, RemotePlayer player) => ById.Add(id, player);

        internal static bool Remove(string id, out RemotePlayer player) => ById.Remove(id, out player);

        internal static void Clear() => ById.Clear();

        internal static void SmoothAll(float t)
        {
            foreach (var player in ById.Values) player.Smooth(t);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Clear();
    }
}
