using System;
using Game.Scripts.Net.Config;
using UnityEngine;

namespace Game.Scripts.Net
{
    /// <summary>Подключение к серверу до загрузки первой сцены. Адрес — из Resources/ServerConfig, аргумент -server &lt;ip&gt; его перекрывает.</summary>
    public sealed class NetClient : MonoBehaviour
    {
        private const string ServerArgument = "-server";

        private NetManager _manager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var host = new GameObject(nameof(NetClient));
            DontDestroyOnLoad(host);
            host.AddComponent<NetClient>();
        }

        private void Awake()
        {
            var (host, port) = ResolveServer();
            Debug.Log($"[Net] Connecting to {host}:{port}");

            _manager = new NetManager();
            _manager.IdAssigned += OnIdAssigned;
            _manager.StartClient(host, port);
            GameNet.Set(_manager);
            NetworkPlayerLoop.Install(Pump);
        }

        private void OnDestroy()
        {
            NetworkPlayerLoop.Uninstall();
            _manager.IdAssigned -= OnIdAssigned;
            _manager.Dispose();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => NetworkPlayerLoop.Uninstall();

        private void Pump()
        {
            _manager.ProcessIncoming();
            Lobby.RemoveExpired();
        }

        private void OnIdAssigned() => NetDiagnostics.Log($"[Net] Got ID: {_manager.LocalId}");

        private static (string Host, int Port) ResolveServer()
        {
            var config = Resources.Load<ServerConfig>(ServerConfig.ResourcePath);
            var host = config != null ? config.Address : ServerConfig.DefaultAddress;
            int port = config != null ? config.Port : ServerConfig.DefaultPort;

            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ServerArgument) host = args[i + 1];
            }

            return (host, port);
        }
    }
}
