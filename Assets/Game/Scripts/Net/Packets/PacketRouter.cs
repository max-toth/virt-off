using System;

namespace Game.Scripts.Net.Packets
{
    /// <summary>Раздаёт входящие пакеты обработчикам по PacketType: модуль получает только свои типы.</summary>
    public sealed class PacketRouter : IDisposable
    {
        private readonly INetTransport _transport;
        private readonly Action<string, byte[]>[] _handlers = new Action<string, byte[]>[byte.MaxValue + 1];

        public PacketRouter(INetTransport transport)
        {
            _transport = transport;
            _transport.PacketReceived += Dispatch;
        }

        /// <summary>Обработчик получает id своего клиента и пакет целиком; data[0] — packetType.</summary>
        public void Subscribe(byte packetType, Action<string, byte[]> handler) => _handlers[packetType] += handler;

        public void Unsubscribe(byte packetType, Action<string, byte[]> handler) => _handlers[packetType] -= handler;

        public void Dispose() => _transport.PacketReceived -= Dispatch;

        private void Dispatch(string localId, byte[] data)
        {
            if (data.Length > 0) _handlers[data[0]]?.Invoke(localId, data);
        }
    }
}
