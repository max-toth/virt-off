using System;

namespace Game.Scripts.Net.Packets
{
    public sealed class PacketSubscription
    {
        private readonly Action<string, byte[]> _handler;
        private INetTransport _transport;

        public PacketSubscription(Action<string, byte[]> handler) => _handler = handler;

        /// <summary>Подписывает на transport; от предыдущего отписывает, повторная подписка на тот же не дублируется.</summary>
        public void Bind(INetTransport transport)
        {
            if (transport == _transport) return;

            Unbind();
            _transport = transport;
            if (transport != null) transport.PacketReceived += _handler;
        }

        public void Unbind()
        {
            if (_transport != null) _transport.PacketReceived -= _handler;
            _transport = null;
        }
    }
}
