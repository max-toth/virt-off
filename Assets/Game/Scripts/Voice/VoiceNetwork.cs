using System;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Voice.Models;
using UnityEngine;

namespace Game.Scripts.Voice
{
    public static class VoiceNetwork
    {
        private static readonly PacketSubscription Subscription = new(OnPacket);
        private static INetTransport _transport;

        /// <summary>Голос другого игрока (свои пакеты отбрасываются). Data ссылается на принятый пакет — не хранить после вызова.</summary>
        public static event Action<VoicePacket> Received;

        /// <summary>Сервер выдал id: можно запускать микрофон.</summary>
        public static event Action Connected;

        public static bool IsConnected => _transport is { IsConnected: true };

        public static void Initialize(INetTransport transport)
        {
            UnsubscribeIdAssigned();
            _transport = transport;
            if (transport != null) transport.IdAssigned += OnIdAssigned;
            Subscription.Bind(transport);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Shutdown()
        {
            UnsubscribeIdAssigned();
            Subscription.Unbind();
            _transport = null;
            Received = null;
            Connected = null;
        }

        // VOICE: [type][sender:str][opus...]
        public static void Send(byte[] opus, int length)
        {
            if (_transport is not { IsConnected: true } transport) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.Voice);
            writer.WriteString(transport.LocalId);
            writer.WriteBytes(opus, 0, length);
            writer.SendTo(transport);
        }

        public static bool TryRead(byte[] data, out VoicePacket packet)
        {
            var reader = new PacketReader(data);
            var sender = reader.ReadString();

            packet = reader.Failed ? default : new VoicePacket(sender, data, reader.Position, reader.Remaining);
            return !reader.Failed;
        }

        private static void UnsubscribeIdAssigned()
        {
            if (_transport != null) _transport.IdAssigned -= OnIdAssigned;
        }

        private static void OnIdAssigned() => Connected?.Invoke();

        private static void OnPacket(string localId, byte[] data)
        {
            if (data.Length == 0 || data[0] != PacketType.Voice) return;
            if (TryRead(data, out var packet) && packet.Sender != localId) Received?.Invoke(packet);
        }
    }
}
