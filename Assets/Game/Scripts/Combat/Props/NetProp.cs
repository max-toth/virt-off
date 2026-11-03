using Game.Scripts.Combat.Props.Models;
using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    /// <summary>Физический предмет, синхронизируемый по сети: владелец (кто держит или двигает) рассылает состояние, остальные следуют за ним.
    /// Переходы владения — в PropOwnership, здесь — их физика и пакеты.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NetProp : MonoBehaviour
    {
        private const float SendInterval = 0.05f;
        private const float FallLimit = 50f;

        private readonly PropOwnership _ownership = new();
        private readonly PropSettleDetector _settle = new();
        private readonly PropRemoteFollower _follower = new();
        private PropHolderCollision _holderCollision;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private float _sendTimer;

        public int Id { get; internal set; }

        /// <summary>Id, выданный сервером в MANIFEST_ACK; -1 до его получения.</summary>
        public int ServerId { get; internal set; } = -1;

        /// <summary>Id для пакетов: ServerId, если сервер его уже выдал, иначе локальный Id.</summary>
        public int NetworkId => ServerId >= 0 ? ServerId : Id;
        public Rigidbody Body { get; private set; }

        /// <summary>Предмет у кого-то в руках — у нас или у другого игрока.</summary>
        public bool IsHeld => _ownership.IsHeld;

        public bool IsHeldByUs => _ownership.IsHeldByUs;

        /// <summary>Id игрока, который последним бросил или толкнул предмет: кому засчитать попадание.</summary>
        public string LastThrower { get; internal set; }

        /// <summary>Сервер отдал нам предмет: мы рассылаем его состояние.</summary>
        internal bool HasAuthority => _ownership.HasAuthority;

        public static bool TryFind(Collider collider, out NetProp prop) => NetPropRegistry.TryFind(collider, out prop);

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            _holderCollision = new PropHolderCollision(GetComponentsInChildren<Collider>());
            transform.GetPositionAndRotation(out _startPosition, out _startRotation);
        }

        private void Update()
        {
            _holderCollision.Update(IsHeldByUs);

            if (_ownership.CheckGrantTimeout(Time.time))
            {
                ResetToStart();
                NetPropDiagnostics.Log($"Grant timeout, rollback prop={Id}");
            }

            if (HasFallen())
            {
                RecoverFromFall();
                return;
            }

            if (HasAuthority) UpdateAuthority();
            else if (_follower.IsActive) _follower.Update(transform);
        }

        /// <summary>Локальное владение физикой (удар, толчок) без запроса к серверу.</summary>
        public void TakeOwnership(string thrower)
        {
            LastThrower = thrower;
            _settle.Restart();
            _follower.Stop();
            if (HasAuthority) return;

            Body.isKinematic = false;
            Body.WakeUp();
        }

        /// <summary>Берёт предмет в руки сразу, не дожидаясь PROP_GRANT; при отказе или тайм-ауте предмет вернётся на место.</summary>
        public void RequestPickup(Collider holder)
        {
            if (!NetPropSender.IsConnected) return;

            _ownership.RequestPickup(Time.time);
            Body.isKinematic = true;
            _holderCollision.Ignore(holder);

            NetPropSender.SendPickupRequest(NetworkId);
            NetPropDiagnostics.Log($"PICKUP_REQ prop={Id} serverId={NetworkId}");
        }

        public void RequestRelease(Vector3 velocity, Vector3 angularVelocity, Collider holder)
        {
            if (!NetPropSender.IsConnected) return;

            _ownership.Release();
            Body.isKinematic = false;
            Body.linearVelocity = velocity;
            Body.angularVelocity = angularVelocity;
            _holderCollision.ScheduleRestore(holder);

            NetPropSender.SendRelease(NetworkId, velocity, angularVelocity);
            NetPropDiagnostics.Log($"PROP_RELEASE prop={Id} serverId={NetworkId}");
        }

        public void MoveHeld(Vector3 position, Quaternion rotation)
        {
            Body.MovePosition(position);
            Body.MoveRotation(rotation);
        }

        internal void ApplyGrant()
        {
            _ownership.ApplyGrant();
            _settle.Restart();
            _follower.Stop();
            Body.isKinematic = IsHeldByUs;
            if (!IsHeldByUs) Body.WakeUp();
        }

        internal void ApplyGrantToOther(string holder)
        {
            if (_ownership.ApplyGrantToOther()) ResetToStart();
            LastThrower = holder;
        }

        internal void ApplyRelease(bool releasedByUs, bool isFree)
        {
            if (!_ownership.ApplyRelease(releasedByUs)) return;

            if (isFree)
            {
                ResetToStart();
                return;
            }

            Body.isKinematic = true;
            transform.GetPositionAndRotation(out var position, out var rotation);
            _follower.SetTarget(position, rotation, true);
        }

        internal void ApplySnapshot(bool held)
        {
            _ownership.ApplySnapshot(held);
            _follower.Stop();
            ResetToStart();
        }

        /// <summary>Применяет состояние предмета от другого клиента.</summary>
        /// <returns>false — предмет у нас в руках, чужое состояние не применяется.</returns>
        internal bool ApplyRemoteState(string sender, Vector3 position, Quaternion rotation, PropStateFlags flags)
        {
            if (!_ownership.ApplyRemoteState((flags & PropStateFlags.Held) != 0)) return false;

            var settled = (flags & PropStateFlags.Settled) != 0;
            Body.isKinematic = true;
            _follower.SetTarget(position, rotation, !settled);
            LastThrower = sender;
            if (settled) transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        private bool HasFallen() =>
            transform.position.y < _startPosition.y - FallLimit && (HasAuthority || !_follower.IsActive);

        private void RecoverFromFall()
        {
            var hadAuthority = HasAuthority;
            ResetToStart();
            if (!hadAuthority) return;

            SendState(PropStateFlags.Settled);
            _ownership.Release();
        }

        private void UpdateAuthority()
        {
            var held = _ownership.State == PropOwnershipState.Held;
            if (!held && _settle.IsSettled(Body))
            {
                SendState(PropStateFlags.Settled);
                _ownership.Release();
                Body.isKinematic = true;
                return;
            }

            _sendTimer += Time.deltaTime;
            if (_sendTimer < SendInterval) return;

            _sendTimer = 0f;
            SendState(held ? PropStateFlags.Held : PropStateFlags.None);
        }

        private void SendState(PropStateFlags flags)
        {
            transform.GetPositionAndRotation(out var position, out var rotation);
            var velocity = Body.isKinematic ? Vector3.zero : Body.linearVelocity;
            NetPropSender.SendState(NetworkId, position, rotation, velocity, flags);
        }

        private void ResetToStart()
        {
            Body.isKinematic = true;
            transform.SetPositionAndRotation(_startPosition, _startRotation);
        }
    }
}
