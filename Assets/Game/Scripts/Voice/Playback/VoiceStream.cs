using System;
using Concentus.Structs;
using Game.Scripts.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Voice.Playback
{
    public sealed class VoiceStream : IDisposable
    {
        private const int ClipLength = VoiceFormat.SampleRate * 3;
        private const int TargetDelay = VoiceFormat.FrameSize;
        private const int MaxDelay = VoiceFormat.SampleRate / 2;
        private const int BufferCapacity = VoiceFormat.SampleRate * 2;

        private readonly OpusDecoder _decoder;
        private readonly short[] _pcm = new short[VoiceFormat.FrameSize];
        private readonly VoiceJitterBuffer _buffer = new(BufferCapacity, TargetDelay, MaxDelay);
        private readonly AudioSource _source;
        private readonly AudioClip _clip;
        private bool _disposed;

        public VoiceStream(string ownerId, Transform parent)
        {
            _decoder = new OpusDecoder(VoiceFormat.SampleRate, VoiceFormat.Channels);

            var host = new GameObject($"VoiceStream_{ownerId}");
            host.transform.SetParent(parent, false);
            _source = host.AddComponent<AudioSource>();
            _source.loop = true;
            _source.volume = 1f;

            _clip = AudioClip.Create("VoiceStream", ClipLength, VoiceFormat.Channels, VoiceFormat.SampleRate, true, OnAudioRead);
            _source.clip = _clip;
            _source.Play();
        }

        /// <summary>Декодирует кадр Opus в буфер воспроизведения.</summary>
        /// <returns>Пиковая амплитуда кадра, 0..1.</returns>
        public float Feed(byte[] data, int offset, int count)
        {
            // Декодируется каждый кадр, даже отброшенный буфером: состояние декодера зависит от предыдущих кадров.
            var samples = _decoder.Decode(data, offset, count, _pcm, 0, _pcm.Length);
            _buffer.Write(_pcm, samples);

            var peak = 0;
            for (var i = 0; i < samples; i++)
                peak = Math.Max(peak, Math.Abs((int)_pcm[i]));
            return Math.Min(peak / (float)short.MaxValue, 1f);
        }

        /// <summary>Возобновляет воспроизведение после смены устройства вывода в ОС.</summary>
        public void Restart()
        {
            if (!_disposed && !_source.isPlaying) _source.Play();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _source.Stop();
            Object.Destroy(_source.gameObject);
            Object.Destroy(_clip);
        }

        private void OnAudioRead(float[] data) => _buffer.Read(data, GameSettings.VoiceVolume);
    }
}
