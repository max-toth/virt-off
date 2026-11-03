using System;
using Concentus.Structs;
using Game.Scripts.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Voice.Playback
{
    public sealed class VoiceStream : IDisposable
    {
        private const int BufferLength = 48000;
        private const int PlayoutDelay = 320;

        private readonly OpusDecoder _decoder;
        private readonly short[] _pcm = new short[VoiceFormat.FrameSize];
        private readonly float[] _buffer = new float[BufferLength];
        private readonly AudioSource _source;
        private readonly AudioClip _clip;
        private volatile int _writePosition = PlayoutDelay % BufferLength;
        private volatile int _readPosition;
        private bool _disposed;

        public VoiceStream(string ownerId, Transform parent)
        {
            _decoder = new OpusDecoder(VoiceFormat.SampleRate, VoiceFormat.Channels);

            var host = new GameObject($"VoiceStream_{ownerId}");
            host.transform.SetParent(parent, false);
            _source = host.AddComponent<AudioSource>();
            _source.loop = true;
            _source.volume = 1f;

            _clip = AudioClip.Create("VoiceStream", BufferLength, VoiceFormat.Channels, VoiceFormat.SampleRate, true, OnAudioRead);
            _source.clip = _clip;
            _source.Play();
        }

        /// <summary>Декодирует кадр Opus в буфер воспроизведения.</summary>
        /// <returns>Пиковая амплитуда кадра, 0..1.</returns>
        public float Feed(byte[] data, int offset, int count)
        {
            var samples = _decoder.Decode(data, offset, count, _pcm, 0, _pcm.Length);

            var writePosition = _writePosition;
            var peak = 0f;
            for (var i = 0; i < samples; i++)
            {
                var value = _pcm[i] / (float)short.MaxValue;
                _buffer[(writePosition + i) % BufferLength] = value;
                peak = Mathf.Max(peak, Mathf.Abs(value));
            }

            _writePosition = (writePosition + samples) % BufferLength;
            return peak;
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

        private void OnAudioRead(float[] data)
        {
            var readPosition = _readPosition;
            var writePosition = _writePosition;
            var volume = GameSettings.VoiceVolume;

            for (var i = 0; i < data.Length; i++)
            {
                if (readPosition == writePosition)
                {
                    data[i] = 0f;
                    continue;
                }

                data[i] = _buffer[readPosition] * volume;
                readPosition = (readPosition + 1) % BufferLength;
            }

            _readPosition = readPosition;
        }
    }
}
