namespace Game.Scripts.Voice.Models
{
    // Данные Opus без копирования: ссылка на принятый пакет и диапазон внутри него.
    public readonly struct VoicePacket
    {
        public VoicePacket(string sender, byte[] data, int offset, int count)
        {
            Sender = sender;
            Data = data;
            Offset = offset;
            Count = count;
        }

        public string Sender { get; }
        public byte[] Data { get; }
        public int Offset { get; }
        public int Count { get; }
    }
}
