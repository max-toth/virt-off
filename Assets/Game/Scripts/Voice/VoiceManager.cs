using Game.Scripts.Controls;
using Game.Scripts.Settings;
using Game.Scripts.Voice.Capture;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Voice
{
    /// <summary>Свой голос: микрофон → Opus → сервер. Клавиши: M — микрофон, R (удерживать) — проверка, U/J — громкость голосов.</summary>
    public class VoiceManager : MonoBehaviour
    {
        private const float VoiceVolumeStep = 0.1f;

        private readonly float[] _frame = new float[VoiceFormat.FrameSize];
        private MicrophoneCapture _capture;
        private VoiceEncoder _encoder;
        private MicTest _micTest;
        private bool _waitingForConnection;

        private static VoiceManager _instance;
        private static bool _hasInstance;

        /// <summary>Уровень микрофона с учётом усиления, 0..1.</summary>
        public float MicLevel => _capture.Level;

        /// <summary>Микрофон запущен (после подключения к серверу и при наличии устройства).</summary>
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

        /// <summary>Компонент на своём игроке, если он есть в сцене.</summary>
        public static bool TryGetInstance(out VoiceManager instance)
        {
            instance = _instance;
            return _hasInstance;
        }

        public bool TryReadLatestSamples(float[] destination) => _capture.TryReadLatest(destination);

        private void Awake()
        {
            _instance = this;
            _hasInstance = true;
            _capture = new MicrophoneCapture();
            _encoder = new VoiceEncoder();
            _micTest = new MicTest(gameObject.AddComponent<AudioSource>());
        }

        private void Start()
        {
            GameSettings.MicDeviceChanged += RestartMic;

            if (VoiceNetwork.IsConnected)
            {
                StartMic();
                return;
            }

            _waitingForConnection = true;
            VoiceNetwork.Connected += OnConnected;
        }

        private void OnDisable()
        {
            _micTest.Cancel();
            _capture.Stop();
        }

        private void OnDestroy()
        {
            GameSettings.MicDeviceChanged -= RestartMic;
            StopWaitingForConnection();
            _micTest.Dispose();
            if (_instance != this) return;
            _instance = null;
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
            if (GameSettings.MicMuted || !VoiceNetwork.IsConnected)
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
            if (keyboard == null || GameplayInput.IsBlocked) return;

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

        private void OnConnected()
        {
            StopWaitingForConnection();
            StartMic();
        }

        private void StopWaitingForConnection()
        {
            if (!_waitingForConnection) return;
            _waitingForConnection = false;
            VoiceNetwork.Connected -= OnConnected;
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
            if (!VoiceNetwork.IsConnected) return;

            _micTest.Cancel();
            _capture.Stop();
            StartMic();
        }
    }
}
