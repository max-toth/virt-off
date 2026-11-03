using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetManager : IDisposable
{
    public event Action<string, byte[]> OnPacketReceived;
    public event Action OnIdAssigned;

    private UdpClient socket;
    private Thread recvThread;
    private volatile bool running;
    private IPEndPoint serverEndpoint;
    public string LocalId { get; private set; }
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

    public void Send(byte[] data) => Send(data, data.Length);

    public void Send(byte[] data, int length)
    {
        if (serverEndpoint != null)
            SendRaw(serverEndpoint, data, length);
    }

    private void SendRaw(IPEndPoint ep, byte[] data) => SendRaw(ep, data, data.Length);

    private void SendRaw(IPEndPoint ep, byte[] data, int length)
    {
        try { socket.Send(data, length, ep); }
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
        if (data.Length >= 2 && data[0] == 0xFE)
        {
            LocalId = System.Text.Encoding.UTF8.GetString(data, 1, data.Length - 1);
            IsConnected = true;
            Debug.Log($"[Net] Got ID: {LocalId}");
            OnIdAssigned?.Invoke();
            return;
        }

        OnPacketReceived?.Invoke(LocalId, data);
    }

    public void Dispose()
    {
        if (IsConnected && !string.IsNullOrEmpty(LocalId))
        {
            // 0x0D DISCONNECT: [id:str]
            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(LocalId);
            var p = new byte[1 + 1 + idBytes.Length];
            p[0] = 0x0D;
            p[1] = (byte)idBytes.Length;
            System.Array.Copy(idBytes, 0, p, 2, idBytes.Length);
            SendRaw(serverEndpoint, p);
        }

        running = false;
        socket?.Close();
        recvThread?.Join(500);
    }
}
