using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Tests.EditMode.Support
{
    /// <summary>Ожидаемые байты пакета по описанию формата, без PacketWriter: little-endian, строки — [length:1][UTF-8].</summary>
    public sealed class Bytes
    {
        private readonly List<byte> _data = new();

        public static Bytes Packet(byte type) => new Bytes().U8(type);

        public Bytes U8(byte value)
        {
            _data.Add(value);
            return this;
        }

        public Bytes Bool(bool value) => U8(value ? (byte)1 : (byte)0);

        public Bytes U16(int value)
        {
            Span<byte> span = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)value);
            return Raw(span.ToArray());
        }

        public Bytes I32(int value)
        {
            Span<byte> span = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
            return Raw(span.ToArray());
        }

        public Bytes F32(float value) => I32(BitConverter.SingleToInt32Bits(value));

        public Bytes V3(Vector3 value) => F32(value.x).F32(value.y).F32(value.z);

        public Bytes Quat(Quaternion value) => F32(value.x).F32(value.y).F32(value.z).F32(value.w);

        public Bytes Str(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            return U8((byte)bytes.Length).Raw(bytes);
        }

        public Bytes Raw(params byte[] bytes)
        {
            _data.AddRange(bytes);
            return this;
        }

        public byte[] ToArray() => _data.ToArray();

        /// <summary>Пакет без последних count байт.</summary>
        public static byte[] Truncated(byte[] data, int count = 1) => data.AsSpan(0, data.Length - count).ToArray();

        public static implicit operator byte[](Bytes bytes) => bytes.ToArray();
    }
}
