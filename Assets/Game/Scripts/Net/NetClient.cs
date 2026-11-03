using System;
using Game.Scripts.Combat;
using Game.Scripts.Combat.Props;
using Game.Scripts.Net.Config;
using Game.Scripts.Player.Sync;
using Game.Scripts.Voice;
using UnityEngine;

namespace Game.Scripts.Net
{
    /// <summary>Точка сборки сетевого слоя: создаёт подключение до загрузки первой сцены и раздаёт его модулям.
    /// Адрес — из Resources/ServerConfig, аргумент -server &lt;ip&gt; его перекрывает.</summary>
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
            Attach(_manager);
            _manager.StartClient(host, port);
            NetworkPlayerLoop.Install(Pump);
        }

        private void OnDestroy()
        {
            NetworkPlayerLoop.Uninstall();
            _manager.IdAssigned -= OnIdAssigned;
            Detach();
            _manager.Dispose();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => NetworkPlayerLoop.Uninstall();

        private static void Attach(INetTransport transport)
        {
            GameNet.Set(transport);
            Lobby.Initialize(transport);
            CombatNetwork.Initialize(transport);
            NetPropSender.Initialize(transport);
            NetPropReceiver.Initialize(transport);
            PositionNetwork.Initialize(transport);
            VoiceNetwork.Initialize(transport);
        }

        private static void Detach()
        {
            VoiceNetwork.Shutdown();
            PositionNetwork.Shutdown();
            NetPropReceiver.Shutdown();
            NetPropSender.Shutdown();
            CombatNetwork.Shutdown();
            Lobby.Shutdown();
            GameNet.Set(null);
        }

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
