using Game.Scripts.Combat.Props.Models;
using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    // Состояние владения физическим предметом: кто его держит/двигает и как синхронизировать с сервером.
    [RequireComponent(typeof(Rigidbody))]
    public class NetProp : MonoBehaviour
    {
        private const float SendInterval = 0.05f;
        private const float GrantTimeout = 0.5f;
        private const float FallLimit = 50f;

        private readonly PropSettleDetector _settle = new();
        private readonly PropRemoteFollower _follower = new();
        private PropHolderCollision _holderCollision;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private bool _pendingGrant;
        private float _grantRequestTime;
        private float _sendTimer;

        public int Id { get; internal set; }

        // Id, выданный сервером в MANIFEST_ACK; -1 до его получения.
        public int ServerId { get; internal set; } = -1;

        public int NetworkId => ServerId >= 0 ? ServerId : Id;
        public Rigidbody Body { get; private set; }
        public bool IsHeld { get; private set; }
        public string LastThrower { get; internal set; }
        internal bool IsServerOwned { get; private set; }

        public static bool TryFind(Collider collider, out NetProp prop) => NetPropRegistry.TryFind(collider, out prop);

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            _holderCollision = new PropHolderCollision(GetComponentsInChildren<Collider>());
            transform.GetPositionAndRotation(out _startPosition, out _startRotation);
        }

        private void Update()
        {
            _holderCollision.Update(IsHeld);
            CheckGrantTimeout();

            if (HasFallen())
            {
                RecoverFromFall();
                return;
            }

            if (IsServerOwned) UpdateOwned();
            else if (_follower.IsActive) _follower.Update(transform);
        }

        // Локальное владение физикой (удар, толчок) без запроса к серверу.
        public void TakeOwnership(string thrower)
        {
            LastThrower = thrower;
            _settle.Restart();
            _follower.Stop();
            if (IsServerOwned) return;

            Body.isKinematic = false;
            Body.WakeUp();
        }

        // Оптимистично берём предмет в руки, не дожидаясь PROP_GRANT.
        public void RequestPickup(Collider holder)
        {
            if (!NetPropSender.IsConnected) return;

            IsHeld = true;
            IsServerOwned = false;
            _pendingGrant = true;
            _grantRequestTime = Time.time;
            Body.isKinematic = true;
            _holderCollision.Ignore(holder);

            NetPropSender.SendPickupRequest(this);
            NetPropDiagnostics.Log($"PICKUP_REQ prop={Id} serverId={NetworkId}");
        }

        public void RequestRelease(Vector3 velocity, Vector3 angularVelocity, Collider holder)
        {
            if (!NetPropSender.IsConnected) return;

            IsHeld = false;
            _pendingGrant = false;
            IsServerOwned = false;
            Body.isKinematic = false;
            Body.linearVelocity = velocity;
            Body.angularVelocity = angularVelocity;
            _holderCollision.ScheduleRestore(holder);

            NetPropSender.SendRelease(this, velocity, angularVelocity);
            NetPropDiagnostics.Log($"PROP_RELEASE prop={Id} serverId={NetworkId}");
        }

        public void MoveHeld(Vector3 position, Quaternion rotation)
        {
            Body.MovePosition(position);
            Body.MoveRotation(rotation);
        }

        internal void ApplyGrant()
        {
            _pendingGrant = false;
            IsServerOwned = true;
            _settle.Restart();
            _follower.Stop();
            Body.isKinematic = IsHeld;
            if (!IsHeld) Body.WakeUp();
        }

        internal void ApplyGrantToOther(string holder)
        {
            if (_pendingGrant)
            {
                _pendingGrant = false;
                IsHeld = false;
                ResetToStart();
            }

            IsServerOwned = false;
            LastThrower = holder;
        }

        internal void ApplyRelease(bool releasedByUs, bool isFree)
        {
            if (releasedByUs)
            {
                IsServerOwned = false;
                _pendingGrant = false;
            }

            IsHeld = false;

            if (isFree)
            {
                IsServerOwned = false;
                ResetToStart();
                return;
            }

            Body.isKinematic = true;
            transform.GetPositionAndRotation(out var position, out var rotation);
            _follower.SetTarget(position, rotation, true);
        }

        internal void ApplySnapshot(bool held)
        {
            IsServerOwned = false;
            _pendingGrant = false;
            _follower.Stop();
            IsHeld = held;
            ResetToStart();
        }

        // false — предмет у нас в руках, чужое состояние не применяется.
        internal bool ApplyRemoteState(string sender, Vector3 position, Quaternion rotation, PropStateFlags flags)
        {
            if (IsHeld) return false;

            var settled = (flags & PropStateFlags.Settled) != 0;
            IsServerOwned = false;
            Body.isKinematic = true;
            _follower.SetTarget(position, rotation, !settled);
            LastThrower = sender;
            if (settled) transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        private void CheckGrantTimeout()
        {
            if (!_pendingGrant || Time.time - _grantRequestTime <= GrantTimeout) return;

            _pendingGrant = false;
            if (IsServerOwned) return;

            IsHeld = false;
            ResetToStart();
            NetPropDiagnostics.Log($"Grant timeout, rollback prop={Id}");
        }

        private bool HasFallen() =>
            transform.position.y < _startPosition.y - FallLimit && (IsServerOwned || !_follower.IsActive);

        private void RecoverFromFall()
        {
            ResetToStart();
            if (!IsServerOwned) return;

            NetPropSender.SendState(this, PropStateFlags.Settled);
            IsServerOwned = false;
            IsHeld = false;
        }

        private void UpdateOwned()
        {
            if (!IsHeld && _settle.IsSettled(Body))
            {
                NetPropSender.SendState(this, PropStateFlags.Settled);
                IsServerOwned = false;
                Body.isKinematic = true;
                return;
            }

            _sendTimer += Time.deltaTime;
            if (_sendTimer < SendInterval) return;

            _sendTimer = 0f;
            NetPropSender.SendState(this, IsHeld ? PropStateFlags.Held : PropStateFlags.None);
        }

        private void ResetToStart()
        {
            Body.isKinematic = true;
            transform.SetPositionAndRotation(_startPosition, _startRotation);
        }
    }
}
