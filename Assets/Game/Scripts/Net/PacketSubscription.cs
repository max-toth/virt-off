using System;

namespace Game.Scripts.Net
{
    // Подписка обработчика на входящие пакеты активного NetManager без повторной подписки.
    public sealed class PacketSubscription
    {
        private readonly Action<string, byte[]> _handler;
        private NetManager _manager;

        public PacketSubscription(Action<string, byte[]> handler) => _handler = handler;

        public void Bind(NetManager manager)
        {
            if (manager == _manager) return;

            Unbind();
            _manager = manager;
            if (manager != null) manager.OnPacketReceived += _handler;
        }

        public void Unbind()
        {
            if (_manager != null) _manager.OnPacketReceived -= _handler;
            _manager = null;
        }
    }
}
