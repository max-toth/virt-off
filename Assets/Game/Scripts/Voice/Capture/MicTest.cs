using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Voice.Capture
{
    // Проверка микрофона: запись до 5 секунд и воспроизведение себе.
    public sealed class MicTest : IDisposable
    {
        private const int MaxSeconds = 5;
        private const string ClipName = "MicTest";

        private readonly AudioSource _source;
        private readonly float[] _recording = new float[VoiceFormat.SampleRate * MaxSeconds];
        private int _length;
        private AudioClip _clip;
        private bool _hasClip;

        public MicTest(AudioSource source)
        {
            _source = source;
            _source.loop = false;
            _source.volume = 1f;
        }

        public bool IsRecording { get; private set; }
        public bool IsPlaying => _source.isPlaying;

        public void Begin()
        {
            _length = 0;
            IsRecording = true;
        }

        // Забирает всё, что успел записать микрофон; лишнее сверх 5 секунд отбрасывается.
        public void Record(MicrophoneCapture capture, float[] frame)
        {
            while (capture.TryRead(frame))
            {
                if (_length + frame.Length > _recording.Length) continue;
                Array.Copy(frame, 0, _recording, _length, frame.Length);
                _length += frame.Length;
            }
        }

        public void Finish(float gain)
        {
            IsRecording = false;
            if (_length == 0) return;

            var samples = new float[_length];
            for (var i = 0; i < _length; i++) samples[i] = Mathf.Clamp(_recording[i] * gain, -1f, 1f);

            ReleaseClip();
            _clip = AudioClip.Create(ClipName, _length, VoiceFormat.Channels, VoiceFormat.SampleRate, false);
            _clip.SetData(samples, 0);
            _hasClip = true;
            _source.clip = _clip;
            _source.Play();
        }

        public void Cancel() => IsRecording = false;

        public void Dispose() => ReleaseClip();

        private void ReleaseClip()
        {
            if (!_hasClip) return;
            _source.Stop();
            _source.clip = null;
            Object.Destroy(_clip);
            _clip = null;
            _hasClip = false;
        }
    }
}
