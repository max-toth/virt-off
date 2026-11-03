using System;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Player.Sync.Models;
using UnityEngine;
using VContainer.Unity;

namespace Game.Scripts.Player.Sync
{
    public sealed class PositionNetwork : IInitializable, IDisposable
    {
        private const byte CrouchingFlag = 1;

        private readonly INetTransport _transport;
        private readonly PacketRouter _router;
        private readonly PacketWriter _writer;

        public PositionNetwork(INetTransport transport, PacketRouter router, PacketWriter writer)
        {
            _transport = transport;
            _router = router;
            _writer = writer;
        }

        /// <summary>Позиция другого игрока (свои пакеты отбрасываются).</summary>
        public event Action<PositionMessage> Received;

        public bool IsConnected => _transport.IsConnected;

        public void Initialize() => _router.Subscribe(PacketType.Position, OnPosition);

        public void Dispose() => _router.Unsubscribe(PacketType.Position, OnPosition);

        // POSITION: [type][sender:str][position:12][yaw:4][flags:1]
        public void Send(Vector3 position, float yaw, bool crouching)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.Position);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteVector3(position);
            _writer.WriteSingle(yaw);
            _writer.WriteByte(crouching ? CrouchingFlag : (byte)0);
            _writer.SendTo(_transport);
        }

        public static bool TryRead(byte[] data, out PositionMessage message)
        {
            var reader = new PacketReader(data);
            var sender = reader.ReadString();
            var position = reader.ReadVector3();
            var yaw = reader.ReadSingle();
            var flags = reader.ReadByte();

            message = reader.Failed ? default : new PositionMessage(sender, position, yaw, (flags & CrouchingFlag) != 0);
            return !reader.Failed;
        }

        private void OnPosition(string localId, byte[] data)
        {
            if (TryRead(data, out var message) && message.Sender != localId) Received?.Invoke(message);
        }
    }
}
