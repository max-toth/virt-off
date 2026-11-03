using System.Collections.Generic;
using Game.Scripts.Bootstrap;
using Game.Scripts.Combat;
using Game.Scripts.Combat.Models;
using Game.Scripts.Combat.Props;
using Game.Scripts.Common;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Player.Sync;
using Game.Scripts.Voice;
using Game.Tests.EditMode.Support;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace Game.Tests.EditMode
{
    // Протокол собирается так же, как в GameLifetimeScope, но поверх фейкового транспорта.
    public sealed class NetworkInstallerTests
    {
        private FakeTransport _transport;
        private IObjectResolver _container;

        [SetUp]
        public void SetUp()
        {
            _transport = FakeTransport.Connected();

            var builder = new ContainerBuilder();
            builder.RegisterInstance<INetTransport>(_transport);
            builder.RegisterInstance<IClock>(new FakeClock());
            new NetworkInstaller().Install(builder);
            _container = builder.Build();
        }

        [TearDown]
        public void TearDown() => _container.Dispose();

        [Test]
        public void AllServicesResolve()
        {
            Assert.That(_container.Resolve<Lobby>(), Is.Not.Null);
            Assert.That(_container.Resolve<CombatNetwork>(), Is.Not.Null);
            Assert.That(_container.Resolve<PositionNetwork>(), Is.Not.Null);
            Assert.That(_container.Resolve<VoiceNetwork>(), Is.Not.Null);
            Assert.That(_container.Resolve<NetPropSender>(), Is.Not.Null);
        }

        [Test]
        public void ServicesAreSingletons()
        {
            Assert.That(_container.Resolve<PacketWriter>(), Is.SameAs(_container.Resolve<PacketWriter>()));
            Assert.That(_container.Resolve<Lobby>(), Is.SameAs(_container.Resolve<Lobby>()));
        }

        [Test]
        public void EntryPoints_AreSubscribedAfterBuild()
        {
            var lobby = _container.Resolve<Lobby>();
            var hits = new List<HitMessage>();
            _container.Resolve<CombatNetwork>().HitReceived += hits.Add;

            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("other").U16(0).Raw(1));
            _transport.Receive(Bytes.Packet(PacketType.Attack)
                .Str("other").U8(0).Str(FakeTransport.DefaultId).V3(Vector3.zero).V3(Vector3.zero).Bool(false));

            Assert.That(lobby.Players, Has.Count.EqualTo(1));
            Assert.That(hits, Has.Count.EqualTo(1));
        }

        [Test]
        public void Senders_ShareTransport()
        {
            _container.Resolve<CombatNetwork>().SendSound(0, Vector3.zero, 1f);
            _container.Resolve<NetPropSender>().SendPickupRequest(3);

            Assert.That(_transport.Sent, Has.Count.EqualTo(2));
        }

        [Test]
        public void ContainerDispose_UnsubscribesFromTransport()
        {
            var lobby = _container.Resolve<Lobby>();

            _container.Dispose();
            _transport.Receive(Bytes.Packet(PacketType.Voice).Str("other").U16(0).Raw(1));

            Assert.That(lobby.Players, Is.Empty);
        }
    }
}
