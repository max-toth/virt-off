using Game.Scripts.Player.Animation.Models;
using UnityEngine;

namespace Game.Scripts.Player.Animation
{
    public sealed class HitReactionLayer : IPoseLayer
    {
        private const float Duration = 0.4f;
        private const float RecoilAngle = -25f;

        private float _startTime = -10f;

        public void Play() => _startTime = Time.time;

        public void Apply(WalkRig rig, in GaitState gait)
        {
            var progress = (Time.time - _startTime) / Duration;
            if (progress is < 0f or >= 1f) return;

            WalkRig.Rotate(rig.Spine, rig.Root.right, RecoilAngle * Mathf.Sin(Mathf.PI * Mathf.Sqrt(progress)));
        }
    }
}
