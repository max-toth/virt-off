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

        private static readonly byte[] HelloPacket = { PacketType.Hello };

        private readonly ConcurrentQueue<byte[]> _incoming = new();
        private UdpClient _socket;
        private IPEndPoint _server;
        private Thread _receiveThread;
        private volatile bool _running;

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
            Send(HelloPacket, HelloPacket.Length);

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

        /// <summary>Разбирает накопленные пакеты и вызывает события. Только из главного потока.</summary>
        public void ProcessIncoming()
        {
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
            if (IsConnected && !string.IsNullOrEmpty(LocalId))
            {
                var writer = PacketWriter.Shared;
                writer.Begin(PacketType.Disconnect);
                writer.WriteString(LocalId);
                writer.SendTo(this);
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

        private void Handle(byte[] data)
        {
            if (data.Length >= 2 && data[0] == PacketType.IdAssigned)
            {
                LocalId = Encoding.UTF8.GetString(data, 1, data.Length - 1);
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
