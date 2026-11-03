using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Game.Scripts.Combat.Props
{
    /// <summary>Предметы сцены и их локальные id. Порядок детерминирован (по позиции), чтобы id совпадали у всех клиентов.
    /// Живёт столько же, сколько сцена: предметы находятся при сборке контейнера.</summary>
    public sealed class NetPropRegistry : IInitializable
    {
        private static readonly string[] Keywords = { "chair", "trashbin", "trash", "eraser", "notepad", "pencilholder" };

        private readonly IObjectResolver _resolver;
        private readonly List<NetProp> _props = new();
        private readonly Dictionary<int, NetProp> _byServerId = new();
        private readonly Dictionary<Collider, NetProp> _byCollider = new();

        public NetPropRegistry(IObjectResolver resolver) => _resolver = resolver;

        public IReadOnlyList<NetProp> All => _props;

        public bool TryFind(Collider collider, out NetProp prop) => _byCollider.TryGetValue(collider, out prop);

        public bool TryGetByServerId(int serverId, out NetProp prop) => _byServerId.TryGetValue(serverId, out prop);

        public void AssignServerId(int index, int serverId)
        {
            if (index < 0 || index >= _props.Count) return;

            var prop = _props[index];
            if (prop.ServerId >= 0) _byServerId.Remove(prop.ServerId);
            prop.ServerId = serverId;
            _byServerId[serverId] = prop;
        }

        public void Initialize()
        {
            var candidates = FindCandidates();
            candidates.Sort(CompareByPosition);

            for (var i = 0; i < candidates.Count; i++)
            {
                var target = candidates[i].gameObject;
                var prop = NetPropFactory.GetOrCreate(target);
                _resolver.InjectGameObject(target);
                prop.Id = i;
                _props.Add(prop);

                if (prop.ServerId >= 0) _byServerId[prop.ServerId] = prop;
                foreach (var collider in target.GetComponentsInChildren<Collider>(true))
                    _byCollider[collider] = prop;
            }

            if (_props.Count > 0) NetPropDiagnostics.Log($"Physical props: {_props.Count}");
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
