using System;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Voice.Models;
using VContainer.Unity;

namespace Game.Scripts.Voice
{
    public sealed class VoiceNetwork : IInitializable, IDisposable
    {
        private readonly INetTransport _transport;
        private readonly PacketRouter _router;
        private readonly PacketWriter _writer;
        private ushort _nextSequence;

        public VoiceNetwork(INetTransport transport, PacketRouter router, PacketWriter writer)
        {
            _transport = transport;
            _router = router;
            _writer = writer;
        }

        /// <summary>Голос другого игрока (свои пакеты отбрасываются). Data ссылается на принятый пакет — не хранить после вызова.</summary>
        public event Action<VoicePacket> Received;

        /// <summary>Сервер выдал id: можно запускать микрофон.</summary>
        public event Action Connected;

        public bool IsConnected => _transport.IsConnected;

        public void Initialize()
        {
            _transport.IdAssigned += OnIdAssigned;
            _router.Subscribe(PacketType.Voice, OnVoice);
        }

        public void Dispose()
        {
            _transport.IdAssigned -= OnIdAssigned;
            _router.Unsubscribe(PacketType.Voice, OnVoice);
        }

        // VOICE: [type][sender:str][seq:2][opus...]
        public void Send(byte[] opus, int length)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.Voice);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteUInt16(_nextSequence++);
            _writer.WriteBytes(opus, 0, length);
            _writer.SendTo(_transport);
        }

        public static bool TryRead(byte[] data, out VoicePacket packet)
        {
            var reader = new PacketReader(data);
            var sender = reader.ReadString();
            var sequence = (ushort)reader.ReadUInt16();

            packet = reader.Failed ? default : new VoicePacket(sender, sequence, data, reader.Position, reader.Remaining);
            return !reader.Failed;
        }

        private void OnIdAssigned() => Connected?.Invoke();

        private void OnVoice(string localId, byte[] data)
        {
            if (TryRead(data, out var packet) && packet.Sender != localId) Received?.Invoke(packet);
        }
    }
}
