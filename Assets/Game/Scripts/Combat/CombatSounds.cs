using Game.Scripts.Audio;
using Game.Scripts.Audio.Models;
using Game.Scripts.Combat.Models;
using UnityEngine;

namespace Game.Scripts.Combat
{
    public static class CombatSounds
    {
        public static void PlayHit(AttackKind kind, bool hitPlayer, bool hitObject, Vector3 point)
        {
            if (kind == AttackKind.Prop)
                GameSounds.Play(Sfx.Punch, point, 1f, 0.8f);
            else if (hitPlayer)
                GameSounds.Play(ToSfx(kind), point);
            else if (hitObject)
                GameSounds.Play(ToSfx(kind), point, 0.5f, 1.2f);
            else
                GameSounds.Play(Sfx.Miss, point, 0.6f);
        }

        private static Sfx ToSfx(AttackKind kind) => kind == AttackKind.Slap ? Sfx.Slap : Sfx.Punch;
    }
}
