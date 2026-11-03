using System;
using System.Collections.Concurrent;
using UnityEngine;

internal static class MainThreadQueue
{
    private static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
        var go = new GameObject("MainThreadDispatcher");
        go.AddComponent<MainThreadDispatcher>();
        GameObject.DontDestroyOnLoad(go);
    }

    public static void Enqueue(Action action) => queue.Enqueue(action);
    public static void ExecuteAll()
    {
        while (queue.TryDequeue(out var a)) a();
    }

    private class MainThreadDispatcher : MonoBehaviour
    {
        void Update() => ExecuteAll();
    }
}
