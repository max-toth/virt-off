using System;
using System.Collections.Generic;
using Game.Scripts.Combat.Models;
using Game.Scripts.Combat.Props;
using Game.Scripts.Player.Sync;
using UnityEngine;

namespace Game.Scripts.Combat
{
    public sealed class MeleeAttacker
    {
        private const int MaxHits = 32;
        private const float AttackRadius = 0.25f;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[MaxHits];
        private static readonly IComparer<RaycastHit> ByDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        private readonly Transform _owner;
        private readonly Transform _view;
        private readonly MeleeSettings _settings;
        private float _nextAttackTime;

        public MeleeAttacker(Transform owner, Transform view, MeleeSettings settings)
        {
            _owner = owner;
            _view = view;
            _settings = settings;
        }

        public bool IsReady => Time.time >= _nextAttackTime;

        public void Attack(AttackKind kind)
        {
            var settings = _settings.For(kind);
            _nextAttackTime = Time.time + settings.Cooldown;

            var origin = _view.position;
            var direction = _view.forward;
            var point = origin + direction * _settings.Reach;
            var impulse = Vector3.zero;
            string target = null;
            var hitSomething = false;

            var count = Physics.SphereCastNonAlloc(origin, AttackRadius, direction, HitBuffer, _settings.Reach,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(HitBuffer, 0, count, ByDistance);

            for (var i = 0; i < count; i++)
            {
                var hit = HitBuffer[i];
                var hitCollider = hit.collider;
                if (hitCollider.transform.IsChildOf(_owner)) continue;

                hitSomething = true;
                point = hit.distance > 0f ? hit.point : hitCollider.ClosestPoint(origin);

                if (RemoteAvatar.TryFind(hitCollider, out var avatar))
                {
                    target = avatar.Id;
                    var forward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                    impulse = settings.GetKnockback(forward, _owner.right);
                }
                else if (NetProp.TryFind(hitCollider, out var prop) && !prop.IsHeld)
                {
                    PushProp(prop, settings, direction, point);
                }

                break;
            }

            var hitPlayer = !string.IsNullOrEmpty(target);
            CombatSounds.PlayHit(kind, hitPlayer, hitSomething, point);
            CombatNetwork.SendHit(kind, target, hitSomething ? point : origin, impulse, hitSomething && !hitPlayer);
        }

        private void PushProp(NetProp prop, AttackSettings settings, Vector3 direction, Vector3 point)
        {
            prop.TakeOwnership(NetPropSender.LocalId);
            var strength = _settings.PropHitImpulse * settings.PropImpulseFactor * Mathf.Max(1f, prop.Body.mass * 0.5f);
            prop.Body.AddForceAtPosition(direction * strength, point, ForceMode.Impulse);
        }
    }
}
