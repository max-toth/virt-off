using Game.Scripts.Combat.Props.Models;

namespace Game.Scripts.Combat.Props
{
    /// <summary>Переходы владения предметом по событиям игрока и сервера. Без Unity: побочные эффекты применяет NetProp.</summary>
    internal sealed class PropOwnership
    {
        public const float GrantTimeout = 0.5f;

        private float _pickupRequestTime;

        public PropOwnershipState State { get; private set; } = PropOwnershipState.Free;

        /// <summary>Предмет у кого-то в руках — у нас или у другого игрока.</summary>
        public bool IsHeld => State is PropOwnershipState.PickupPending or PropOwnershipState.Held or PropOwnershipState.HeldByOther;

        public bool IsHeldByUs => State is PropOwnershipState.PickupPending or PropOwnershipState.Held;

        /// <summary>Сервер отдал нам предмет: мы рассылаем его состояние.</summary>
        public bool HasAuthority => State is PropOwnershipState.Held or PropOwnershipState.Simulating;

        public void RequestPickup(float now)
        {
            State = PropOwnershipState.PickupPending;
            _pickupRequestTime = now;
        }

        /// <summary>Бросили, предмет успокоился или упал: владение возвращается серверу.</summary>
        public void Release() => State = PropOwnershipState.Free;

        public void ApplyGrant() => State = IsHeldByUs ? PropOwnershipState.Held : PropOwnershipState.Simulating;

        /// <returns>true — наш запрос на подбор отклонён, предмет нужно вернуть на место.</returns>
        public bool ApplyGrantToOther()
        {
            var rejected = State == PropOwnershipState.PickupPending;
            State = PropOwnershipState.HeldByOther;
            return rejected;
        }

        /// <returns>false — событие о прежнем владельце, а предмет уже у нас: не применять.</returns>
        public bool ApplyRelease(bool releasedByUs)
        {
            if (!releasedByUs && IsHeldByUs) return false;
            State = PropOwnershipState.Free;
            return true;
        }

        public void ApplySnapshot(bool held) => State = held ? PropOwnershipState.HeldByOther : PropOwnershipState.Free;

        /// <returns>false — предмет у нас в руках, чужое состояние не применяется.</returns>
        public bool ApplyRemoteState(bool held)
        {
            if (IsHeldByUs) return false;
            State = held ? PropOwnershipState.HeldByOther : PropOwnershipState.Free;
            return true;
        }

        /// <returns>true — PROP_GRANT не пришёл вовремя, подбор отменён.</returns>
        public bool CheckGrantTimeout(float now)
        {
            if (State != PropOwnershipState.PickupPending || now - _pickupRequestTime <= GrantTimeout) return false;
            State = PropOwnershipState.Free;
            return true;
        }
    }
}
