using Game.Scripts.Combat.Props;
using UnityEngine;

public class NetComponent : MonoBehaviour
{
    public NetManager Manager { get; private set; }
    bool manifestSent;

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
        Manager.OnIdAssigned += OnIdAssigned;
        Manager.StartClient(host, port);

        GameNet.Set(Manager);
    }

    void OnIdAssigned()
    {
        if (manifestSent) return;
        manifestSent = true;
        NetPropSender.SendManifest();
    }

    void OnDestroy()
    {
        if (Manager != null) Manager.OnIdAssigned -= OnIdAssigned;
        Manager?.Dispose();
    }
}
