using System;

namespace Game.Scripts.Voice.Capture
{
    /// <summary>Пропускает кадры с речью: тишина не отправляется. После речи держится открытым ещё немного,
    /// чтобы не обрезать затухающие окончания слов.</summary>
    public sealed class VoiceActivityGate
    {
        private readonly float _threshold;
        private readonly int _hangoverFrames;
        private int _framesLeft;

        /// <param name="threshold">Пиковая амплитуда кадра с учётом усиления, 0..1, с которой кадр считается речью.</param>
        /// <param name="hangoverFrames">Сколько кадров после речи ещё отправляется.</param>
        public VoiceActivityGate(float threshold, int hangoverFrames)
        {
            _threshold = threshold;
            _hangoverFrames = hangoverFrames;
        }

        /// <summary>Последний кадр был отправлен.</summary>
        public bool IsOpen { get; private set; }

        public bool ShouldSend(float[] frame, float gain)
        {
            var peak = 0f;
            foreach (var sample in frame) peak = Math.Max(peak, Math.Abs(sample));

            if (peak * gain >= _threshold) _framesLeft = _hangoverFrames + 1;

            IsOpen = _framesLeft > 0;
            if (IsOpen) _framesLeft--;
            return IsOpen;
        }

        public void Close()
        {
            _framesLeft = 0;
            IsOpen = false;
        }
    }
}
