using System.Collections.Generic;
using Game.Scripts.Net.Packets;
using Game.Scripts.Voice;
using Game.Scripts.Voice.Models;
using Game.Tests.EditMode.Support;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class VoiceNetworkTests
    {
        private FakeTransport _transport;
        private List<VoicePacket> _received;

        [SetUp]
        public void SetUp()
        {
            _transport = FakeTransport.Connected();
            VoiceNetwork.Initialize(_transport, _transport.Router);
            _received = new List<VoicePacket>();
            VoiceNetwork.Received += _received.Add;
        }

        [TearDown]
        public void TearDown() => VoiceNetwork.Shutdown();

        [Test]
        public void Send_MatchesFormat()
        {
            var opus = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

            VoiceNetwork.Send(opus, 3);

            var expected = Bytes.Packet(PacketType.Voice).Str(FakeTransport.DefaultId).Raw(0xAA, 0xBB, 0xCC);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void TryRead_PointsToOpusPayload()
        {
            byte[] data = Bytes.Packet(PacketType.Voice).Str("other").Raw(1, 2, 3, 4, 5);

            Assert.That(VoiceNetwork.TryRead(data, out var packet), Is.True);
            Assert.That(packet.Sender, Is.EqualTo("other"));
            Assert.That(packet.Data, Is.SameAs(data));
            Assert.That(packet.Offset, Is.EqualTo(1 + 1 + "other".Length));
            Assert.That(packet.Count, Is.EqualTo(5));
        }

        [Test]
        public void TryRead_WithoutSender_Fails()
        {
            Assert.That(VoiceNetwork.TryRead(new[] { PacketType.Voice }, out _), Is.False);
        }

        [Test]
        public void Incoming_FromOther_RaisesReceived_OwnIgnored()
        {
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("other").Raw(9));
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str(FakeTransport.DefaultId).Raw(9));

            Assert.That(_received, Has.Count.EqualTo(1));
            Assert.That(_received[0].Sender, Is.EqualTo("other"));
        }

        [Test]
        public void Connected_RaisedOnIdAssigned()
        {
            var transport = new FakeTransport();
            VoiceNetwork.Initialize(transport, transport.Router);
            var connected = 0;
            VoiceNetwork.Connected += () => connected++;

            Assert.That(VoiceNetwork.IsConnected, Is.False);
            transport.AssignId("late");

            Assert.That(connected, Is.EqualTo(1));
            Assert.That(VoiceNetwork.IsConnected, Is.True);
        }

        [Test]
        public void Reinitialize_UnsubscribesFromPreviousTransport()
        {
            var first = new FakeTransport();
            VoiceNetwork.Initialize(first, first.Router);
            var connected = 0;
            VoiceNetwork.Connected += () => connected++;

            var second = new FakeTransport();
            VoiceNetwork.Initialize(second, second.Router);
            first.AssignId("stale");
            first.Receive(Bytes.Packet(PacketType.Voice).Str("other").Raw(9));

            Assert.That(connected, Is.Zero);
            Assert.That(_received, Is.Empty);
        }
    }
}
