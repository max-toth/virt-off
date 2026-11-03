using System;

namespace Game.Scripts.Voice.Playback
{
    /// <summary>Кольцевой буфер голоса между декодером (главный поток) и аудиопотоком: один писатель, один читатель.
    /// После опустошения снова набирает запас, при задержке сверх предела отбрасывает новые кадры.
    /// Аудиопоток может забирать данные крупными блоками, поэтому запас и предел не меньше размера блока.</summary>
    public sealed class VoiceJitterBuffer
    {
        private const float SampleScale = 1f / short.MaxValue;

        private readonly float[] _samples;
        private readonly int _targetDelay;
        private readonly int _maxDelay;

        // Позицию записи двигает только главный поток, чтения — только аудиопоток.
        private volatile int _writePosition;
        private volatile int _readPosition;
        private volatile bool _starved = true;
        private volatile int _readBlock;

        /// <param name="capacity">Размер кольца в сэмплах; должен быть больше maxDelay.</param>
        /// <param name="targetDelay">Запас перед воспроизведением после опустошения, сэмплы.</param>
        /// <param name="maxDelay">Предел накопленной задержки, сэмплы.</param>
        public VoiceJitterBuffer(int capacity, int targetDelay, int maxDelay)
        {
            _samples = new float[capacity];
            _targetDelay = targetDelay;
            _maxDelay = maxDelay;
        }

        /// <summary>Сэмплы, ещё не отданные аудиопотоку.</summary>
        public int Buffered => (_writePosition - _readPosition + _samples.Length) % _samples.Length;

        private int TargetDelay => Math.Max(_targetDelay, _readBlock);

        private int MaxDelay => Math.Min(Math.Max(_maxDelay, _readBlock * 2 + _targetDelay), _samples.Length - 1);

        /// <summary>Добавляет кадр. Только главный поток.</summary>
        /// <returns>false — задержка уже на пределе, кадр отброшен.</returns>
        public bool Write(short[] pcm, int count)
        {
            if (_starved)
            {
                _starved = false;
                AppendSilence(TargetDelay - Buffered);
            }

            if (Buffered + count > MaxDelay) return false;

            var position = _writePosition;
            for (var i = 0; i < count; i++)
                _samples[(position + i) % _samples.Length] = pcm[i] * SampleScale;

            _writePosition = (position + count) % _samples.Length;
            return true;
        }

        /// <summary>Заполняет destination; при нехватке данных — тишиной. Только аудиопоток.</summary>
        public void Read(float[] destination, float volume)
        {
            _readBlock = destination.Length;
            var position = _readPosition;
            var writePosition = _writePosition;

            for (var i = 0; i < destination.Length; i++)
            {
                if (position == writePosition)
                {
                    destination[i] = 0f;
                    _starved = true;
                    continue;
                }

                destination[i] = _samples[position] * volume;
                position = (position + 1) % _samples.Length;
            }

            _readPosition = position;
        }

        // Запись только вперёд: аудиопоток может как раз читать то, что уже записано.
        private void AppendSilence(int count)
        {
            if (count <= 0) return;

            var position = _writePosition;
            for (var i = 0; i < count; i++)
                _samples[(position + i) % _samples.Length] = 0f;

            _writePosition = (position + count) % _samples.Length;
        }
    }
}
