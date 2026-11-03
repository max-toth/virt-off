using System;
using Game.Scripts.Audio;
using Game.Scripts.Combat.Models;
using Game.Scripts.Player.Sync;
using VContainer.Unity;

namespace Game.Scripts.Combat
{
    /// <summary>Удары и звуки других игроков: анимация чужих аватаров и звук в точке.</summary>
    public sealed class RemoteCombatPresenter : IInitializable, IDisposable
    {
        private readonly CombatNetwork _network;
        private readonly RemotePlayers _remotePlayers;
        private readonly CombatSounds _combatSounds;
        private readonly GameSounds _sounds;

        public RemoteCombatPresenter(CombatNetwork network, RemotePlayers remotePlayers, CombatSounds combatSounds, GameSounds sounds)
        {
            _network = network;
            _remotePlayers = remotePlayers;
            _combatSounds = combatSounds;
            _sounds = sounds;
        }

        public void Initialize()
        {
            _network.HitReceived += OnHitReceived;
            _network.SoundReceived += OnSoundReceived;
        }

        public void Dispose()
        {
            _network.HitReceived -= OnHitReceived;
            _network.SoundReceived -= OnSoundReceived;
        }

        private void OnHitReceived(HitMessage hit)
        {
            if (hit.Kind != AttackKind.Prop && _remotePlayers.TryGetWalk(hit.Attacker, out var attackerWalk))
                attackerWalk.PlayAttack(hit.Kind == AttackKind.Slap);

            _combatSounds.PlayHit(hit.Kind, hit.HasTarget, hit.HitObject, hit.Point);

            if (hit.HasTarget && _remotePlayers.TryGetWalk(hit.Target, out var victimWalk))
                victimWalk.PlayHit();
        }

        private void OnSoundReceived(SoundMessage sound) => _sounds.Play(sound.Sfx, sound.Position, sound.Volume);
    }
}
