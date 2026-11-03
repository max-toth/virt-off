using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Scripts.Combat.Props
{
    // Порядок детерминирован (по позиции), чтобы id совпадали у всех клиентов.
    public static class NetPropRegistry
    {
        private static readonly string[] Keywords = { "chair", "trashbin", "trash", "eraser", "notepad", "pencilholder" };

        private static readonly List<NetProp> Props = new();
        private static readonly Dictionary<int, NetProp> ByServerId = new();
        private static readonly Dictionary<Collider, NetProp> ByCollider = new();

        public static IReadOnlyList<NetProp> All => Props;

        public static bool TryFind(Collider collider, out NetProp prop) => ByCollider.TryGetValue(collider, out prop);

        public static bool TryGetByServerId(int serverId, out NetProp prop) => ByServerId.TryGetValue(serverId, out prop);

        public static void AssignServerId(int index, int serverId)
        {
            if (index < 0 || index >= Props.Count) return;

            var prop = Props[index];
            if (prop.ServerId >= 0) ByServerId.Remove(prop.ServerId);
            prop.ServerId = serverId;
            ByServerId[serverId] = prop;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            RegisterSceneProps();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RegisterSceneProps();

        private static void Clear()
        {
            Props.Clear();
            ByServerId.Clear();
            ByCollider.Clear();
        }

        private static void RegisterSceneProps()
        {
            Clear();

            var candidates = FindCandidates();
            candidates.Sort(CompareByPosition);

            for (var i = 0; i < candidates.Count; i++)
            {
                var target = candidates[i].gameObject;
                var prop = NetPropFactory.GetOrCreate(target);
                prop.Id = i;
                Props.Add(prop);

                if (prop.ServerId >= 0) ByServerId[prop.ServerId] = prop;
                foreach (var collider in target.GetComponentsInChildren<Collider>(true))
                    ByCollider[collider] = prop;
            }

            if (Props.Count > 0) NetPropDiagnostics.Log($"Physical props: {Props.Count}");
        }

        private static List<Transform> FindCandidates() =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(IsCandidate).ToList();

        private static bool IsCandidate(Transform candidate) =>
            MatchesKeyword(candidate)
            && candidate.GetComponentInParent<PlayerController>() == null
            && (candidate.parent == null || !MatchesKeyword(candidate.parent))
            && candidate.GetComponentInChildren<MeshFilter>() != null;

        private static bool MatchesKeyword(Transform candidate)
        {
            var name = candidate.name.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
            return Keywords.Any(keyword => name.Contains(keyword));
        }

        private static int CompareByPosition(Transform a, Transform b)
        {
            var positionA = Vector3Int.RoundToInt(a.position * 100f);
            var positionB = Vector3Int.RoundToInt(b.position * 100f);

            var result = positionA.x.CompareTo(positionB.x);
            if (result == 0) result = positionA.z.CompareTo(positionB.z);
            if (result == 0) result = positionA.y.CompareTo(positionB.y);
            if (result == 0) result = string.CompareOrdinal(a.name, b.name);
            return result;
        }
    }
}
