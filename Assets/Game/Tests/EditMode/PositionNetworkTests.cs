using System.Collections.Generic;
using Game.Scripts.Net.Packets;
using Game.Scripts.Player.Sync;
using Game.Scripts.Player.Sync.Models;
using Game.Tests.EditMode.Support;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class PositionNetworkTests
    {
        private FakeTransport _transport;
        private List<PositionMessage> _received;

        [SetUp]
        public void SetUp()
        {
            _transport = FakeTransport.Connected();
            PositionNetwork.Initialize(_transport, _transport.Router);
            _received = new List<PositionMessage>();
            PositionNetwork.Received += _received.Add;
        }

        [TearDown]
        public void TearDown() => PositionNetwork.Shutdown();

        [TestCase(false, (byte)0)]
        [TestCase(true, (byte)1)]
        public void Send_MatchesFormat(bool crouching, byte flags)
        {
            var position = new Vector3(10f, 0.5f, -3f);

            PositionNetwork.Send(position, 90f, crouching);

            var expected = Bytes.Packet(PacketType.Position).Str(FakeTransport.DefaultId).V3(position).F32(90f).U8(flags);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void Send_BeforeIdAssigned_SendsNothing()
        {
            var transport = new FakeTransport();
            PositionNetwork.Initialize(transport, transport.Router);

            PositionNetwork.Send(Vector3.zero, 0f, false);

            Assert.That(PositionNetwork.IsConnected, Is.False);
            Assert.That(transport.Sent, Is.Empty);
        }

        [Test]
        public void TryRead_ReadsAllFields()
        {
            PositionNetwork.Send(new Vector3(1f, 2f, 3f), 45f, true);

            Assert.That(PositionNetwork.TryRead(_transport.LastSent, out var message), Is.True);
            Assert.That(message.Sender, Is.EqualTo(FakeTransport.DefaultId));
            Assert.That(message.Position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(message.Yaw, Is.EqualTo(45f));
            Assert.That(message.Crouching, Is.True);
        }

        [Test]
        public void TryRead_Truncated_Fails()
        {
            PositionNetwork.Send(Vector3.one, 0f, false);

            Assert.That(PositionNetwork.TryRead(Bytes.Truncated(_transport.LastSent), out _), Is.False);
        }

        [Test]
        public void Incoming_FromOther_RaisesReceived()
        {
            _transport.Receive(Bytes.Packet(PacketType.Position).Str("other").V3(Vector3.right).F32(180f).U8(0));

            Assert.That(_received, Has.Count.EqualTo(1));
            Assert.That(_received[0].Sender, Is.EqualTo("other"));
            Assert.That(_received[0].Yaw, Is.EqualTo(180f));
        }

        [Test]
        public void Incoming_OwnOrOtherType_IsIgnored()
        {
            _transport.Receive(Bytes.Packet(PacketType.Position).Str(FakeTransport.DefaultId).V3(Vector3.zero).F32(0f).U8(0));
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("other").V3(Vector3.zero).F32(0f).U8(0));

            Assert.That(_received, Is.Empty);
        }
    }
}
