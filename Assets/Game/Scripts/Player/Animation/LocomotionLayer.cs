using Game.Scripts.Player.Animation.Models;
using UnityEngine;

namespace Game.Scripts.Player.Animation
{
    // Шаг, бег и присед: таз, ноги и маховые движения рук.
    public sealed class LocomotionLayer : IPoseLayer
    {
        private const float HipsBob = 0.03f;
        private const float StepKneeBend = 5f;
        private const float CrouchSpineLean = 0.45f;
        private const float FootLevelling = 0.8f;

        private readonly PoseAmplitudes _amplitudes;

        public LocomotionLayer(PoseAmplitudes amplitudes) => _amplitudes = amplitudes;

        public void Apply(WalkRig rig, in GaitState gait)
        {
            var root = rig.Root;
            var right = root.right;
            var forward = root.forward;

            var drop = 2f * rig.LegLength * (1f - Mathf.Cos(gait.CrouchAngle * Mathf.Deg2Rad));
            var bob = Mathf.Abs(gait.PhaseCos) * HipsBob * rig.LegLength * gait.Move;
            rig.Hips.position -= root.up * ((drop + bob) * root.lossyScale.y);

            var legSwing = Mathf.Lerp(_amplitudes.WalkLegSwing, _amplitudes.RunLegSwing, gait.Run) * gait.Move;
            var knee = Mathf.Lerp(_amplitudes.WalkKneeBend, _amplitudes.RunKneeBend, gait.Run) * gait.Move;
            var armSwing = Mathf.Lerp(_amplitudes.WalkArmSwing, _amplitudes.RunArmSwing, gait.Run) * gait.Move;

            // «+» наклоняет верх кости вперёд (для ноги — отводит стопу назад).
            var thighLeft = -legSwing * gait.PhaseSin - gait.CrouchAngle;
            var thighRight = legSwing * gait.PhaseSin - gait.CrouchAngle;
            var shinLeft = knee * Mathf.Max(0f, gait.PhaseCos) + StepKneeBend * gait.Move + gait.CrouchAngle * 2f;
            var shinRight = knee * Mathf.Max(0f, -gait.PhaseCos) + StepKneeBend * gait.Move + gait.CrouchAngle * 2f;

            WalkRig.Rotate(rig.Spine, right, _amplitudes.RunLean * gait.Run + gait.CrouchAngle * CrouchSpineLean);
            WalkRig.Rotate(rig.ThighLeft, right, thighLeft);
            WalkRig.Rotate(rig.ThighRight, right, thighRight);
            WalkRig.Rotate(rig.ShinLeft, right, shinLeft);
            WalkRig.Rotate(rig.ShinRight, right, shinRight);
            WalkRig.Rotate(rig.FootLeft, right, -(thighLeft + shinLeft) * FootLevelling);
            WalkRig.Rotate(rig.FootRight, right, -(thighRight + shinRight) * FootLevelling);

            WalkRig.Rotate(rig.ArmLeft, forward, _amplitudes.ArmsDown);
            WalkRig.Rotate(rig.ArmRight, forward, -_amplitudes.ArmsDown);
            WalkRig.Rotate(rig.ArmLeft, right, armSwing * gait.PhaseSin);
            WalkRig.Rotate(rig.ArmRight, right, -armSwing * gait.PhaseSin);
            WalkRig.Rotate(rig.ForearmLeft, right, -gait.ElbowAngle);
            WalkRig.Rotate(rig.ForearmRight, right, -gait.ElbowAngle);
        }
    }
}
