using System.Linq;
using Game.Scripts.Net.Packets;
using Game.Tests.EditMode.Support;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class PacketWriterReaderTests
    {
        private const byte AnyType = 0x42;

        private PacketWriter _writer;
        private FakeTransport _transport;

        [SetUp]
        public void SetUp()
        {
            _writer = new PacketWriter();
            _transport = FakeTransport.Connected();
            _writer.Begin(AnyType);
        }

        [Test]
        public void Begin_WritesTypeFirst()
        {
            Assert.That(Send(), Is.EqualTo(new[] { AnyType }));
        }

        [Test]
        public void Begin_DiscardsPreviousPacket()
        {
            _writer.WriteInt32(123);
            _writer.Begin(AnyType);
            _writer.WriteByte(7);

            Assert.That(Send(), Is.EqualTo(new byte[] { AnyType, 7 }));
        }

        [Test]
        public void Numbers_AreLittleEndian()
        {
            _writer.WriteUInt16(0x1234);
            _writer.WriteInt32(0x0A0B0C0D);

            Assert.That(Send(), Is.EqualTo(new byte[] { AnyType, 0x34, 0x12, 0x0D, 0x0C, 0x0B, 0x0A }));
        }

        [Test]
        public void Numbers_RoundTrip()
        {
            _writer.WriteByte(255);
            _writer.WriteBool(true);
            _writer.WriteBool(false);
            _writer.WriteUInt16(65535);
            _writer.WriteInt32(int.MinValue);
            _writer.WriteInt32(-1);
            _writer.WriteSingle(-123.456f);
            _writer.WriteSingle(float.PositiveInfinity);

            var reader = new PacketReader(Send());
            Assert.That(reader.ReadByte(), Is.EqualTo(255));
            Assert.That(reader.ReadBool(), Is.True);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadUInt16(), Is.EqualTo(65535));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MinValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(-1));
            Assert.That(reader.ReadSingle(), Is.EqualTo(-123.456f));
            Assert.That(reader.ReadSingle(), Is.EqualTo(float.PositiveInfinity));
            Assert.That(reader.Failed, Is.False);
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void VectorAndQuaternion_RoundTripComponentwise()
        {
            var vector = new Vector3(1.5f, -2.25f, 1e6f);
            var rotation = new Quaternion(0.1f, 0.2f, 0.3f, 0.9f);
            _writer.WriteVector3(vector);
            _writer.WriteQuaternion(rotation);

            var data = Send();
            Assert.That(data, Is.EqualTo((byte[])Bytes.Packet(AnyType).V3(vector).Quat(rotation)));

            var reader = new PacketReader(data);
            var readVector = reader.ReadVector3();
            var readRotation = reader.ReadQuaternion();
            Assert.That(new[] { readVector.x, readVector.y, readVector.z }, Is.EqualTo(new[] { vector.x, vector.y, vector.z }));
            Assert.That(new[] { readRotation.x, readRotation.y, readRotation.z, readRotation.w },
                Is.EqualTo(new[] { rotation.x, rotation.y, rotation.z, rotation.w }));
        }

        [TestCase("")]
        [TestCase("player-1")]
        [TestCase("Привет, 世界 🎮")]
        public void String_RoundTrip(string value)
        {
            _writer.WriteString(value);

            var data = Send();
            Assert.That(data, Is.EqualTo((byte[])Bytes.Packet(AnyType).Str(value)));

            var reader = new PacketReader(data);
            Assert.That(reader.ReadString(), Is.EqualTo(value));
            Assert.That(reader.Failed, Is.False);
        }

        [Test]
        public void String_LengthIsUtf8ByteCount()
        {
            _writer.WriteString("ж");

            Assert.That(Send()[1], Is.EqualTo(2));
        }

        [Test]
        public void String_OfMaxLength_IsKeptWhole()
        {
            var value = new string('a', PacketWriter.MaxStringBytes);
            _writer.WriteString(value);

            Assert.That(new PacketReader(Send()).ReadString(), Is.EqualTo(value));
        }

        [Test]
        public void String_TooLong_IsTruncatedAndPacketStaysReadable()
        {
            _writer.WriteString(new string('a', 300));
            _writer.WriteByte(77);

            var reader = new PacketReader(Send());
            Assert.That(reader.ReadString(), Is.EqualTo(new string('a', PacketWriter.MaxStringBytes)));
            Assert.That(reader.ReadByte(), Is.EqualTo(77));
            Assert.That(reader.Failed, Is.False);
        }

        [Test]
        public void String_TooLong_IsTruncatedAtCharBoundary()
        {
            // 'ж' — 2 байта: 128 символов = 256 байт, целиком влезают только 127.
            _writer.WriteString(new string('ж', 128));

            var data = Send();
            Assert.That(data[1], Is.EqualTo(254));
            Assert.That(new PacketReader(data).ReadString(), Is.EqualTo(new string('ж', 127)));
        }

        [Test]
        public void String_TooLong_DoesNotSplitSurrogatePair()
        {
            // 'a' + 63 эмодзи по 4 байта = 253 байта, 64-е эмодзи уже не влезает.
            var value = "a" + string.Concat(System.Linq.Enumerable.Repeat("🎮", 64));
            _writer.WriteString(value);

            var read = new PacketReader(Send()).ReadString();
            Assert.That(read, Is.EqualTo("a" + string.Concat(System.Linq.Enumerable.Repeat("🎮", 63))));
        }

        [Test]
        public void String_NullIsWrittenAsEmpty()
        {
            _writer.WriteString(null);

            Assert.That(new PacketReader(Send()).ReadString(), Is.Empty);
        }

        [Test]
        public void WriteBytes_CopiesRange()
        {
            _writer.WriteBytes(new byte[] { 1, 2, 3, 4, 5 }, 1, 3);

            Assert.That(Send(), Is.EqualTo(new byte[] { AnyType, 2, 3, 4 }));
        }

        [Test]
        public void Buffer_GrowsBeyondInitialCapacity()
        {
            var payload = Enumerable.Range(0, 2000).Select(i => (byte)i).ToArray();
            _writer.WriteBytes(payload, 0, payload.Length);
            _writer.WriteInt32(42);

            var data = Send();
            Assert.That(data.Length, Is.EqualTo(1 + payload.Length + 4));
            Assert.That(data.Skip(1).Take(payload.Length), Is.EqualTo(payload));

            var reader = new PacketReader(data, 1 + payload.Length);
            Assert.That(reader.ReadInt32(), Is.EqualTo(42));
        }

        [Test]
        public void Reader_StartsAfterTypeByDefault()
        {
            var reader = new PacketReader(new byte[] { AnyType, 9 });

            Assert.That(reader.Position, Is.EqualTo(1));
            Assert.That(reader.ReadByte(), Is.EqualTo(9));
        }

        [Test]
        public void Reader_FailsOnTruncatedNumber()
        {
            var reader = new PacketReader(new byte[] { AnyType, 1, 2, 3 });

            Assert.That(reader.ReadInt32(), Is.Zero);
            Assert.That(reader.Failed, Is.True);
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void Reader_StaysFailedAfterFirstError()
        {
            var reader = new PacketReader(new byte[] { AnyType, 5 });

            reader.ReadUInt16();
            Assert.That(reader.ReadByte(), Is.Zero, "байт есть, но после ошибки чтение не продолжается");
            Assert.That(reader.Failed, Is.True);
        }

        [Test]
        public void Reader_FailsOnStringLongerThanData()
        {
            var data = Bytes.Packet(AnyType).U8(10).Raw(1, 2, 3).ToArray();
            var reader = new PacketReader(data);

            Assert.That(reader.ReadString(), Is.Null);
            Assert.That(reader.Failed, Is.True);
        }

        [Test]
        public void Reader_FailsOnEmptyPacket()
        {
            var reader = new PacketReader(new byte[] { AnyType });

            reader.ReadString();
            Assert.That(reader.Failed, Is.True);
        }

        [Test]
        public void Reader_SkipStringAndSkip()
        {
            var data = Bytes.Packet(AnyType).Str("skip me").Raw(0, 0, 0).U8(77).ToArray();
            var reader = new PacketReader(data);

            reader.SkipString();
            reader.Skip(3);
            Assert.That(reader.ReadByte(), Is.EqualTo(77));
            Assert.That(reader.Failed, Is.False);
        }

        [Test]
        public void Reader_SkipPastEndFails()
        {
            var reader = new PacketReader(new byte[] { AnyType, 1 });

            reader.Skip(2);
            Assert.That(reader.Failed, Is.True);
        }

        private byte[] Send()
        {
            _writer.SendTo(_transport);
            return _transport.LastSent;
        }
    }
}
