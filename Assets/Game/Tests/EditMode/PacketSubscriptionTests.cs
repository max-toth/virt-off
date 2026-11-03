using Game.Scripts.Net.Packets;
using Game.Tests.EditMode.Support;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class PacketSubscriptionTests
    {
        private static readonly byte[] AnyPacket = { PacketType.Voice };

        private int _calls;
        private PacketSubscription _subscription;

        [SetUp]
        public void SetUp()
        {
            _calls = 0;
            _subscription = new PacketSubscription((_, _) => _calls++);
        }

        [Test]
        public void BindTwice_ToSameTransport_DoesNotDuplicate()
        {
            var transport = new FakeTransport();
            _subscription.Bind(transport);
            _subscription.Bind(transport);

            transport.Receive(AnyPacket);

            Assert.That(_calls, Is.EqualTo(1));
        }

        [Test]
        public void Bind_ToAnotherTransport_MovesSubscription()
        {
            var first = new FakeTransport();
            var second = new FakeTransport();
            _subscription.Bind(first);
            _subscription.Bind(second);

            first.Receive(AnyPacket);
            second.Receive(AnyPacket);

            Assert.That(_calls, Is.EqualTo(1));
        }

        [Test]
        public void Unbind_StopsDelivery()
        {
            var transport = new FakeTransport();
            _subscription.Bind(transport);
            _subscription.Unbind();

            transport.Receive(AnyPacket);

            Assert.That(_calls, Is.Zero);
        }

        [Test]
        public void Handler_ReceivesLocalIdAndData()
        {
            var transport = FakeTransport.Connected("id-7");
            string localId = null;
            byte[] data = null;
            var subscription = new PacketSubscription((id, packet) => (localId, data) = (id, packet));
            subscription.Bind(transport);

            transport.Receive(AnyPacket);

            Assert.That(localId, Is.EqualTo("id-7"));
            Assert.That(data, Is.SameAs(AnyPacket));
            subscription.Unbind();
        }
    }
}
