using System;
using System.Globalization;
using UnityEngine;

namespace Game.Scripts.Net.Config
{
    /// <summary>Адрес сервера: из Resources/ServerConfig, аргумента -server &lt;host[:port]&gt; или строки «хост[:порт]».</summary>
    public sealed class ServerEndpoint
    {
        private const string ServerArgument = "-server";

        /// <summary>Сервер, запущенный на этой же машине.</summary>
        public static readonly ServerEndpoint Local = new("127.0.0.1", ServerConfig.DefaultPort);

        public ServerEndpoint(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }
        public int Port { get; }

        /// <summary>Сервер из ServerConfig, без учёта аргументов командной строки.</summary>
        public static ServerEndpoint FromConfig()
        {
            var config = Resources.Load<ServerConfig>(ServerConfig.ResourcePath);
            return config != null
                ? new ServerEndpoint(config.Address, config.Port)
                : new ServerEndpoint(ServerConfig.DefaultAddress, ServerConfig.DefaultPort);
        }

        /// <summary>Сервер из аргумента -server, если он передан: тогда стартовое меню не показывается.</summary>
        public static bool TryGetFromCommandLine(out ServerEndpoint endpoint)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ServerArgument && TryParse(args[i + 1], out endpoint)) return true;
            }

            endpoint = null;
            return false;
        }

        /// <summary>Разбирает «хост» или «хост:порт»; без порта — ServerConfig.DefaultPort.</summary>
        public static bool TryParse(string address, out ServerEndpoint endpoint)
        {
            endpoint = null;
            if (string.IsNullOrWhiteSpace(address)) return false;

            if (!TrySplitPort(address.Trim(), out var host, out var port)) return false;

            host = host.Trim('[', ']');
            if (host.Length == 0 || host.Contains(' ')) return false;

            endpoint = new ServerEndpoint(host, port);
            return true;
        }

        public override string ToString() => Host.Contains(':') ? $"[{Host}]:{Port}" : $"{Host}:{Port}";

        // Порт отделяется последним двоеточием; адрес IPv6 с двоеточиями пишется в скобках: [::1]:7777.
        private static bool TrySplitPort(string address, out string host, out int port)
        {
            host = address;
            port = ServerConfig.DefaultPort;

            var colon = address.LastIndexOf(':');
            var bracket = address.LastIndexOf(']');
            var hasPort = colon > bracket && (bracket >= 0 || address.IndexOf(':') == colon);
            if (!hasPort) return true;

            host = address.Substring(0, colon);
            return int.TryParse(address.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port)
                   && port is >= 1 and <= ushort.MaxValue;
        }
    }
}
