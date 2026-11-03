using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    /// <summary>Решает, отправлять ли позицию: при движении, повороте или смене приседа, а стоя на месте — раз в keepAliveInterval,
    /// чтобы другие клиенты не сочли игрока ушедшим. Сравнение — с последним отправленным, поэтому мелкие сдвиги не копятся незамеченными.</summary>
    public sealed class PositionSendFilter
    {
        private const float MinMoveSqr = 0.0001f;
        private const float MinYawDelta = 0.5f;

        private readonly float _keepAliveInterval;
        private bool _hasSent;
        private Vector3 _position;
        private float _yaw;
        private bool _crouching;
        private float _sentAt;

        public PositionSendFilter(float keepAliveInterval) => _keepAliveInterval = keepAliveInterval;

        public bool ShouldSend(Vector3 position, float yaw, bool crouching, float now)
        {
            var changed = !_hasSent
                          || (position - _position).sqrMagnitude > MinMoveSqr
                          || Mathf.Abs(Mathf.DeltaAngle(yaw, _yaw)) > MinYawDelta
                          || crouching != _crouching
                          || now - _sentAt >= _keepAliveInterval;
            if (!changed) return false;

            _hasSent = true;
            _position = position;
            _yaw = yaw;
            _crouching = crouching;
            _sentAt = now;
            return true;
        }
    }
}
