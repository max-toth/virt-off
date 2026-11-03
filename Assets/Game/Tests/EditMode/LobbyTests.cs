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
        private FakeClock _clock;
        private Lobby _lobby;
        private List<string> _joined;
        private List<string> _left;

        [SetUp]
        public void SetUp()
        {
            _transport = FakeTransport.Connected();
            _clock = new FakeClock();
            _lobby = new Lobby(_transport.Router, _clock);
            _lobby.Initialize();
            _joined = new List<string>();
            _left = new List<string>();
            _lobby.PlayerJoined += _joined.Add;
            _lobby.PlayerLeft += _left.Add;
        }

        [TearDown]
        public void TearDown() => _lobby.Dispose();

        [Test]
        public void VoiceOrPosition_FromNewPlayer_Joins()
        {
            _transport.Receive(Voice("a"));
            _transport.Receive(Bytes.Packet(PacketType.Position).Str("b").V3(Vector3.zero).F32(0f).U8(0));

            Assert.That(_joined, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(_lobby.Players.Select(player => player.Id), Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void RepeatedPackets_JoinOnce()
        {
            _transport.Receive(Voice("a"));
            _transport.Receive(Voice("a"));

            Assert.That(_joined, Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void OwnPackets_AreIgnored()
        {
            _transport.Receive(Voice(FakeTransport.DefaultId));

            Assert.That(_joined, Is.Empty);
            Assert.That(_lobby.Players, Is.Empty);
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
            _transport.Receive(Voice("a"));
            _transport.Receive(Voice("b"));

            _lobby.Players.Single(player => player.Id == "a").Muted = true;

            Assert.That(_lobby.IsMuted("a"), Is.True);
            Assert.That(_lobby.IsMuted("b"), Is.False);
            Assert.That(_lobby.IsMuted("unknown"), Is.False);
        }

        [Test]
        public void PlayerLeft_RemovesPlayerAtOnce()
        {
            _transport.Receive(Voice("a"));

            _transport.Receive(Bytes.Packet(PacketType.PlayerLeft).Str("a"));

            Assert.That(_left, Is.EqualTo(new[] { "a" }));
            Assert.That(_lobby.Players, Is.Empty);
        }

        [Test]
        public void PlayerLeft_ForUnknownPlayer_IsIgnored()
        {
            _transport.Receive(Bytes.Packet(PacketType.PlayerLeft).Str("ghost"));

            Assert.That(_left, Is.Empty);
        }

        [Test]
        public void SilentPlayer_ExpiresAfterTimeout()
        {
            _transport.Receive(Voice("a"));

            _clock.UnscaledTime = 4f;
            _lobby.RemoveExpired();
            Assert.That(_left, Is.Empty);

            _clock.UnscaledTime = 10f;
            _lobby.RemoveExpired();
            Assert.That(_left, Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void Speaking_HoldsForAMoment()
        {
            _transport.Receive(Voice("a"));
            var player = _lobby.Players.Single();

            _lobby.MarkSpeaking("a");
            Assert.That(_lobby.IsSpeaking(player), Is.True);

            _clock.UnscaledTime = 1f;
            Assert.That(_lobby.IsSpeaking(player), Is.False);
        }

        [Test]
        public void Dispose_Unsubscribes()
        {
            _lobby.Dispose();

            _transport.Receive(Voice("b"));

            Assert.That(_joined, Is.Empty);
        }

        private static byte[] Voice(string sender) => Bytes.Packet(PacketType.Voice).Str(sender).U16(0).Raw(1);
    }
}
