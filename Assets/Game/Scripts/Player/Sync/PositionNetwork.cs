using System;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Player.Sync.Models;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    public static class PositionNetwork
    {
        private const byte CrouchingFlag = 1;

        private static INetTransport _transport;
        private static PacketRouter _router;

        /// <summary>Позиция другого игрока (свои пакеты отбрасываются).</summary>
        public static event Action<PositionMessage> Received;

        public static bool IsConnected => _transport is { IsConnected: true };

        public static void Initialize(INetTransport transport, PacketRouter router)
        {
            Unsubscribe();
            _transport = transport;
            _router = router;
            router.Subscribe(PacketType.Position, OnPosition);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Shutdown()
        {
            Unsubscribe();
            _transport = null;
            Received = null;
        }

        // POSITION: [type][sender:str][position:12][yaw:4][flags:1]
        public static void Send(Vector3 position, float yaw, bool crouching)
        {
            if (_transport is not { IsConnected: true } transport) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.Position);
            writer.WriteString(transport.LocalId);
            writer.WriteVector3(position);
            writer.WriteSingle(yaw);
            writer.WriteByte(crouching ? CrouchingFlag : (byte)0);
            writer.SendTo(transport);
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

        private static void Unsubscribe()
        {
            _router?.Unsubscribe(PacketType.Position, OnPosition);
            _router = null;
        }

        private static void OnPosition(string localId, byte[] data)
        {
            if (TryRead(data, out var message) && message.Sender != localId) Received?.Invoke(message);
        }
    }
}
