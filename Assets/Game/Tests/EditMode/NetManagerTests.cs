using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    // Без сокета: пакеты кладутся в очередь так же, как это делает поток приёма.
    public sealed class NetManagerTests
    {
        private NetManager _manager;

        [SetUp]
        public void SetUp() => _manager = new NetManager();

        [TearDown]
        public void TearDown() => _manager.Dispose();

        [Test]
        public void IdAssigned_SetsLocalIdAndConnects()
        {
            var assigned = 0;
            _manager.IdAssigned += () => assigned++;

            _manager.Enqueue(IdPacket("42"));
            Assert.That(_manager.IsConnected, Is.False, "до ProcessIncoming событий нет");

            _manager.ProcessIncoming();

            Assert.That(assigned, Is.EqualTo(1));
            Assert.That(_manager.IsConnected, Is.True);
            Assert.That(_manager.LocalId, Is.EqualTo("42"));
        }

        [Test]
        public void RepeatedIdAssigned_UpdatesIdButRaisesEventOnce()
        {
            var assigned = 0;
            _manager.IdAssigned += () => assigned++;

            _manager.Enqueue(IdPacket("1"));
            _manager.Enqueue(IdPacket("2"));
            _manager.ProcessIncoming();

            Assert.That(assigned, Is.EqualTo(1));
            Assert.That(_manager.LocalId, Is.EqualTo("2"));
        }

        [Test]
        public void IdAssigned_IsNotForwardedAsPacket()
        {
            var packets = 0;
            _manager.PacketReceived += (_, _) => packets++;

            _manager.Enqueue(IdPacket("42"));
            _manager.ProcessIncoming();

            Assert.That(packets, Is.Zero);
        }

        [Test]
        public void Packets_AreForwardedInOrderWithLocalId()
        {
            var received = new List<(string, byte)>();
            _manager.PacketReceived += (id, data) => received.Add((id, data[0]));

            _manager.Enqueue(IdPacket("7"));
            _manager.Enqueue(new[] { PacketType.Voice });
            _manager.Enqueue(new[] { PacketType.Position });
            _manager.ProcessIncoming();

            Assert.That(received, Is.EqualTo(new[] { ("7", PacketType.Voice), ("7", PacketType.Position) }));
        }

        [Test]
        public void HandlerException_DoesNotStopOtherPackets()
        {
            var received = 0;
            _manager.PacketReceived += (_, data) =>
            {
                if (data[0] == PacketType.Voice) throw new InvalidOperationException("broken handler");
                received++;
            };

            _manager.Enqueue(new[] { PacketType.Voice });
            _manager.Enqueue(new[] { PacketType.Position });
            LogAssert.Expect(LogType.Exception, new Regex("broken handler"));
            _manager.ProcessIncoming();

            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void Send_BeforeStart_IsIgnored()
        {
            Assert.DoesNotThrow(() => _manager.Send(new byte[] { 1 }, 1));
        }

        private static byte[] IdPacket(string id)
        {
            var bytes = Encoding.UTF8.GetBytes(id);
            var packet = new byte[bytes.Length + 1];
            packet[0] = PacketType.IdAssigned;
            bytes.CopyTo(packet, 1);
            return packet;
        }
    }
}
