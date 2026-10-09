using UnityEngine;

public static class AppSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var go = new GameObject("NetManager");
        Object.DontDestroyOnLoad(go);
        var nm = go.AddComponent<NetComponent>();
        nm.StartNet();
    }
}

public class NetComponent : MonoBehaviour
{
    public NetManager Manager { get; private set; }

    public void StartNet()
    {
        Manager = new NetManager();

        var config = Resources.Load<ServerConfig>("ServerConfig");
        var host = config != null ? config.address : "193.218.141.214";
        var port = config != null ? config.port : 7777;

        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "-server" && i + 1 < args.Length)
                host = args[i + 1];

        Debug.Log($"[Client] Connecting to {host}:{port}...");
        Manager.StartClient(host, port);

        GameNet.Set(Manager);
    }

    void OnDestroy() => Manager?.Dispose();
}

public static class GameNet
{
    private static NetManager instance;
    public static NetManager Instance => instance;
    public static void Set(NetManager nm) => instance = nm;
}
