using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    // Пока предмет в руках, он не сталкивается с держащим игроком; после броска коллизия возвращается с задержкой.
    internal sealed class PropHolderCollision
    {
        private const float RestoreDelay = 0.4f;

        private readonly Collider[] _colliders;
        private Collider _pendingHolder;
        private float _restoreTime;
        private bool _restorePending;

        public PropHolderCollision(Collider[] colliders) => _colliders = colliders;

        public void Ignore(Collider holder) => SetIgnored(holder, true);

        public void ScheduleRestore(Collider holder)
        {
            _pendingHolder = holder;
            _restoreTime = Time.time + RestoreDelay;
            _restorePending = true;
        }

        public void Update(bool isHeld)
        {
            if (!_restorePending || Time.time < _restoreTime) return;

            _restorePending = false;
            if (!isHeld) SetIgnored(_pendingHolder, false);
        }

        private void SetIgnored(Collider holder, bool ignored)
        {
            foreach (var own in _colliders)
            {
                if (own.enabled) Physics.IgnoreCollision(own, holder, ignored);
            }
        }
    }
}
