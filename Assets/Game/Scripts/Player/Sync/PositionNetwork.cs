using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Player.Sync.Models;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    public static class PositionNetwork
    {
        private const byte CrouchingFlag = 1;

        // POSITION: [type][sender:str][position:12][yaw:4][flags:1]
        public static void Send(Vector3 position, float yaw, bool crouching)
        {
            if (!GameNet.TryGetConnected(out var manager)) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.Position);
            writer.WriteString(manager.LocalId);
            writer.WriteVector3(position);
            writer.WriteSingle(yaw);
            writer.WriteByte(crouching ? CrouchingFlag : (byte)0);
            writer.SendTo(manager);
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
    }
}
