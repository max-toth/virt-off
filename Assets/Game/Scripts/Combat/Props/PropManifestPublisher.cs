using System;
using System.Collections.Generic;
using Game.Scripts.Combat.Props.Models;
using Game.Scripts.Net;
using UnityEngine;
using VContainer.Unity;

namespace Game.Scripts.Combat.Props
{
    /// <summary>Отправляет манифест предметов сцены, как только сервер выдал id: в ответ придут их серверные id.</summary>
    public sealed class PropManifestPublisher : IInitializable, IDisposable
    {
        private readonly INetTransport _transport;
        private readonly NetPropRegistry _registry;
        private readonly NetPropSender _sender;
        private readonly List<PropManifestEntry> _manifest = new();
        private bool _sent;

        public PropManifestPublisher(INetTransport transport, NetPropRegistry registry, NetPropSender sender)
        {
            _transport = transport;
            _registry = registry;
            _sender = sender;
        }

        public void Initialize() => _transport.IdAssigned += OnIdAssigned;

        public void Dispose() => _transport.IdAssigned -= OnIdAssigned;

        private void OnIdAssigned()
        {
            if (_sent) return;
            _sent = true;
            _sender.SendManifest(BuildManifest());
        }

        private List<PropManifestEntry> BuildManifest()
        {
            _manifest.Clear();
            foreach (var prop in _registry.All)
                _manifest.Add(new PropManifestEntry(GetHierarchyPath(prop.transform), GetHalfExtents(prop), prop.Body.mass));
            return _manifest;
        }

        private static string GetHierarchyPath(Transform target)
        {
            var path = target.name;
            for (var parent = target.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }

        private static Vector3 GetHalfExtents(NetProp prop) =>
            prop.TryGetComponent(out BoxCollider box) ? box.size * 0.5f : Vector3.one * 0.5f;
    }
}
