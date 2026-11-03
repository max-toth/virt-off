using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    /// <summary>Метка на модели чужого игрока: по ней удары и брошенные предметы понимают, в кого попали.</summary>
    public class RemoteAvatar : MonoBehaviour
    {
        private const float Height = 1.8f;
        private const float Radius = 0.35f;
        private const float CenterHeight = 0.85f;
        private const float MinScale = 0.01f;

        private static readonly Dictionary<Collider, RemoteAvatar> ByCollider = new();

        private Collider[] _colliders;

        public string Id { get; private set; }

        public static bool TryFind(Collider collider, out RemoteAvatar avatar) => ByCollider.TryGetValue(collider, out avatar);

        /// <summary>Добавляет капсулу и кинематический Rigidbody: в игрока можно попасть, предметы от него отскакивают.</summary>
        public static void Attach(GameObject target, string id)
        {
            var avatar = target.AddComponent<RemoteAvatar>();
            avatar.Id = id;

            var scale = Mathf.Max(MinScale, target.transform.lossyScale.y);
            var capsule = target.AddComponent<CapsuleCollider>();
            capsule.height = Height / scale;
            capsule.radius = Radius / scale;
            capsule.center = new Vector3(0f, CenterHeight / scale, 0f);

            if (!target.TryGetComponent(out Rigidbody body)) body = target.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;

            avatar.RegisterColliders();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => ByCollider.Clear();

        private void OnDestroy()
        {
            if (_colliders == null) return;
            foreach (var own in _colliders) ByCollider.Remove(own);
        }

        private void RegisterColliders()
        {
            _colliders = GetComponentsInChildren<Collider>(true);
            foreach (var own in _colliders) ByCollider[own] = this;
        }
    }
}
