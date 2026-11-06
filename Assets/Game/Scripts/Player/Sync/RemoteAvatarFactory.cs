using System;
using System.Linq;
using Game.Scripts.Net;
using Game.Scripts.Player.Animation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Player.Sync
{
    public sealed class RemoteAvatarFactory : IDisposable
    {
        private const string TemplateName = "RemoteAvatarTemplate";

        private readonly GameObject[] _prefabs;
        private readonly float _prefabScale;
        private readonly GameObject _template;
        private readonly bool _hasTemplate;
        private readonly RemotePlayers _registry;
        private readonly PlayerNames _names;
        private readonly Transform _view;

        public RemoteAvatarFactory(Transform localPlayer, GameObject[] prefabs, float prefabScale, RemotePlayers registry,
            PlayerNames names, Transform view)
        {
            _registry = registry;
            _names = names;
            _view = view;
            _prefabs = prefabs;
            _prefabScale = prefabScale;
            _hasTemplate = TryCreateTemplate(localPlayer, out _template);
        }

        public RemotePlayer Create(string id, Vector3 position, float yaw)
        {
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var avatar = Instantiate(id, position, rotation);
            avatar.name = $"RemotePlayer_{id}";
            RemoteAvatar.Attach(avatar, id, _registry);
            RemoteNameplate.Attach(avatar, id, _names, _view);
            avatar.TryGetComponent(out ProceduralWalk walk);
            return new RemotePlayer(avatar.transform, walk);
        }

        public void Dispose()
        {
            if (_hasTemplate) Object.Destroy(_template);
        }

        // Своей модели добавляется анимация, её неактивная копия становится шаблоном для чужих игроков.
        private static bool TryCreateTemplate(Transform localPlayer, out GameObject template)
        {
            template = null;
            var skin = localPlayer.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin == null) return false;

            var model = skin.transform;
            while (model.parent != localPlayer) model = model.parent;
            EnsureWalk(model.gameObject);

            template = Object.Instantiate(model.gameObject);
            template.name = TemplateName;
            template.transform.localScale = model.lossyScale;
            template.SetActive(false);
            return true;
        }

        private static void EnsureWalk(GameObject avatar)
        {
            if (!avatar.TryGetComponent(out ProceduralWalk _)) avatar.AddComponent<ProceduralWalk>();
        }

        // Одинаковый выбор модели у всех клиентов.
        private static int StableIndex(string id, int count)
        {
            unchecked
            {
                var hash = id.Aggregate(17, (current, c) => current * 31 + c);
                return (hash & 0x7fffffff) % count;
            }
        }

        private GameObject Instantiate(string id, Vector3 position, Quaternion rotation)
        {
            if (TryGetPrefab(id, out var prefab))
            {
                var avatar = Object.Instantiate(prefab, position, rotation);
                avatar.transform.localScale = Vector3.one * _prefabScale;
                EnsureWalk(avatar);
                return avatar;
            }

            if (!_hasTemplate) return CreatePlaceholder(position, rotation);

            var copy = Object.Instantiate(_template, position, rotation);
            copy.SetActive(true);
            return copy;
        }

        private bool TryGetPrefab(string id, out GameObject prefab)
        {
            prefab = null;
            if (_prefabs == null || _prefabs.Length == 0) return false;

            prefab = _prefabs[StableIndex(id, _prefabs.Length)];
            return prefab != null;
        }

        private static GameObject CreatePlaceholder(Vector3 position, Quaternion rotation)
        {
            var avatar = new GameObject();
            avatar.transform.SetPositionAndRotation(position, rotation);

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(avatar.transform, false);
            capsule.transform.localPosition = Vector3.up;
            Object.Destroy(capsule.GetComponent<Collider>());
            return avatar;
        }
    }
}
