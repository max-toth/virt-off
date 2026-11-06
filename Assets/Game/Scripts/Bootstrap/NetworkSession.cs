using System;
using System.Net.Sockets;
using Game.Scripts.Net;
using Game.Scripts.Net.Config;
using UnityEngine;
using VContainer.Unity;

namespace Game.Scripts.Bootstrap
{
    /// <summary>Подключение к серверу на время жизни сцены: входящие пакеты разбираются в PreUpdate, до Update скриптов.
    /// Без стартового меню подключается сразу, иначе — когда меню вызовет Connect.</summary>
    public sealed class NetworkSession : IInitializable, IDisposable
    {
        private readonly NetManager _manager;
        private readonly StartupOptions _options;

        public NetworkSession(NetManager manager, ServerEndpoint defaultEndpoint, StartupOptions options)
        {
            _manager = manager;
            DefaultEndpoint = defaultEndpoint;
            _options = options;
        }

        /// <summary>Сервер из -server или ServerConfig: к нему сцена без стартового меню подключается сразу.</summary>
        public ServerEndpoint DefaultEndpoint { get; }

        /// <summary>Подключение уже начато: сменить сервер можно только перезапуском сцены.</summary>
        public bool IsStarted { get; private set; }

        /// <summary>Сервер выбирают в стартовом меню; без него сервер задан заранее и сменить его нельзя.</summary>
        public bool HasStartMenu => _options.ShowStartMenu;

        /// <summary>Подключение ждёт выбора сервера в стартовом меню.</summary>
        public bool AwaitsServerChoice => _options.ShowStartMenu && !IsStarted;

        public void Initialize()
        {
            _manager.IdAssigned += OnIdAssigned;
            NetworkPlayerLoop.Install(_manager.ProcessIncoming);

            if (!_options.ShowStartMenu) Connect(DefaultEndpoint);
        }

        // NetManager освобождает контейнер: после этого он отправит DISCONNECT.
        public void Dispose()
        {
            NetworkPlayerLoop.Uninstall();
            _manager.IdAssigned -= OnIdAssigned;
        }

        /// <returns>false — подключение уже начато или имя хоста не разрешилось.</returns>
        public bool Connect(ServerEndpoint endpoint)
        {
            if (IsStarted) return false;

            Debug.Log($"[Net] Connecting to {endpoint}");
            try
            {
                _manager.StartClient(endpoint.Host, endpoint.Port);
            }
            catch (SocketException e)
            {
                Debug.LogError($"[Net] Cannot resolve server {endpoint.Host}: {e.Message}");
                return false;
            }

            IsStarted = true;
            return true;
        }

        private void OnIdAssigned() => NetDiagnostics.Log($"[Net] Got ID: {_manager.LocalId}");
    }
}
