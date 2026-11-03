using Game.Scripts.Audio;
using Game.Scripts.Audio.Models;
using Game.Scripts.Combat.Models;
using UnityEngine;

namespace Game.Scripts.Combat
{
    public sealed class CombatSounds
    {
        private readonly GameSounds _sounds;

        public CombatSounds(GameSounds sounds) => _sounds = sounds;

        public void PlayHit(AttackKind kind, bool hitPlayer, bool hitObject, Vector3 point)
        {
            if (kind == AttackKind.Prop)
                _sounds.Play(Sfx.Punch, point, 1f, 0.8f);
            else if (hitPlayer)
                _sounds.Play(ToSfx(kind), point);
            else if (hitObject)
                _sounds.Play(ToSfx(kind), point, 0.5f, 1.2f);
            else
                _sounds.Play(Sfx.Miss, point, 0.6f);
        }

        private static Sfx ToSfx(AttackKind kind) => kind == AttackKind.Slap ? Sfx.Slap : Sfx.Punch;
    }
}
