using Game.Scripts.Player.Animation.Models;
using UnityEngine;

namespace Game.Scripts.Player.Animation
{
    public sealed class CarryLayer : IPoseLayer
    {
        private const float BlendSpeed = 6f;
        private const float ArmsForwardAngle = -75f;
        private const float ArmsInwardAngle = 15f;

        private float _holdUntil;
        private float _blend;

        public void Hold(float duration) => _holdUntil = Time.time + duration;

        public void Apply(WalkRig rig, in GaitState gait)
        {
            _blend = Mathf.MoveTowards(_blend, Time.time < _holdUntil ? 1f : 0f, BlendSpeed * gait.DeltaTime);
            if (_blend <= 0f) return;

            var right = rig.Root.right;
            var up = rig.Root.up;
            WalkRig.Rotate(rig.ArmLeft, right, ArmsForwardAngle * _blend);
            WalkRig.Rotate(rig.ArmRight, right, ArmsForwardAngle * _blend);
            WalkRig.Rotate(rig.ArmLeft, up, ArmsInwardAngle * _blend);
            WalkRig.Rotate(rig.ArmRight, up, -ArmsInwardAngle * _blend);
        }
    }
}
