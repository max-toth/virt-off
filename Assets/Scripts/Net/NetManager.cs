using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetManager : IDisposable
{
    public event Action<ulong, byte[]> OnPacketReceived;

    private UdpClient socket;
    private Thread recvThread;
    private volatile bool running;
    private IPEndPoint serverEndpoint;
    public ulong LocalId { get; private set; }
    public bool IsConnected { get; private set; }

    public void StartClient(string host, int port)
    {
        socket = new UdpClient();
        serverEndpoint = new IPEndPoint(IPAddress.Parse(host), port);
        SendRaw(serverEndpoint, new byte[] { 0xFF });

        Debug.Log($"[Net] Connecting to {host}:{port}");

        running = true;
        recvThread = new Thread(ReceiveLoop) { IsBackground = true };
        recvThread.Start();
    }

    public void Send(byte[] data)
    {
        if (serverEndpoint != null)
            SendRaw(serverEndpoint, data);
    }

    private void SendRaw(IPEndPoint ep, byte[] data)
    {
        try { socket.Send(data, data.Length, ep); }
        catch { }
    }

    private void ReceiveLoop()
    {
        while (running)
        {
            try
            {
                var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = socket.Receive(ref remoteEp);
                MainThreadQueue.Enqueue(() => HandlePacket(data));
            }
            catch { break; }
        }
    }

    private void HandlePacket(byte[] data)
    {
        if (data.Length >= 9 && data[0] == 0xFE)
        {
            LocalId = BitConverter.ToUInt64(data, 1);
            IsConnected = true;
            Debug.Log($"[Net] Got ID: {LocalId}");
            return;
        }

        OnPacketReceived?.Invoke(LocalId, data);
    }

    public void Dispose()
    {
        running = false;
        socket?.Close();
        recvThread?.Join(500);
    }
}

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
