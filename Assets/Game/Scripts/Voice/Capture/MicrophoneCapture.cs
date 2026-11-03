using Game.Scripts.Settings;
using UnityEngine;

namespace Game.Scripts.Voice.Capture
{
    public sealed class MicrophoneCapture
    {
        private const int ClipLengthSeconds = 10;
        private const int LevelWindow = 320;
        private const float LevelDecayPerSecond = 1.5f;

        private readonly float[] _levelBuffer = new float[LevelWindow];
        private AudioClip _clip;
        private int _readPosition;

        public string Device { get; private set; }
        public bool IsStarted { get; private set; }

        // Пиковый уровень с учётом усиления (0..1): быстрый подъём, плавный спад.
        public float Level { get; private set; }

        public bool IsRecording => IsStarted && Microphone.IsRecording(Device);

        public bool Start()
        {
            Device = GameSettings.ResolveMicDevice();
            if (Device == null) return false;

            _clip = Microphone.Start(Device, true, ClipLengthSeconds, VoiceFormat.SampleRate);
            _readPosition = 0;
            IsStarted = Microphone.IsRecording(Device);
            return IsStarted;
        }

        public void Stop()
        {
            if (IsStarted) Microphone.End(Device);
            IsStarted = false;
            Level = 0f;
            _clip = null;
        }

        public bool TryRead(float[] frame)
        {
            if (!IsRecording || Available() < frame.Length) return false;

            _clip.GetData(frame, _readPosition);
            _readPosition = (_readPosition + frame.Length) % _clip.samples;
            return true;
        }

        public void SkipToNow()
        {
            if (IsRecording) _readPosition = Microphone.GetPosition(Device);
        }

        public bool TryReadLatest(float[] destination)
        {
            if (!IsRecording) return false;

            var position = Microphone.GetPosition(Device) - destination.Length;
            _clip.GetData(destination, Mathf.Max(position, 0));
            return true;
        }

        public void UpdateLevel(float gain)
        {
            if (!IsRecording)
            {
                Level = 0f;
                return;
            }

            var position = Microphone.GetPosition(Device) - LevelWindow;
            if (position < 0) position += _clip.samples;
            _clip.GetData(_levelBuffer, position);

            var peak = Mathf.Max(Mathf.Max(_levelBuffer), -Mathf.Min(_levelBuffer));
            Level = Mathf.Max(peak * gain, Level - Time.deltaTime * LevelDecayPerSecond);
        }

        private int Available()
        {
            var available = Microphone.GetPosition(Device) - _readPosition;
            return available < 0 ? available + _clip.samples : available;
        }
    }
}
