using System.Collections.Generic;
using Game.Scripts.Combat.Props;
using Game.Scripts.Combat.Props.Models;
using Game.Scripts.Net.Packets;
using Game.Tests.EditMode.Support;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class NetPropProtocolTests
    {
        private FakeTransport _transport;

        [SetUp]
        public void SetUp()
        {
            _transport = FakeTransport.Connected();
            NetPropSender.Initialize(_transport);
        }

        [TearDown]
        public void TearDown() => NetPropSender.Shutdown();

        [Test]
        public void SendManifest_MatchesFormat()
        {
            var entries = new[]
            {
                new PropManifestEntry("Office/Chair", new Vector3(0.3f, 0.5f, 0.3f), 8f),
                new PropManifestEntry("Office/Desk/Eraser", Vector3.one * 0.05f, 0.1f)
            };

            NetPropSender.SendManifest(entries);

            var expected = Bytes.Packet(PacketType.PropManifest).Str(FakeTransport.DefaultId).U16(2)
                .Str("Office/Chair").V3(entries[0].HalfExtents).F32(8f)
                .Str("Office/Desk/Eraser").V3(entries[1].HalfExtents).F32(0.1f);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void SendManifest_Empty_SendsNothing()
        {
            NetPropSender.SendManifest(new PropManifestEntry[0]);

            Assert.That(_transport.Sent, Is.Empty);
        }

        [Test]
        public void SendPickupRequest_MatchesFormat()
        {
            NetPropSender.SendPickupRequest(17);

            var expected = Bytes.Packet(PacketType.PropPickupRequest).Str(FakeTransport.DefaultId).I32(17);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void SendRelease_MatchesFormat()
        {
            var velocity = new Vector3(1f, 2f, 3f);
            var angular = new Vector3(0f, -1f, 0f);

            NetPropSender.SendRelease(5, velocity, angular);

            var expected = Bytes.Packet(PacketType.PropRelease).Str(FakeTransport.DefaultId).I32(5).V3(velocity).V3(angular).U8(0);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void SendState_MatchesFormat()
        {
            var position = new Vector3(4f, 1f, -2f);
            var rotation = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f);
            var velocity = new Vector3(0f, -9.8f, 0f);

            NetPropSender.SendState(3, position, rotation, velocity, PropStateFlags.Held);

            var expected = Bytes.Packet(PacketType.PropState).Str(FakeTransport.DefaultId).I32(3)
                .V3(position).Quat(rotation).V3(velocity).U8((byte)PropStateFlags.Held);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void Send_BeforeIdAssigned_SendsNothing()
        {
            var transport = new FakeTransport();
            NetPropSender.Initialize(transport);

            NetPropSender.SendPickupRequest(1);
            NetPropSender.SendRelease(1, Vector3.zero, Vector3.zero);
            NetPropSender.SendState(1, Vector3.zero, Quaternion.identity, Vector3.zero, PropStateFlags.None);

            Assert.That(NetPropSender.IsConnected, Is.False);
            Assert.That(transport.Sent, Is.Empty);
        }

        [Test]
        public void TryReadState_ReadsWhatSendStateWrites()
        {
            var position = new Vector3(1f, 2f, 3f);
            var rotation = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f);
            NetPropSender.SendState(9, position, rotation, Vector3.one, PropStateFlags.Settled);

            Assert.That(NetPropReceiver.TryReadState(_transport.LastSent, out var state), Is.True);
            Assert.That(state.Sender, Is.EqualTo(FakeTransport.DefaultId));
            Assert.That(state.PropId, Is.EqualTo(9));
            Assert.That(state.Position, Is.EqualTo(position));
            Assert.That(state.Rotation, Is.EqualTo(rotation));
            Assert.That(state.Flags, Is.EqualTo(PropStateFlags.Settled));
        }

        [Test]
        public void TryReadState_Truncated_Fails()
        {
            NetPropSender.SendState(9, Vector3.zero, Quaternion.identity, Vector3.zero, PropStateFlags.None);

            Assert.That(NetPropReceiver.TryReadState(Bytes.Truncated(_transport.LastSent), out _), Is.False);
        }

        [Test]
        public void TryReadManifestAck_ReadsIdsInOrder()
        {
            byte[] data = Bytes.Packet(PacketType.PropManifestAck).U16(3).I32(100).I32(-1).I32(102);
            var ids = new List<int> { 999 };

            Assert.That(NetPropReceiver.TryReadManifestAck(data, ids), Is.True);
            Assert.That(ids, Is.EqualTo(new[] { 100, -1, 102 }));
        }

        [Test]
        public void TryReadManifestAck_FewerIdsThanCount_Fails()
        {
            byte[] data = Bytes.Packet(PacketType.PropManifestAck).U16(3).I32(100).I32(101);
            var ids = new List<int>();

            Assert.That(NetPropReceiver.TryReadManifestAck(data, ids), Is.False);
            Assert.That(ids, Is.Empty);
        }

        [Test]
        public void TryReadGrant_ReadsFields()
        {
            byte[] data = Bytes.Packet(PacketType.PropGrant).I32(42).Str("holder").U8(0);

            Assert.That(NetPropReceiver.TryReadGrant(data, out var grant), Is.True);
            Assert.That(grant.PropId, Is.EqualTo(42));
            Assert.That(grant.Holder, Is.EqualTo("holder"));
        }

        [Test]
        public void TryReadGrant_Truncated_Fails()
        {
            byte[] data = Bytes.Packet(PacketType.PropGrant).I32(42).U8(6).Raw((byte)'h');

            Assert.That(NetPropReceiver.TryReadGrant(data, out _), Is.False);
        }

        [TestCase("owner", (byte)0, false)]
        [TestCase("owner", (byte)1, true)]
        [TestCase("", (byte)0, true)]
        public void TryReadReleaseEvent_DetectsFree(string previousOwner, byte flags, bool expectedFree)
        {
            byte[] data = Bytes.Packet(PacketType.PropReleaseEvent)
                .I32(7).Str(previousOwner).V3(Vector3.one).V3(Vector3.up).U8(flags);

            Assert.That(NetPropReceiver.TryReadReleaseEvent(data, out var release), Is.True);
            Assert.That(release.PropId, Is.EqualTo(7));
            Assert.That(release.PreviousOwner, Is.EqualTo(previousOwner));
            Assert.That(release.IsFree, Is.EqualTo(expectedFree));
        }

        [Test]
        public void TryReadReleaseEvent_WithoutFlags_Fails()
        {
            byte[] data = Bytes.Packet(PacketType.PropReleaseEvent).I32(7).Str("owner").V3(Vector3.one).V3(Vector3.up);

            Assert.That(NetPropReceiver.TryReadReleaseEvent(data, out _), Is.False);
        }

        [Test]
        public void TryReadSnapshot_ReadsEntries()
        {
            byte[] data = Bytes.Packet(PacketType.PropSnapshot).U16(2)
                .I32(1).Str("someone").Bool(true)
                .I32(2).Str("").Bool(false);
            var entries = new List<PropSnapshotEntry>();

            Assert.That(NetPropReceiver.TryReadSnapshot(data, entries), Is.True);
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0].PropId, Is.EqualTo(1));
            Assert.That(entries[0].Held, Is.True);
            Assert.That(entries[1].PropId, Is.EqualTo(2));
            Assert.That(entries[1].Held, Is.False);
        }

        [Test]
        public void TryReadSnapshot_Truncated_KeepsCompleteEntries()
        {
            byte[] data = Bytes.Packet(PacketType.PropSnapshot).U16(2)
                .I32(1).Str("someone").Bool(true)
                .I32(2).Str("x");
            var entries = new List<PropSnapshotEntry>();

            Assert.That(NetPropReceiver.TryReadSnapshot(data, entries), Is.False);
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].PropId, Is.EqualTo(1));
        }
    }
}
