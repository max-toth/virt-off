using Game.Scripts.Combat.Models;
using UnityEngine;

namespace Game.Scripts.Combat
{
    // Как чужой удар выглядит и звучит у нас: замах ударившего, звук, вздрагивание жертвы.
    public static class RemoteCombatPresenter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            CombatNetwork.HitReceived -= OnHitReceived;
            CombatNetwork.HitReceived += OnHitReceived;
        }

        private static void OnHitReceived(HitMessage hit)
        {
            if (hit.Kind != AttackKind.Prop && TryGetRemoteWalk(hit.Attacker, out var attackerWalk))
                attackerWalk.PlayAttack(hit.Kind == AttackKind.Slap);

            CombatSounds.PlayHit(hit.Kind, hit.HasTarget, hit.HitObject, hit.Point);

            if (hit.HasTarget && TryGetRemoteWalk(hit.Target, out var victimWalk))
                victimWalk.PlayHit();
        }

        private static bool TryGetRemoteWalk(string id, out ProceduralWalk walk) =>
            PositionSync.TryGetRemote(id, out _, out walk) && walk != null;
    }
}
