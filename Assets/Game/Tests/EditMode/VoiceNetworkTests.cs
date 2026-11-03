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
        private VoiceNetwork _network;
        private List<VoicePacket> _received;

        [SetUp]
        public void SetUp()
        {
            _transport = FakeTransport.Connected();
            _network = Create(_transport);
            _received = new List<VoicePacket>();
            _network.Received += _received.Add;
        }

        [TearDown]
        public void TearDown() => _network.Dispose();

        [Test]
        public void Send_MatchesFormat()
        {
            var opus = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

            _network.Send(opus, 3);

            var expected = Bytes.Packet(PacketType.Voice).Str(FakeTransport.DefaultId).U16(0).Raw(0xAA, 0xBB, 0xCC);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void Send_NumbersFramesInOrder()
        {
            _network.Send(new byte[] { 1 }, 1);
            _network.Send(new byte[] { 1 }, 1);
            _network.Send(new byte[] { 1 }, 1);

            for (var i = 0; i < 3; i++)
            {
                Assert.That(VoiceNetwork.TryRead(_transport.Sent[i], out var packet), Is.True);
                Assert.That(packet.Sequence, Is.EqualTo(i));
            }
        }

        [Test]
        public void TryRead_PointsToOpusPayload()
        {
            byte[] data = Bytes.Packet(PacketType.Voice).Str("other").U16(513).Raw(1, 2, 3, 4, 5);

            Assert.That(VoiceNetwork.TryRead(data, out var packet), Is.True);
            Assert.That(packet.Sender, Is.EqualTo("other"));
            Assert.That(packet.Sequence, Is.EqualTo(513));
            Assert.That(packet.Data, Is.SameAs(data));
            Assert.That(packet.Offset, Is.EqualTo(1 + 1 + "other".Length + 2));
            Assert.That(packet.Count, Is.EqualTo(5));
        }

        [Test]
        public void TryRead_WithoutSequence_Fails()
        {
            Assert.That(VoiceNetwork.TryRead(new[] { PacketType.Voice }, out _), Is.False);
            Assert.That(VoiceNetwork.TryRead(Bytes.Packet(PacketType.Voice).Str("other").U8(1), out _), Is.False);
        }

        [Test]
        public void Incoming_FromOther_RaisesReceived_OwnIgnored()
        {
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("other").U16(0).Raw(9));
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str(FakeTransport.DefaultId).U16(0).Raw(9));

            Assert.That(_received, Has.Count.EqualTo(1));
            Assert.That(_received[0].Sender, Is.EqualTo("other"));
        }

        [Test]
        public void Connected_RaisedOnIdAssigned()
        {
            var transport = new FakeTransport();
            var network = Create(transport);
            var connected = 0;
            network.Connected += () => connected++;

            Assert.That(network.IsConnected, Is.False);
            transport.AssignId("late");

            Assert.That(connected, Is.EqualTo(1));
            Assert.That(network.IsConnected, Is.True);
        }

        [Test]
        public void Dispose_Unsubscribes()
        {
            var transport = new FakeTransport();
            var network = Create(transport);
            var connected = 0;
            network.Connected += () => connected++;
            var received = 0;
            network.Received += _ => received++;

            network.Dispose();
            transport.AssignId("late");
            transport.Receive(Bytes.Packet(PacketType.Voice).Str("other").U16(0).Raw(9));

            Assert.That(connected, Is.Zero);
            Assert.That(received, Is.Zero);
        }

        private static VoiceNetwork Create(FakeTransport transport)
        {
            var network = new VoiceNetwork(transport, transport.Router, new PacketWriter());
            network.Initialize();
            return network;
        }
    }
}
