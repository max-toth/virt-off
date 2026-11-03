using System;
using UnityEngine;

namespace Game.Scripts.Player.Animation.Models
{
    [Serializable]
    public sealed class PoseAmplitudes
    {
        [SerializeField] private float walkLegSwing = 28f;
        [SerializeField] private float runLegSwing = 45f;
        [SerializeField] private float walkKneeBend = 35f;
        [SerializeField] private float runKneeBend = 80f;
        [SerializeField] private float walkArmSwing = 22f;
        [SerializeField] private float runArmSwing = 50f;
        [SerializeField] private float walkElbowBend = 12f;
        [SerializeField] private float runElbowBend = 70f;
        [SerializeField] private float runLean = 8f;
        [Tooltip("Насколько опустить руки из A-позы к телу")]
        [SerializeField] private float armsDown = 25f;

        public float WalkLegSwing => walkLegSwing;
        public float RunLegSwing => runLegSwing;
        public float WalkKneeBend => walkKneeBend;
        public float RunKneeBend => runKneeBend;
        public float WalkArmSwing => walkArmSwing;
        public float RunArmSwing => runArmSwing;
        public float WalkElbowBend => walkElbowBend;
        public float RunElbowBend => runElbowBend;
        public float RunLean => runLean;
        public float ArmsDown => armsDown;
    }
}
