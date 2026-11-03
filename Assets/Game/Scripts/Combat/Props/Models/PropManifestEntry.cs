using UnityEngine;

namespace Game.Scripts.Combat.Props.Models
{
    public readonly struct PropManifestEntry
    {
        public PropManifestEntry(string key, Vector3 halfExtents, float mass)
        {
            Key = key;
            HalfExtents = halfExtents;
            Mass = mass;
        }

        /// <summary>Путь в иерархии сцены: по нему сервер сопоставляет предметы разных клиентов.</summary>
        public string Key { get; }
        public Vector3 HalfExtents { get; }
        public float Mass { get; }
    }
}
