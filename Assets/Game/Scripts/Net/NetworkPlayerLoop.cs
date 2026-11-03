using System;
using System.Linq;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Game.Scripts.Net
{
    // Шаг в PreUpdate: входящие пакеты разбираются до Update всех скриптов кадра.
    internal static class NetworkPlayerLoop
    {
        private struct NetworkPump
        {
        }

        public static void Install(PlayerLoopSystem.UpdateFunction update)
        {
            var root = PlayerLoop.GetCurrentPlayerLoop();
            RemovePump(ref root);

            var phases = root.subSystemList;
            for (var i = 0; i < phases.Length; i++)
            {
                if (phases[i].type != typeof(PreUpdate)) continue;

                var steps = phases[i].subSystemList ?? Array.Empty<PlayerLoopSystem>();
                var pump = new PlayerLoopSystem { type = typeof(NetworkPump), updateDelegate = update };
                phases[i].subSystemList = steps.Append(pump).ToArray();
                break;
            }

            PlayerLoop.SetPlayerLoop(root);
        }

        public static void Uninstall()
        {
            var root = PlayerLoop.GetCurrentPlayerLoop();
            if (RemovePump(ref root)) PlayerLoop.SetPlayerLoop(root);
        }

        private static bool RemovePump(ref PlayerLoopSystem root)
        {
            var phases = root.subSystemList;
            if (phases == null) return false;

            var removed = false;
            for (var i = 0; i < phases.Length; i++)
            {
                var steps = phases[i].subSystemList;
                if (steps == null || steps.All(step => step.type != typeof(NetworkPump))) continue;

                phases[i].subSystemList = steps.Where(step => step.type != typeof(NetworkPump)).ToArray();
                removed = true;
            }

            return removed;
        }
    }
}
