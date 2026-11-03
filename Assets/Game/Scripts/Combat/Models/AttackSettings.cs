using System;
using UnityEngine;

namespace Game.Scripts.Combat.Models
{
    [Serializable]
    public sealed class AttackSettings
    {
        [SerializeField] private float cooldown;
        [Tooltip("Отброс противника вперёд, м/с")]
        [SerializeField] private float force;
        [Tooltip("Доля отброса вбок от силы удара")]
        [SerializeField] private float sideForceFactor;
        [Tooltip("Подброс вверх, м/с")]
        [SerializeField] private float liftForce;
        [Tooltip("Множитель силы удара по предметам")]
        [SerializeField] private float propImpulseFactor;
        [SerializeField] private float attackerShake;
        [SerializeField] private float victimShake;

        public AttackSettings()
        {
        }

        private AttackSettings(
            float cooldown, float force, float sideForceFactor, float liftForce,
            float propImpulseFactor, float attackerShake, float victimShake)
        {
            this.cooldown = cooldown;
            this.force = force;
            this.sideForceFactor = sideForceFactor;
            this.liftForce = liftForce;
            this.propImpulseFactor = propImpulseFactor;
            this.attackerShake = attackerShake;
            this.victimShake = victimShake;
        }

        public float Cooldown => cooldown;
        public float PropImpulseFactor => propImpulseFactor;
        public float AttackerShake => attackerShake;
        public float VictimShake => victimShake;

        public static AttackSettings CreatePunch() => new(0.45f, 6f, 0f, 2.5f, 1f, 1.5f, 6f);

        public static AttackSettings CreateSlap() => new(0.6f, 3.5f, 0.6f, 1f, 0.6f, 1f, 4f);

        public Vector3 GetKnockback(Vector3 forward, Vector3 right) =>
            forward * force + right * (force * sideForceFactor) + Vector3.up * liftForce;
    }
}
