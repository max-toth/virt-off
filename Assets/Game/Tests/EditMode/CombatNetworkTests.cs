using System.Collections.Generic;
using Game.Scripts.Audio.Models;
using Game.Scripts.Combat;
using Game.Scripts.Combat.Models;
using Game.Scripts.Net.Packets;
using Game.Tests.EditMode.Support;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class CombatNetworkTests
    {
        private FakeTransport _transport;
        private List<HitMessage> _hits;

        [SetUp]
        public void SetUp()
        {
            _transport = FakeTransport.Connected();
            CombatNetwork.Initialize(_transport);
            _hits = new List<HitMessage>();
            CombatNetwork.HitReceived += _hits.Add;
        }

        [TearDown]
        public void TearDown() => CombatNetwork.Shutdown();

        [Test]
        public void SendHit_MatchesFormat()
        {
            var point = new Vector3(1f, 2f, 3f);
            var impulse = new Vector3(-4f, 5f, 0.5f);

            CombatNetwork.SendHit(AttackKind.Slap, "target", point, impulse, true);

            var expected = Bytes.Packet(PacketType.Attack)
                .Str(FakeTransport.DefaultId).U8((byte)AttackKind.Slap).Str("target").V3(point).V3(impulse).Bool(true);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void SendHit_WithoutTarget_WritesEmptyString()
        {
            CombatNetwork.SendHit(AttackKind.Punch, null, Vector3.zero, Vector3.zero);

            Assert.That(CombatNetwork.TryReadHit(_transport.LastSent, out var hit), Is.True);
            Assert.That(hit.HasTarget, Is.False);
            Assert.That(hit.HitObject, Is.False);
        }

        [Test]
        public void SendSound_MatchesFormat()
        {
            var position = new Vector3(7f, 8f, 9f);

            CombatNetwork.SendSound(Sfx.Impact, position, 0.75f);

            var expected = Bytes.Packet(PacketType.Sound)
                .Str(FakeTransport.DefaultId).U8((byte)Sfx.Impact).V3(position).F32(0.75f);
            Assert.That(_transport.LastSent, Is.EqualTo((byte[])expected));
        }

        [Test]
        public void Send_BeforeIdAssigned_SendsNothing()
        {
            var transport = new FakeTransport();
            CombatNetwork.Initialize(transport);

            CombatNetwork.SendHit(AttackKind.Punch, "x", Vector3.zero, Vector3.zero);
            CombatNetwork.SendSound(Sfx.Miss, Vector3.zero, 1f);

            Assert.That(transport.Sent, Is.Empty);
        }

        [Test]
        public void TryReadHit_ReadsAllFields()
        {
            var data = Bytes.Packet(PacketType.Attack)
                .Str("attacker").U8((byte)AttackKind.Prop).Str("victim").V3(Vector3.one).V3(Vector3.up).Bool(false)
                .ToArray();

            Assert.That(CombatNetwork.TryReadHit(data, out var hit), Is.True);
            Assert.That(hit.Attacker, Is.EqualTo("attacker"));
            Assert.That(hit.Kind, Is.EqualTo(AttackKind.Prop));
            Assert.That(hit.Target, Is.EqualTo("victim"));
            Assert.That(hit.Point, Is.EqualTo(Vector3.one));
            Assert.That(hit.Impulse, Is.EqualTo(Vector3.up));
            Assert.That(hit.HitObject, Is.False);
        }

        [Test]
        public void TryReadHit_Truncated_Fails()
        {
            CombatNetwork.SendHit(AttackKind.Punch, "victim", Vector3.one, Vector3.one);
            var data = _transport.LastSent;

            Assert.That(CombatNetwork.TryReadHit(Bytes.Truncated(data), out _), Is.False);
        }

        [Test]
        public void TryReadSound_ReadsAllFields()
        {
            CombatNetwork.SendSound(Sfx.Whoosh, new Vector3(1f, 2f, 3f), 0.5f);

            Assert.That(CombatNetwork.TryReadSound(_transport.LastSent, out var sound), Is.True);
            Assert.That(sound.Sender, Is.EqualTo(FakeTransport.DefaultId));
            Assert.That(sound.Sfx, Is.EqualTo(Sfx.Whoosh));
            Assert.That(sound.Position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(sound.Volume, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryReadSound_Truncated_Fails()
        {
            var data = Bytes.Packet(PacketType.Sound).Str("other").U8(0).V3(Vector3.zero).ToArray();

            Assert.That(CombatNetwork.TryReadSound(data, out _), Is.False);
        }

        [Test]
        public void IncomingHit_FromOtherPlayer_RaisesEvent()
        {
            _transport.Receive(HitFrom("other"));

            Assert.That(_hits, Has.Count.EqualTo(1));
            Assert.That(_hits[0].Attacker, Is.EqualTo("other"));
        }

        [Test]
        public void IncomingHit_Own_IsIgnored()
        {
            _transport.Receive(HitFrom(FakeTransport.DefaultId));

            Assert.That(_hits, Is.Empty);
        }

        [Test]
        public void IncomingHit_Malformed_IsIgnored()
        {
            _transport.Receive(new[] { PacketType.Attack, (byte)5 });
            _transport.Receive(new byte[0]);

            Assert.That(_hits, Is.Empty);
        }

        [Test]
        public void Shutdown_Unsubscribes()
        {
            CombatNetwork.Shutdown();

            _transport.Receive(HitFrom("other"));
            CombatNetwork.SendSound(Sfx.Punch, Vector3.zero, 1f);

            Assert.That(_hits, Is.Empty);
            Assert.That(_transport.Sent, Is.Empty);
        }

        private static byte[] HitFrom(string attacker) =>
            Bytes.Packet(PacketType.Attack)
                .Str(attacker).U8((byte)AttackKind.Punch).Str(FakeTransport.DefaultId).V3(Vector3.zero).V3(Vector3.forward).Bool(false);
    }
}
