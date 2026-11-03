using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Game.Scripts.Net.Packets;
using UnityEngine;

namespace Game.Scripts.Net
{
    /// <summary>UDP-клиент: приём в фоновом потоке, события — в главном потоке из ProcessIncoming.</summary>
    public sealed class NetManager : INetTransport, IDisposable
    {
        private const int ReceiveThreadJoinTimeoutMs = 500;

        // Сервер мог быть ещё не запущен. На повторный Hello с того же адреса он выдаёт новый id
        // и забывает прежний, поэтому Hello повторяется только до первого IdAssigned.
        private const int HelloRetryIntervalMs = 1000;

        private static readonly byte[] HelloPacket = { PacketType.Hello };

        private readonly ConcurrentQueue<byte[]> _incoming = new();
        private readonly PacketWriter _writer = new();
        private UdpClient _socket;
        private IPEndPoint _server;
        private Thread _receiveThread;
        private volatile bool _running;
        private int _lastHelloTick;
        private bool _disposed;

        public event Action<string, byte[]> PacketReceived;
        public event Action IdAssigned;

        public string LocalId { get; private set; }
        public bool IsConnected { get; private set; }

        /// <param name="host">IPv4/IPv6-адрес или имя хоста.</param>
        /// <exception cref="SocketException">Имя хоста не разрешилось.</exception>
        public void StartClient(string host, int port)
        {
            var address = Resolve(host);
            _socket = new UdpClient(address.AddressFamily);
            _server = new IPEndPoint(address, port);
            SendHello();

            _running = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "NetManager.Receive" };
            _receiveThread.Start();
        }

        public void Send(byte[] data, int length)
        {
            if (_server == null) return;

            try
            {
                _socket.Send(data, length, _server);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>Разбирает накопленные пакеты и вызывает события; до получения id повторяет Hello. Только из главного потока.</summary>
        public void ProcessIncoming()
        {
            RetryHello();

            while (_incoming.TryDequeue(out var data))
            {
                // Ошибка в одном обработчике не должна задерживать остальные пакеты кадра.
                try
                {
                    Handle(data);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (IsConnected && !string.IsNullOrEmpty(LocalId))
            {
                _writer.Begin(PacketType.Disconnect);
                _writer.WriteString(LocalId);
                _writer.SendTo(this);
            }

            _running = false;
            _socket?.Close();
            _receiveThread?.Join(ReceiveThreadJoinTimeoutMs);
        }

        /// <summary>Ставит принятый пакет в очередь; вызывается потоком приёма.</summary>
        internal void Enqueue(byte[] data) => _incoming.Enqueue(data);

        private static IPAddress Resolve(string host)
        {
            if (IPAddress.TryParse(host, out var address)) return address;

            var addresses = Dns.GetHostAddresses(host);
            foreach (var candidate in addresses)
            {
                if (candidate.AddressFamily == AddressFamily.InterNetwork) return candidate;
            }

            if (addresses.Length > 0) return addresses[0];
            throw new SocketException((int)SocketError.HostNotFound);
        }

        private void RetryHello()
        {
            if (IsConnected || _server == null) return;
            if (unchecked(Environment.TickCount - _lastHelloTick) < HelloRetryIntervalMs) return;
            SendHello();
        }

        private void SendHello()
        {
            _lastHelloTick = Environment.TickCount;
            Send(HelloPacket, HelloPacket.Length);
        }

        private void Handle(byte[] data)
        {
            if (data.Length >= 2 && data[0] == PacketType.IdAssigned)
            {
                // Поздний ответ на один из повторных Hello: сервер помнит последний выданный id.
                LocalId = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                if (IsConnected) return;

                IsConnected = true;
                IdAssigned?.Invoke();
                return;
            }

            PacketReceived?.Invoke(LocalId, data);
        }

        private void ReceiveLoop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    Enqueue(_socket.Receive(ref remote));
                }
                catch (SocketException e) when (_running && e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // Windows: ICMP «порт недоступен», пока сервер не запущен.
                }
                catch (Exception) when (!_running)
                {
                    // Сокет закрыт в Dispose.
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    break;
                }
            }
        }
    }
}
