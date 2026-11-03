namespace Game.Scripts.Net
{
    public static class GameNet
    {
        public static NetManager Instance { get; private set; }

        public static string LocalId => Instance?.LocalId;

        public static bool TryGetConnected(out NetManager manager)
        {
            manager = Instance;
            return manager is { IsConnected: true };
        }

        internal static void Set(NetManager manager) => Instance = manager;
    }
}
