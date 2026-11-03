using Concentus.Enums;
using Concentus.Structs;
using UnityEngine;

namespace Game.Scripts.Voice.Capture
{
    public sealed class VoiceEncoder
    {
        private const int Bitrate = 24000;
        private const int Complexity = 5;
        private const int MaxPacketSize = 4000;

        private readonly OpusEncoder _encoder = new(VoiceFormat.SampleRate, VoiceFormat.Channels, OpusApplication.OPUS_APPLICATION_VOIP)
        {
            Bitrate = Bitrate,
            Complexity = Complexity
        };

        private readonly short[] _pcm = new short[VoiceFormat.FrameSize];

        public byte[] Output { get; } = new byte[MaxPacketSize];

        public int Encode(float[] frame, float gain)
        {
            for (var i = 0; i < _pcm.Length; i++)
                _pcm[i] = (short)(Mathf.Clamp(frame[i] * gain, -1f, 1f) * short.MaxValue);

            return _encoder.Encode(_pcm, 0, _pcm.Length, Output, 0, Output.Length);
        }
    }
}
