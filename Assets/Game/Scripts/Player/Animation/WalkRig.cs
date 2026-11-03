using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Scripts.Player.Animation
{
    /// <summary>Кости скелета Rigify metarig (модели Floreswa) и их исходная поза.</summary>
    public sealed class WalkRig
    {
        private static readonly string[] BoneNames =
        {
            "spine", "spine.001",
            "thigh.L", "thigh.R", "shin.L", "shin.R", "foot.L", "foot.R",
            "upper_arm.L", "upper_arm.R", "forearm.L", "forearm.R"
        };

        private readonly Transform[] _bones;
        private readonly Quaternion[] _restRotations;
        private readonly Vector3 _hipsRestPosition;

        private WalkRig(Transform root, Transform[] bones)
        {
            Root = root;
            _bones = bones;
            _restRotations = new Quaternion[bones.Length];
            for (var i = 0; i < bones.Length; i++) _restRotations[i] = bones[i].localRotation;
            _hipsRestPosition = Hips.localPosition;

            var thigh = root.InverseTransformPoint(ThighLeft.position);
            var shin = root.InverseTransformPoint(ShinLeft.position);
            var foot = root.InverseTransformPoint(FootLeft.position);
            LegLength = ((thigh - shin).magnitude + (shin - foot).magnitude) * 0.5f;
        }

        public Transform Root { get; }

        /// <summary>Длина ноги в локальных единицах корня модели.</summary>
        public float LegLength { get; }

        public Transform Hips => _bones[0];
        public Transform Spine => _bones[1];
        public Transform ThighLeft => _bones[2];
        public Transform ThighRight => _bones[3];
        public Transform ShinLeft => _bones[4];
        public Transform ShinRight => _bones[5];
        public Transform FootLeft => _bones[6];
        public Transform FootRight => _bones[7];
        public Transform ArmLeft => _bones[8];
        public Transform ArmRight => _bones[9];
        public Transform ForearmLeft => _bones[10];
        public Transform ForearmRight => _bones[11];

        public static bool TryCreate(Transform root, out WalkRig rig)
        {
            var byName = new Dictionary<string, Transform>();
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                byName.TryAdd(child.name, child);

            if (!BoneNames.All(byName.ContainsKey))
            {
                rig = null;
                return false;
            }

            rig = new WalkRig(root, BoneNames.Select(boneName => byName[boneName]).ToArray());
            return true;
        }

        public static void Rotate(Transform bone, Vector3 axis, float angle) =>
            bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;

        public void ResetPose()
        {
            for (var i = 0; i < _bones.Length; i++) _bones[i].localRotation = _restRotations[i];
            Hips.localPosition = _hipsRestPosition;
        }
    }
}
