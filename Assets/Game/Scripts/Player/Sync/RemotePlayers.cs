using System;
using System.Collections.Generic;
using Game.Scripts.Player.Animation;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    /// <summary>Чужие игроки сцены: аватары по id и по коллайдерам — по ним удары и брошенные предметы понимают, в кого попали.</summary>
    public sealed class RemotePlayers
    {
        private readonly Dictionary<string, RemotePlayer> _byId = new(StringComparer.Ordinal);
        private readonly Dictionary<Collider, RemoteAvatar> _avatarsByCollider = new();

        /// <summary>Анимация чужого игрока по его id.</summary>
        public bool TryGetWalk(string id, out ProceduralWalk walk)
        {
            if (id != null && _byId.TryGetValue(id, out var player) && player.HasWalk)
            {
                walk = player.Walk;
                return true;
            }

            walk = null;
            return false;
        }

        public bool TryFindAvatar(Collider collider, out RemoteAvatar avatar) => _avatarsByCollider.TryGetValue(collider, out avatar);

        internal bool TryGet(string id, out RemotePlayer player) => _byId.TryGetValue(id, out player);

        internal void Add(string id, RemotePlayer player) => _byId.Add(id, player);

        internal bool Remove(string id, out RemotePlayer player) => _byId.Remove(id, out player);

        internal void Clear() => _byId.Clear();

        internal void SmoothAll(float t)
        {
            foreach (var player in _byId.Values) player.Smooth(t);
        }

        internal void RegisterAvatar(RemoteAvatar avatar, IEnumerable<Collider> colliders)
        {
            foreach (var collider in colliders) _avatarsByCollider[collider] = avatar;
        }

        internal void UnregisterAvatar(IEnumerable<Collider> colliders)
        {
            foreach (var collider in colliders) _avatarsByCollider.Remove(collider);
        }
    }
}
