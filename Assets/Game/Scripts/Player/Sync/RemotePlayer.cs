using Game.Scripts.Player.Animation;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    public sealed class RemotePlayer
    {
        private const float TeleportDistanceSqr = 25f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        public RemotePlayer(Transform root, ProceduralWalk walk)
        {
            Root = root;
            Walk = walk;
            HasWalk = walk != null;
            root.GetPositionAndRotation(out _targetPosition, out _targetRotation);
        }

        public Transform Root { get; }
        public ProceduralWalk Walk { get; }
        public bool HasWalk { get; }

        public void SetTarget(Vector3 position, float yaw, bool crouching)
        {
            _targetPosition = position;
            _targetRotation = Quaternion.Euler(0f, yaw, 0f);
            if (HasWalk) Walk.SetCrouching(crouching);
        }

        // Пакеты приходят 20 раз в секунду: без сглаживания чужие игроки дёргаются. Далеко (респаун) — телепорт.
        public void Smooth(float t)
        {
            Root.GetPositionAndRotation(out var position, out var rotation);
            var nextPosition = (position - _targetPosition).sqrMagnitude > TeleportDistanceSqr
                ? _targetPosition
                : Vector3.Lerp(position, _targetPosition, t);
            Root.SetPositionAndRotation(nextPosition, Quaternion.Slerp(rotation, _targetRotation, t));
        }
    }
}
