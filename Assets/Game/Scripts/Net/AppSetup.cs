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
