using Game.Scripts.Player.Animation.Models;
using UnityEngine;

namespace Game.Scripts.Player.Animation
{
    public sealed class AttackLayer : IPoseLayer
    {
        private const float Duration = 0.35f;
        private const float StrikePart = 0.3f;

        private float _startTime = -10f;
        private bool _isSlap;

        public void Play(bool slap)
        {
            _isSlap = slap;
            _startTime = Time.time;
        }

        public void Apply(WalkRig rig, in GaitState gait)
        {
            var progress = (Time.time - _startTime) / Duration;
            if (progress is < 0f or >= 1f) return;

            // Быстрый выброс и плавный возврат.
            var extension = progress < StrikePart
                ? Mathf.SmoothStep(0f, 1f, progress / StrikePart)
                : 1f - Mathf.SmoothStep(0f, 1f, (progress - StrikePart) / (1f - StrikePart));

            var right = rig.Root.right;
            var up = rig.Root.up;
            if (_isSlap)
            {
                var sweep = progress < StrikePart ? Mathf.Lerp(70f, -35f, Mathf.SmoothStep(0f, 1f, progress / StrikePart)) : -35f;
                WalkRig.Rotate(rig.ArmRight, right, -80f * extension);
                WalkRig.Rotate(rig.ArmRight, up, sweep * extension);
                WalkRig.Rotate(rig.Spine, up, -15f * extension);
            }
            else
            {
                WalkRig.Rotate(rig.ArmRight, right, -85f * extension);
                WalkRig.Rotate(rig.ForearmRight, right, gait.ElbowAngle * extension);
                WalkRig.Rotate(rig.Spine, up, -20f * extension);
            }
        }
    }
}
