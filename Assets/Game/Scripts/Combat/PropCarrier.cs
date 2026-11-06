using Game.Scripts.Audio;
using Game.Scripts.Audio.Models;
using Game.Scripts.Combat.Models;
using Game.Scripts.Combat.Props;
using UnityEngine;

namespace Game.Scripts.Combat
{
    public sealed class PropCarrier
    {
        private const int MaxHits = 32;
        private const float ThrowVolume = 0.8f;
        private const float ThrowPitch = 0.85f;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[MaxHits];

        private readonly Transform _owner;
        private readonly Transform _view;
        private readonly CharacterController _controller;
        private readonly CarrySettings _settings;
        private readonly NetPropRegistry _props;
        private readonly GameSounds _sounds;
        private readonly CombatNetwork _network;
        private NetProp _held;
        private NetProp _aimed;
        private Quaternion _heldRotationOffset;

        public PropCarrier(Transform owner, Transform view, CharacterController controller, CarrySettings settings,
            NetPropRegistry props, GameSounds sounds, CombatNetwork network)
        {
            _props = props;
            _sounds = sounds;
            _network = network;
            _owner = owner;
            _view = view;
            _controller = controller;
            _settings = settings;
        }

        public bool IsHolding { get; private set; }
        public bool HasAimedProp { get; private set; }

        public void ClearAim() => HasAimedProp = false;

        public void UpdateAim() => HasAimedProp = TryFindAimedProp(out _aimed);

        public bool TryPickupAimed()
        {
            if (!HasAimedProp) return false;

            _held = _aimed;
            IsHolding = true;
            _heldRotationOffset = Quaternion.Inverse(YawRotation()) * _held.transform.rotation;
            _held.RequestPickup(_controller);
            return true;
        }

        /// <summary>Двигает предмет перед камерой.</summary>
        /// <returns>false — в руках ничего нет или предмет отобрали.</returns>
        public bool UpdateHold()
        {
            if (!IsHolding) return false;
            if (!_held.IsHeldByUs)
            {
                Release();
                return false;
            }

            var position = _view.position + _view.forward * _settings.HoldDistance - Vector3.up * _settings.HoldHeightOffset;
            _held.MoveHeld(position, YawRotation() * _heldRotationOffset);
            return true;
        }

        public void Throw()
        {
            if (!IsHolding) return;

            var prop = _held;
            Drop(_view.forward * _settings.ThrowSpeed + _controller.velocity);
            prop.Body.angularVelocity = Random.insideUnitSphere * _settings.ThrowSpin;

            var position = prop.transform.position;
            _sounds.Play(Sfx.Whoosh, position, ThrowVolume, ThrowPitch);
            _network.SendSound(Sfx.Whoosh, position, ThrowVolume);
        }

        public void Drop(Vector3 velocity)
        {
            if (!IsHolding) return;

            var prop = _held;
            Release();
            prop.RequestRelease(velocity, Vector3.zero, _controller);
        }

        private void Release()
        {
            _held = null;
            IsHolding = false;
        }

        private Quaternion YawRotation() => Quaternion.Euler(0f, _owner.eulerAngles.y, 0f);

        private bool TryFindAimedProp(out NetProp prop)
        {
            var count = Physics.RaycastNonAlloc(_view.position, _view.forward, HitBuffer, _settings.PickupDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            var nearestIndex = -1;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = HitBuffer[i];
                if (hit.distance >= nearestDistance || hit.collider.transform.IsChildOf(_owner)) continue;
                nearestIndex = i;
                nearestDistance = hit.distance;
            }

            prop = null;
            return nearestIndex >= 0 && _props.TryFind(HitBuffer[nearestIndex].collider, out prop) && !prop.IsHeld
                   && !prop.IsMovedByOther;
        }
    }
}
