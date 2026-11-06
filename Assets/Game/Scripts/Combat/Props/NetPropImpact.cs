using System;
using System.Collections.Generic;
using Game.Scripts.Audio;
using Game.Scripts.Audio.Models;
using Game.Scripts.Combat.Models;
using Game.Scripts.Player.Sync;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Combat.Props
{
    [RequireComponent(typeof(NetProp))]
    public class NetPropImpact : MonoBehaviour
    {
        private const float PushPropSpeed = 1f;
        private const float PushPropImpulseFactor = 0.5f;
        private const float SoundSpeed = 1.5f;
        private const float SoundInterval = 0.12f;
        private const float SoundMaxSpeed = 8f;
        private const float HeavyPropMass = 8f;
        private const float AvatarHitSpeed = 3f;
        private const float AvatarHitInterval = 0.5f;
        private const float AvatarHitMinUpward = 0.25f;
        private const float AvatarHitForceFactor = 0.25f;
        private const float AvatarHitMinForce = 3f;
        private const float AvatarHitMaxForce = 12f;
        private const float MovingSpeedSqr = 0.01f;

        private readonly Dictionary<string, float> _lastAvatarHitTime = new(StringComparer.Ordinal);
        private NetProp _prop;
        private float _lastSoundTime;
        private NetPropRegistry _registry;
        private RemotePlayers _remotePlayers;
        private CombatNetwork _network;
        private CombatSounds _combatSounds;
        private GameSounds _sounds;

        [Inject]
        public void Construct(NetPropRegistry registry, RemotePlayers remotePlayers, CombatNetwork network,
            CombatSounds combatSounds, GameSounds sounds)
        {
            _registry = registry;
            _remotePlayers = remotePlayers;
            _network = network;
            _combatSounds = combatSounds;
            _sounds = sounds;
        }

        private void Awake() => _prop = GetComponent<NetProp>();

        private void OnCollisionEnter(Collision collision)
        {
            if (!_prop.HasAuthority || _prop.IsHeld) return;

            var speed = collision.relativeVelocity.magnitude;
            var point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            PushOtherProp(collision, speed, point);
            PlaySound(speed, point);
            TryHitRemoteAvatar(collision, speed, point);
        }

        private void PushOtherProp(Collision collision, float speed, Vector3 point)
        {
            if (speed <= PushPropSpeed || !_registry.TryFind(collision.collider, out var other) || other.HasAuthority) return;
            if (!other.Push(_prop.LastThrower)) return;

            other.Body.AddForceAtPosition(
                -collision.relativeVelocity * (_prop.Body.mass * PushPropImpulseFactor), point, ForceMode.Impulse);
        }

        private void PlaySound(float speed, Vector3 point)
        {
            if (speed <= SoundSpeed || Time.time - _lastSoundTime <= SoundInterval) return;

            _lastSoundTime = Time.time;
            var volume = Mathf.Clamp01(speed / SoundMaxSpeed);
            var pitch = Mathf.Lerp(1.3f, 0.8f, Mathf.Clamp01(_prop.Body.mass / HeavyPropMass));
            _sounds.Play(Sfx.Impact, point, volume, pitch);
            _network.SendSound(Sfx.Impact, point, volume);
        }

        private void TryHitRemoteAvatar(Collision collision, float speed, Vector3 point)
        {
            if (speed <= AvatarHitSpeed || string.IsNullOrEmpty(_prop.LastThrower)) return;
            if (!_remotePlayers.TryFindAvatar(collision.collider, out var avatar)) return;
            if (_lastAvatarHitTime.TryGetValue(avatar.Id, out var lastHit) && Time.time - lastHit < AvatarHitInterval) return;
            _lastAvatarHitTime[avatar.Id] = Time.time;

            var body = _prop.Body;
            var direction = body.linearVelocity.sqrMagnitude > MovingSpeedSqr
                ? body.linearVelocity.normalized
                : -collision.relativeVelocity.normalized;
            direction.y = Mathf.Max(direction.y, AvatarHitMinUpward);

            var force = Mathf.Clamp(speed * body.mass * AvatarHitForceFactor, AvatarHitMinForce, AvatarHitMaxForce);
            _combatSounds.PlayHit(AttackKind.Prop, true, false, point);
            _network.SendHit(AttackKind.Prop, avatar.Id, point, direction.normalized * force);
        }
    }
}
