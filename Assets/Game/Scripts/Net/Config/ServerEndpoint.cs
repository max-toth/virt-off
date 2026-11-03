using System;
using UnityEngine;

namespace Game.Scripts.Net.Config
{
    /// <summary>Адрес сервера: из Resources/ServerConfig, аргумент -server &lt;host&gt; его перекрывает.</summary>
    public sealed class ServerEndpoint
    {
        private const string ServerArgument = "-server";

        public ServerEndpoint(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }
        public int Port { get; }

        public static ServerEndpoint Resolve()
        {
            var config = Resources.Load<ServerConfig>(ServerConfig.ResourcePath);
            var host = config != null ? config.Address : ServerConfig.DefaultAddress;
            int port = config != null ? config.Port : ServerConfig.DefaultPort;

            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ServerArgument) host = args[i + 1];
            }

            return new ServerEndpoint(host, port);
        }
    }
}
