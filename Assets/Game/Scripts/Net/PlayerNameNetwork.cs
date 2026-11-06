using System;
using Game.Scripts.Net.Packets;
using VContainer.Unity;

namespace Game.Scripts.Net
{
    /// <summary>Пакеты имён: SET_NAME серверу и PLAYER_NAME от него (сервер чистит имя и рассылает всем, включая автора).</summary>
    public sealed class PlayerNameNetwork : IInitializable, IDisposable
    {
        private readonly INetTransport _transport;
        private readonly PacketRouter _router;
        private readonly PacketWriter _writer;

        public PlayerNameNetwork(INetTransport transport, PacketRouter router, PacketWriter writer)
        {
            _transport = transport;
            _router = router;
            _writer = writer;
        }

        /// <summary>Имя игрока от сервера, в том числе своё; аргументы — id и имя.</summary>
        public event Action<string, string> Received;

        public void Initialize() => _router.Subscribe(PacketType.PlayerName, OnPlayerName);

        public void Dispose() => _router.Unsubscribe(PacketType.PlayerName, OnPlayerName);

        // SET_NAME: [type][sender:str][name:str]
        public void Send(string name)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.SetName);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteString(name);
            _writer.SendTo(_transport);
        }

        // PLAYER_NAME: [type][id:str][name:str]
        public static bool TryRead(byte[] data, out string id, out string name)
        {
            var reader = new PacketReader(data);
            id = reader.ReadString();
            name = reader.ReadString();
            return !reader.Failed;
        }

        private void OnPlayerName(string localId, byte[] data)
        {
            if (TryRead(data, out var id, out var name)) Received?.Invoke(id, name);
        }
    }
}
