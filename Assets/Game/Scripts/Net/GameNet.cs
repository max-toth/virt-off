namespace Game.Scripts.Net
{
    /// <summary>Временный мост к подключению для презентеров UI; остальной код получает INetTransport от NetClient.</summary>
    public static class GameNet
    {
        public static INetTransport Transport { get; private set; }

        public static string LocalId => Transport?.LocalId;

        /// <summary>Текущее подключение, если сервер уже выдал id.</summary>
        /// <returns>true — можно отправлять пакеты.</returns>
        public static bool TryGetConnected(out INetTransport transport)
        {
            transport = Transport;
            return transport is { IsConnected: true };
        }

        internal static void Set(INetTransport transport) => Transport = transport;
    }
}
