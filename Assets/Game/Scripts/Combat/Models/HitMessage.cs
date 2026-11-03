using UnityEngine;

namespace Game.Scripts.Combat.Models
{
    public readonly struct HitMessage
    {
        public HitMessage(string attacker, AttackKind kind, string target, Vector3 point, Vector3 impulse, bool hitObject)
        {
            Attacker = attacker;
            Kind = kind;
            Target = target;
            Point = point;
            Impulse = impulse;
            HitObject = hitObject;
        }

        public string Attacker { get; }
        public AttackKind Kind { get; }
        public string Target { get; }
        public Vector3 Point { get; }
        public Vector3 Impulse { get; }
        public bool HitObject { get; }
        public bool HasTarget => !string.IsNullOrEmpty(Target);
    }
}
