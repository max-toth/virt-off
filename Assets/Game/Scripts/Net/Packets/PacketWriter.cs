using System;
using System.Buffers.Binary;
using System.Text;
using UnityEngine;

namespace Game.Scripts.Net.Packets
{
    /// <summary>Общий переиспользуемый буфер исходящего пакета. Только главный поток: INetTransport.Send отправляет синхронно.</summary>
    public sealed class PacketWriter
    {
        /// <summary>Длина строки пишется одним байтом.</summary>
        public const int MaxStringBytes = byte.MaxValue;

        private const int InitialCapacity = 512;

        private byte[] _buffer = new byte[InitialCapacity];
        private int _length;

        public static PacketWriter Shared { get; } = new();

        public void Begin(byte packetType)
        {
            _length = 0;
            WriteByte(packetType);
        }

        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            _buffer[_length++] = value;
        }

        public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteUInt16(int value)
        {
            EnsureCapacity(2);
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_length, 2), (ushort)value);
            _length += 2;
        }

        public void WriteInt32(int value)
        {
            EnsureCapacity(4);
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_length, 4), value);
            _length += 4;
        }

        public void WriteSingle(float value) => WriteInt32(BitConverter.SingleToInt32Bits(value));

        public void WriteVector3(Vector3 value)
        {
            WriteSingle(value.x);
            WriteSingle(value.y);
            WriteSingle(value.z);
        }

        public void WriteQuaternion(Quaternion value)
        {
            WriteSingle(value.x);
            WriteSingle(value.y);
            WriteSingle(value.z);
            WriteSingle(value.w);
        }

        /// <summary>Пишет строку как [length:1][UTF-8]. Длиннее MaxStringBytes байт — обрезается по границе символа.</summary>
        public void WriteString(string value)
        {
            value ??= string.Empty;
            var byteCount = Encoding.UTF8.GetByteCount(value);
            EnsureCapacity(1 + byteCount);

            var start = _length + 1;
            Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, start);
            if (byteCount > MaxStringBytes)
            {
                byteCount = MaxStringBytes;
                // Первый отброшенный байт — продолжение символа (10xxxxxx): символ целиком уходит за обрезку.
                while (byteCount > 0 && (_buffer[start + byteCount] & 0xC0) == 0x80) byteCount--;
            }

            _buffer[_length] = (byte)byteCount;
            _length = start + byteCount;
        }

        public void WriteBytes(byte[] source, int offset, int count)
        {
            EnsureCapacity(count);
            Buffer.BlockCopy(source, offset, _buffer, _length, count);
            _length += count;
        }

        public void SendTo(INetTransport transport) => transport.Send(_buffer, _length);

        private void EnsureCapacity(int count)
        {
            if (_length + count > _buffer.Length)
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _length + count));
        }
    }
}
