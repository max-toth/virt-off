using System;
using System.Buffers.Binary;
using System.Text;
using UnityEngine;

namespace Game.Scripts.Net.Packets
{
    /// <summary>Чтение пакета с проверкой границ; по умолчанию начинает после байта типа.</summary>
    public struct PacketReader
    {
        private readonly byte[] _data;

        public PacketReader(byte[] data, int offset = 1)
        {
            _data = data;
            Position = offset;
            Failed = false;
        }

        /// <summary>true, если данных не хватило; дальнейшие чтения возвращают значения по умолчанию.</summary>
        public bool Failed { get; private set; }

        public int Position { get; private set; }

        public int Remaining => Failed ? 0 : _data.Length - Position;

        public byte ReadByte() => TryAdvance(1, out var start) ? _data[start] : (byte)0;

        public bool ReadBool() => ReadByte() != 0;

        public int ReadUInt16() =>
            TryAdvance(2, out var start) ? BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(start, 2)) : 0;

        public int ReadInt32() =>
            TryAdvance(4, out var start) ? BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(start, 4)) : 0;

        public float ReadSingle() => BitConverter.Int32BitsToSingle(ReadInt32());

        public Vector3 ReadVector3() => new(ReadSingle(), ReadSingle(), ReadSingle());

        public Quaternion ReadQuaternion() => new(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        public string ReadString()
        {
            var length = ReadByte();
            return TryAdvance(length, out var start) ? Encoding.UTF8.GetString(_data, start, length) : null;
        }

        public void SkipString() => Skip(ReadByte());

        public void Skip(int count) => TryAdvance(count, out _);

        private bool TryAdvance(int count, out int start)
        {
            start = Position;
            if (Failed || Position + count > _data.Length)
            {
                Failed = true;
                return false;
            }

            Position += count;
            return true;
        }
    }
}
