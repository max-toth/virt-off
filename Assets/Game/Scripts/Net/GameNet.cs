namespace Game.Scripts.Net
{
    /// <summary>Доступ к активному подключению. Выставляется NetClient до загрузки первой сцены.</summary>
    public static class GameNet
    {
        public static NetManager Instance { get; private set; }

        public static string LocalId => Instance?.LocalId;

        /// <summary>Текущее подключение, если сервер уже выдал id.</summary>
        /// <returns>true — можно отправлять пакеты.</returns>
        public static bool TryGetConnected(out NetManager manager)
        {
            manager = Instance;
            return manager is { IsConnected: true };
        }

        internal static void Set(NetManager manager) => Instance = manager;
    }
}
