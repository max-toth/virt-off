namespace Game.Scripts.Voice.Models
{
    /// <summary>Данные Opus без копирования: ссылка на принятый пакет и диапазон внутри него.</summary>
    public readonly struct VoicePacket
    {
        public VoicePacket(string sender, ushort sequence, byte[] data, int offset, int count)
        {
            Sender = sender;
            Sequence = sequence;
            Data = data;
            Offset = offset;
            Count = count;
        }

        public string Sender { get; }

        /// <summary>Номер кадра у отправителя: по пропускам видно потери.</summary>
        public ushort Sequence { get; }
        public byte[] Data { get; }
        public int Offset { get; }
        public int Count { get; }
    }
}
