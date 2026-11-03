using System;
using UnityEngine;

namespace Game.Scripts.Combat.Models
{
    [Serializable]
    public sealed class MeleeSettings
    {
        [SerializeField] private float reach = 1.7f;
        [Tooltip("Сила удара по предметам")]
        [SerializeField] private float propHitImpulse = 4f;
        [SerializeField] private AttackSettings punch = AttackSettings.CreatePunch();
        [SerializeField] private AttackSettings slap = AttackSettings.CreateSlap();

        public float Reach => reach;
        public float PropHitImpulse => propHitImpulse;

        public AttackSettings For(AttackKind kind) => kind == AttackKind.Slap ? slap : punch;
    }
}
