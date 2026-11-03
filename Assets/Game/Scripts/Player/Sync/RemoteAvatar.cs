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

        private Collider[] _colliders;
        private RemotePlayers _registry;

        public string Id { get; private set; }

        /// <summary>Добавляет капсулу и кинематический Rigidbody: в игрока можно попасть, предметы от него отскакивают.</summary>
        public static void Attach(GameObject target, string id, RemotePlayers registry)
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

            avatar._registry = registry;
            avatar._colliders = target.GetComponentsInChildren<Collider>(true);
            registry.RegisterAvatar(avatar, avatar._colliders);
        }

        private void OnDestroy()
        {
            if (_colliders != null) _registry.UnregisterAvatar(_colliders);
        }
    }
}
