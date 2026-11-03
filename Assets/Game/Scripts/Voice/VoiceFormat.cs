namespace Game.Scripts.Voice
{
    public static class VoiceFormat
    {
        public const int SampleRate = 16000;
        public const int Channels = 1;

        // 40 мс при 16 кГц — один пакет Opus.
        public const int FrameSize = 640;
    }
}
