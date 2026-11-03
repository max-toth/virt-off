using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Voice.Models;

namespace Game.Scripts.Voice
{
    public static class VoiceNetwork
    {
        // VOICE: [type][sender:str][opus...]
        public static void Send(byte[] opus, int length)
        {
            if (!GameNet.TryGetConnected(out var manager)) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.Voice);
            writer.WriteString(manager.LocalId);
            writer.WriteBytes(opus, 0, length);
            writer.SendTo(manager);
        }

        public static bool TryRead(byte[] data, out VoicePacket packet)
        {
            var reader = new PacketReader(data);
            var sender = reader.ReadString();

            packet = reader.Failed ? default : new VoicePacket(sender, data, reader.Position, reader.Remaining);
            return !reader.Failed;
        }
    }
}
