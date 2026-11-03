using System;
using System.Net.Sockets;
using Game.Scripts.Net;
using Game.Scripts.Net.Config;
using UnityEngine;
using VContainer.Unity;

namespace Game.Scripts.Bootstrap
{
    /// <summary>Подключение к серверу на время жизни сцены: входящие пакеты разбираются в PreUpdate, до Update скриптов.</summary>
    public sealed class NetworkSession : IInitializable, IDisposable
    {
        private readonly NetManager _manager;
        private readonly ServerEndpoint _endpoint;

        public NetworkSession(NetManager manager, ServerEndpoint endpoint)
        {
            _manager = manager;
            _endpoint = endpoint;
        }

        public void Initialize()
        {
            Debug.Log($"[Net] Connecting to {_endpoint.Host}:{_endpoint.Port}");
            _manager.IdAssigned += OnIdAssigned;

            try
            {
                _manager.StartClient(_endpoint.Host, _endpoint.Port);
            }
            catch (SocketException e)
            {
                Debug.LogError($"[Net] Cannot resolve server {_endpoint.Host}: {e.Message}");
            }

            NetworkPlayerLoop.Install(_manager.ProcessIncoming);
        }

        // NetManager освобождает контейнер: после этого он отправит DISCONNECT.
        public void Dispose()
        {
            NetworkPlayerLoop.Uninstall();
            _manager.IdAssigned -= OnIdAssigned;
        }

        private void OnIdAssigned() => NetDiagnostics.Log($"[Net] Got ID: {_manager.LocalId}");
    }
}
