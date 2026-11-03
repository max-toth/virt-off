public static class GameNet
{
    private static NetManager instance;
    public static NetManager Instance => instance;
    public static void Set(NetManager nm) => instance = nm;
}
