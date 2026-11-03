using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    // Определяет, что брошенный/толкнутый предмет успокоился и владение можно вернуть серверу.
    internal sealed class PropSettleDetector
    {
        private const float MinOwnedTime = 0.5f;
        private const float MaxOwnedTime = 10f;
        private const float SlowTime = 0.4f;
        private const float SlowLinearSpeedSqr = 0.01f;
        private const float SlowAngularSpeedSqr = 0.04f;

        private float _ownedSince;
        private float _slowSince;

        public void Restart()
        {
            _ownedSince = Time.time;
            _slowSince = -1f;
        }

        public bool IsSettled(Rigidbody body)
        {
            var ownedFor = Time.time - _ownedSince;
            if (ownedFor < MinOwnedTime) return false;
            if (ownedFor > MaxOwnedTime || body.IsSleeping()) return true;

            var slow = body.linearVelocity.sqrMagnitude < SlowLinearSpeedSqr
                       && body.angularVelocity.sqrMagnitude < SlowAngularSpeedSqr;
            if (!slow)
            {
                _slowSince = -1f;
                return false;
            }

            if (_slowSince < 0f) _slowSince = Time.time;
            return Time.time - _slowSince > SlowTime;
        }
    }
}
