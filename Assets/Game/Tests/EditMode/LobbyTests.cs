using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Tests.EditMode.Support;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class LobbyTests
    {
        private FakeTransport _transport;
        private List<string> _joined;

        [SetUp]
        public void SetUp()
        {
            Lobby.Shutdown();
            _transport = FakeTransport.Connected();
            Lobby.Initialize(_transport);
            _joined = new List<string>();
            Lobby.PlayerJoined += _joined.Add;
        }

        [TearDown]
        public void TearDown() => Lobby.Shutdown();

        [Test]
        public void VoiceOrPosition_FromNewPlayer_Joins()
        {
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("a").Raw(1));
            _transport.Receive(Bytes.Packet(PacketType.Position).Str("b").V3(Vector3.zero).F32(0f).U8(0));

            Assert.That(_joined, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(Lobby.Players.Select(player => player.Id), Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void RepeatedPackets_JoinOnce()
        {
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("a").Raw(1));
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("a").Raw(2));

            Assert.That(_joined, Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void OwnPackets_AreIgnored()
        {
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str(FakeTransport.DefaultId).Raw(1));

            Assert.That(_joined, Is.Empty);
            Assert.That(Lobby.Players, Is.Empty);
        }

        [Test]
        public void OtherPacketTypes_DoNotJoin()
        {
            _transport.Receive(Bytes.Packet(PacketType.Sound).Str("a").U8(0).V3(Vector3.zero).F32(1f));
            _transport.Receive(new[] { PacketType.Voice });

            Assert.That(_joined, Is.Empty);
        }

        [Test]
        public void Mute_IsPerPlayer()
        {
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("a").Raw(1));
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("b").Raw(1));

            Lobby.Players.Single(player => player.Id == "a").Muted = true;

            Assert.That(Lobby.IsMuted("a"), Is.True);
            Assert.That(Lobby.IsMuted("b"), Is.False);
            Assert.That(Lobby.IsMuted("unknown"), Is.False);
        }

        [Test]
        public void Shutdown_ForgetsPlayersAndUnsubscribes()
        {
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("a").Raw(1));

            Lobby.Shutdown();
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("b").Raw(1));

            Assert.That(Lobby.Players, Is.Empty);
            Assert.That(_joined, Is.EqualTo(new[] { "a" }));
        }
    }
}
