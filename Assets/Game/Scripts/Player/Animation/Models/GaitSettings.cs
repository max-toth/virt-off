using System;
using UnityEngine;

namespace Game.Scripts.Player.Animation.Models
{
    [Serializable]
    public sealed class GaitSettings
    {
        [Tooltip("Сколько метров проходит персонаж за полный цикл (два шага) при ходьбе")]
        [SerializeField] private float walkCycleLength = 1.6f;
        [Tooltip("То же при беге")]
        [SerializeField] private float runCycleLength = 2.6f;
        [Tooltip("Скорость, при которой ходьба проигрывается с полной амплитудой")]
        [SerializeField] private float walkSpeed = 3f;
        [Tooltip("Скорость, начиная с которой анимация полностью «бег»")]
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float crouchKneeAngle = 65f;
        [SerializeField] private float crouchBlendSpeed = 6f;

        public float WalkCycleLength => walkCycleLength;
        public float RunCycleLength => runCycleLength;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float CrouchKneeAngle => crouchKneeAngle;
        public float CrouchBlendSpeed => crouchBlendSpeed;
    }
}
