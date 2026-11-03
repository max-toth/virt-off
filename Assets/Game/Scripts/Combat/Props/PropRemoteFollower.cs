using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    internal sealed class PropRemoteFollower
    {
        private const float Timeout = 1f;
        private const float Sharpness = 15f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _lastTargetTime = -10f;

        public bool IsActive { get; private set; }

        public void SetTarget(Vector3 position, Quaternion rotation, bool follow)
        {
            _targetPosition = position;
            _targetRotation = rotation;
            _lastTargetTime = Time.time;
            IsActive = follow;
        }

        public void Stop() => IsActive = false;

        public void Update(Transform target)
        {
            var t = 1f - Mathf.Exp(-Sharpness * Time.deltaTime);
            target.GetPositionAndRotation(out var position, out var rotation);
            target.SetPositionAndRotation(
                Vector3.Lerp(position, _targetPosition, t),
                Quaternion.Slerp(rotation, _targetRotation, t));

            if (Time.time - _lastTargetTime > Timeout) IsActive = false;
        }
    }
}
