using System.Collections.Generic;
using Game.Scripts.Net.Packets;
using Game.Tests.EditMode.Support;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class PacketRouterTests
    {
        private FakeTransport _transport;
        private List<byte> _received;

        [SetUp]
        public void SetUp()
        {
            _transport = FakeTransport.Connected("id-7");
            _received = new List<byte>();
        }

        [Test]
        public void Dispatch_OnlyToHandlersOfPacketType()
        {
            _transport.Router.Subscribe(PacketType.Voice, (_, data) => _received.Add(data[0]));

            _transport.Receive(new[] { PacketType.Position });
            _transport.Receive(new[] { PacketType.Voice });

            Assert.That(_received, Is.EqualTo(new[] { PacketType.Voice }));
        }

        [Test]
        public void Dispatch_ToAllHandlersOfType()
        {
            _transport.Router.Subscribe(PacketType.Voice, (_, data) => _received.Add(1));
            _transport.Router.Subscribe(PacketType.Voice, (_, data) => _received.Add(2));

            _transport.Receive(new[] { PacketType.Voice });

            Assert.That(_received, Is.EqualTo(new byte[] { 1, 2 }));
        }

        [Test]
        public void Handler_ReceivesLocalIdAndPacket()
        {
            string localId = null;
            byte[] packet = null;
            var data = new[] { PacketType.Sound, (byte)1 };
            _transport.Router.Subscribe(PacketType.Sound, (id, received) => (localId, packet) = (id, received));

            _transport.Receive(data);

            Assert.That(localId, Is.EqualTo("id-7"));
            Assert.That(packet, Is.SameAs(data));
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            void Handler(string id, byte[] data) => _received.Add(data[0]);
            _transport.Router.Subscribe(PacketType.Voice, Handler);
            _transport.Router.Unsubscribe(PacketType.Voice, Handler);

            _transport.Receive(new[] { PacketType.Voice });

            Assert.That(_received, Is.Empty);
        }

        [Test]
        public void EmptyPacket_IsIgnored()
        {
            _transport.Router.Subscribe(0, (_, data) => _received.Add(0));

            _transport.Receive(new byte[0]);

            Assert.That(_received, Is.Empty);
        }

        [Test]
        public void Dispose_UnsubscribesFromTransport()
        {
            _transport.Router.Subscribe(PacketType.Voice, (_, data) => _received.Add(data[0]));

            _transport.Router.Dispose();
            _transport.Receive(new[] { PacketType.Voice });

            Assert.That(_received, Is.Empty);
        }
    }
}
