using Game.Scripts.Net;
using Game.Scripts.Settings;
using Game.Scripts.UI.Menu;
using Game.Scripts.Voice.Capture;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Voice
{
    // Свой голос: микрофон → Opus → сервер. Горячие клавиши: M — микрофон, R — проверка, U/J — громкость голосов.
    public class VoiceManager : MonoBehaviour
    {
        private const float VoiceVolumeStep = 0.1f;

        private readonly float[] _frame = new float[VoiceFormat.FrameSize];
        private MicrophoneCapture _capture;
        private VoiceEncoder _encoder;
        private MicTest _micTest;
        private NetManager _pendingManager;

        private static bool _hasInstance;

        public static VoiceManager Instance { get; private set; }

        public float MicLevel => _capture.Level;
        public bool IsReady => _capture.IsStarted;
        public bool IsTesting => _micTest.IsRecording;
        public bool IsPlayingTest => _micTest.IsPlaying;
        public bool IsTransmitting => !GameSettings.MicMuted && _capture.IsRecording;
        public string MicDevice => _capture.Device;

        public void StartMicTest()
        {
            if (IsReady) _micTest.Begin();
        }

        public void StopMicTest()
        {
            if (IsTesting) _micTest.Finish(GameSettings.MicGain);
        }

        public static bool TryGetInstance(out VoiceManager instance)
        {
            instance = Instance;
            return _hasInstance;
        }

        public bool TryReadLatestSamples(float[] destination) => _capture.TryReadLatest(destination);

        private void Awake()
        {
            Instance = this;
            _hasInstance = true;
            _capture = new MicrophoneCapture();
            _encoder = new VoiceEncoder();
            _micTest = new MicTest(gameObject.AddComponent<AudioSource>());
        }

        private void Start()
        {
            GameSettings.MicDeviceChanged += RestartMic;

            if (GameNet.TryGetConnected(out _))
            {
                StartMic();
                return;
            }

            _pendingManager = GameNet.Instance;
            if (_pendingManager != null) _pendingManager.IdAssigned += OnIdAssigned;
        }

        private void OnDisable()
        {
            _micTest.Cancel();
            _capture.Stop();
        }

        private void OnDestroy()
        {
            GameSettings.MicDeviceChanged -= RestartMic;
            UnsubscribePending();
            _micTest.Dispose();
            if (Instance != this) return;
            Instance = null;
            _hasInstance = false;
        }

        private void Update()
        {
            if (!IsReady) return;

            HandleHotkeys();
            _capture.UpdateLevel(GameSettings.MicGain);

            if (IsTesting)
            {
                _micTest.Record(_capture, _frame);
                return;
            }

            Transmit();
        }

        // Отправляет все накопленные кадры: при низком FPS отставание не копится.
        private void Transmit()
        {
            if (GameSettings.MicMuted || !GameNet.TryGetConnected(out _))
            {
                _capture.SkipToNow();
                return;
            }

            while (_capture.TryRead(_frame))
            {
                var length = _encoder.Encode(_frame, GameSettings.MicGain);
                VoiceNetwork.Send(_encoder.Output, length);
            }
        }

        private void HandleHotkeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || GameMenu.IsOpen) return;

            if (keyboard.mKey.wasPressedThisFrame)
            {
                GameSettings.MicMuted = !GameSettings.MicMuted;
                VoiceDiagnostics.Log($"[Voice] {(GameSettings.MicMuted ? "Muted" : "Unmuted")}");
            }

            if (keyboard.rKey.wasPressedThisFrame) StartMicTest();
            else if (keyboard.rKey.wasReleasedThisFrame) StopMicTest();

            if (keyboard.uKey.wasPressedThisFrame) GameSettings.VoiceVolume += VoiceVolumeStep;
            if (keyboard.jKey.wasPressedThisFrame) GameSettings.VoiceVolume -= VoiceVolumeStep;
        }

        private void OnIdAssigned()
        {
            UnsubscribePending();
            StartMic();
        }

        private void UnsubscribePending()
        {
            if (_pendingManager != null) _pendingManager.IdAssigned -= OnIdAssigned;
            _pendingManager = null;
        }

        private void StartMic()
        {
            VoiceDiagnostics.Log(_capture.Start()
                ? $"[Voice] Microphone started: {_capture.Device}"
                : "[Voice] No microphone found");
        }

        // До подключения микрофон не запущен: выбранное устройство подхватится в StartMic.
        private void RestartMic()
        {
            if (!GameNet.TryGetConnected(out _)) return;

            _micTest.Cancel();
            _capture.Stop();
            StartMic();
        }
    }
}
