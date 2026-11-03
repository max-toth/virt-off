using Game.Scripts.Controls;
using Game.Scripts.Settings;
using Game.Scripts.Voice.Capture;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.Scripts.Voice
{
    /// <summary>Свой голос: микрофон → Opus → сервер. Клавиши: M — микрофон, R (удерживать) — проверка, U/J — громкость голосов.</summary>
    public class VoiceManager : MonoBehaviour
    {
        private const float VoiceVolumeStep = 0.1f;
        private const float FrameSeconds = (float)VoiceFormat.FrameSize / VoiceFormat.SampleRate;

        [Tooltip("Пиковый уровень микрофона (с усилением), с которого кадр считается речью и отправляется")]
        [Range(0f, 0.2f)]
        [SerializeField] private float activationThreshold = 0.02f;
        [Tooltip("Сколько секунд после речи ещё отправлять, чтобы не обрезать окончания слов")]
        [SerializeField] private float hangoverSeconds = 0.3f;

        private readonly float[] _frame = new float[VoiceFormat.FrameSize];
        private MicrophoneCapture _capture;
        private VoiceEncoder _encoder;
        private MicTest _micTest;
        private VoiceActivityGate _gate;
        private VoiceNetwork _network;
        private GameSettings _settings;
        private GameplayInput _input;
        private bool _waitingForConnection;

        /// <summary>Уровень микрофона с учётом усиления, 0..1.</summary>
        public float MicLevel => _capture.Level;

        /// <summary>Микрофон запущен (после подключения к серверу и при наличии устройства).</summary>
        public bool IsReady => _capture.IsStarted;
        public bool IsTesting => _micTest.IsRecording;
        public bool IsPlayingTest => _micTest.IsPlaying;
        /// <summary>Идёт отправка: микрофон включён и сейчас звучит речь.</summary>
        public bool IsTransmitting => !_settings.MicMuted && _capture.IsRecording && _gate.IsOpen;
        public string MicDevice => _capture.Device;

        [Inject]
        public void Construct(VoiceNetwork network, GameSettings settings, GameplayInput input)
        {
            _network = network;
            _settings = settings;
            _input = input;
            _capture = new MicrophoneCapture(settings);
            _encoder = new VoiceEncoder();
            _gate = new VoiceActivityGate(activationThreshold, Mathf.CeilToInt(hangoverSeconds / FrameSeconds));
        }

        public void StartMicTest()
        {
            if (IsReady) _micTest.Begin();
        }

        public void StopMicTest()
        {
            if (IsTesting) _micTest.Finish(_settings.MicGain);
        }

        public bool TryReadLatestSamples(float[] destination) => _capture.TryReadLatest(destination);

        private void Awake() => _micTest = new MicTest(gameObject.AddComponent<AudioSource>());

        private void Start()
        {
            _settings.MicDeviceChanged += RestartMic;

            if (_network.IsConnected)
            {
                StartMic();
                return;
            }

            _waitingForConnection = true;
            _network.Connected += OnConnected;
        }

        private void OnDisable()
        {
            _micTest.Cancel();
            _capture.Stop();
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.MicDeviceChanged -= RestartMic;
            StopWaitingForConnection();
            _micTest.Dispose();
        }

        private void Update()
        {
            if (!IsReady) return;

            HandleHotkeys();
            _capture.UpdateLevel(_settings.MicGain);

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
            if (_settings.MicMuted || !_network.IsConnected)
            {
                _gate.Close();
                _capture.SkipToNow();
                return;
            }

            // Тишина не отправляется: собеседникам не приходится её декодировать, трафик — только пока говорят.
            while (_capture.TryRead(_frame))
            {
                if (!_gate.ShouldSend(_frame, _settings.MicGain)) continue;

                var length = _encoder.Encode(_frame, _settings.MicGain);
                _network.Send(_encoder.Output, length);
            }
        }

        private void HandleHotkeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _input.IsBlocked) return;

            if (keyboard.mKey.wasPressedThisFrame)
            {
                _settings.MicMuted = !_settings.MicMuted;
                VoiceDiagnostics.Log($"[Voice] {(_settings.MicMuted ? "Muted" : "Unmuted")}");
            }

            if (keyboard.rKey.wasPressedThisFrame) StartMicTest();
            else if (keyboard.rKey.wasReleasedThisFrame) StopMicTest();

            if (keyboard.uKey.wasPressedThisFrame) _settings.VoiceVolume += VoiceVolumeStep;
            if (keyboard.jKey.wasPressedThisFrame) _settings.VoiceVolume -= VoiceVolumeStep;
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
            _network.Connected -= OnConnected;
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
            if (!_network.IsConnected) return;

            _micTest.Cancel();
            _capture.Stop();
            StartMic();
        }
    }
}
