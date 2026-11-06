using Game.Scripts.Combat.Props.Models;

namespace Game.Scripts.Combat.Props
{
    /// <summary>Переходы владения предметом по событиям игрока и сервера. Без Unity: побочные эффекты применяет NetProp.</summary>
    internal sealed class PropOwnership
    {
        public const float GrantTimeout = 0.5f;

        private float _requestTime;

        public PropOwnershipState State { get; private set; } = PropOwnershipState.Free;

        /// <summary>Предмет у кого-то в руках — у нас или у другого игрока.</summary>
        public bool IsHeld => State is PropOwnershipState.PickupPending or PropOwnershipState.Held or PropOwnershipState.HeldByOther;

        public bool IsHeldByUs => State is PropOwnershipState.PickupPending or PropOwnershipState.Held;

        /// <summary>Сервер отдал нам предмет: мы рассылаем его состояние.</summary>
        public bool HasAuthority => State is PropOwnershipState.Held or PropOwnershipState.Simulating;

        public void RequestPickup(float now)
        {
            State = PropOwnershipState.PickupPending;
            _requestTime = now;
        }

        /// <summary>Свободный предмет толкнули: просим у сервера его физику.</summary>
        /// <returns>true — запрос нужно отправить; false — предмет не свободен или запрос уже ждёт ответа.</returns>
        public bool RequestSimulation(float now)
        {
            if (State != PropOwnershipState.Free) return false;

            State = PropOwnershipState.SimulationPending;
            _requestTime = now;
            return true;
        }

        /// <summary>Бросили или уронили. Подтверждённый предмет остаётся за нами, пока не успокоится; неподтверждённый
        /// станет нашим, когда придёт запоздалый PROP_GRANT.</summary>
        public void Throw() => State = State == PropOwnershipState.Held ? PropOwnershipState.Simulating : PropOwnershipState.Free;

        /// <summary>Предмет успокоился или упал: владение возвращается серверу.</summary>
        public void Release() => State = PropOwnershipState.Free;

        public void ApplyGrant() => State = IsHeldByUs ? PropOwnershipState.Held : PropOwnershipState.Simulating;

        /// <returns>true — наш запрос на подбор отклонён, предмет нужно вернуть на место.</returns>
        public bool ApplyGrantToOther()
        {
            var rejected = State == PropOwnershipState.PickupPending;
            State = PropOwnershipState.HeldByOther;
            return rejected;
        }

        /// <param name="releasedByUs">Предмет отпустили мы.</param>
        /// <param name="isFree">Сервер освободил предмет (владелец отключился). Иначе бросивший ведёт его физику дальше.</param>
        /// <returns>false — событие не применять: это эхо нашего броска или предмет уже снова у нас.</returns>
        public bool ApplyRelease(bool releasedByUs, bool isFree)
        {
            if (releasedByUs && !isFree) return false;
            if (!releasedByUs && IsHeldByUs) return false;

            State = PropOwnershipState.Free;
            return true;
        }

        public void ApplySnapshot(bool held) => State = held ? PropOwnershipState.HeldByOther : PropOwnershipState.Free;

        /// <returns>false — физику предмета ведём мы, чужое состояние не применяется.</returns>
        public bool ApplyRemoteState(bool held)
        {
            // Simulating: запоздалые пакеты прежнего владельца не должны отбирать у нас физику.
            if (IsHeldByUs || State == PropOwnershipState.Simulating) return false;

            State = held ? PropOwnershipState.HeldByOther : PropOwnershipState.Free;
            return true;
        }

        /// <summary>Отменяет запрос, на который PROP_GRANT не пришёл вовремя.</summary>
        /// <returns>true — отменён подбор, предмет нужно вернуть туда, откуда его взяли.</returns>
        public bool CheckGrantTimeout(float now)
        {
            if (State is not (PropOwnershipState.PickupPending or PropOwnershipState.SimulationPending)) return false;
            if (now - _requestTime <= GrantTimeout) return false;

            var pickupRejected = State == PropOwnershipState.PickupPending;
            State = PropOwnershipState.Free;
            return pickupRejected;
        }
    }
}
