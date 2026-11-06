using Game.Scripts.Net;
using Game.Scripts.Settings;
using Game.Scripts.Voice;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Game.Scripts.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public class VoiceDebugPresenter : MonoBehaviour
    {
        private const int WaveformWidth = 200;
        private const int WaveformHeight = 60;
        private const int SamplesPerFrame = 8;

        private static readonly Color32 BarColor = new(0, 255, 0, 255);
        private static readonly Color32 BackgroundColor = new(0, 0, 0, 255);

        private readonly float[] _latest = new float[SamplesPerFrame];
        private readonly float[] _history = new float[WaveformWidth];
        private readonly Color32[] _pixels = new Color32[WaveformWidth * WaveformHeight];
        private Texture2D _waveform;
        private int _historyPosition;

        private VisualElement _panel;
        private Label _idLabel;
        private Label _micLabel;
        private Label _speakersLabel;
        private Label _statusLabel;
        private Label _volumeLabel;

        private VoiceManager _voice;
        private VoiceReceiver _receiver;
        private INetTransport _transport;
        private GameSettings _settings;

        private string _shownId;
        private string _shownMic;
        private int _shownSpeakers = -1;
        private string _shownStatus;
        private float _shownVolume = -1f;

        [Inject]
        public void Construct(VoiceManager voice, VoiceReceiver receiver, INetTransport transport, GameSettings settings)
        {
            _voice = voice;
            _receiver = receiver;
            _transport = transport;
            _settings = settings;
        }

        private void Awake() => _waveform = new Texture2D(WaveformWidth, WaveformHeight, TextureFormat.RGB24, false);

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _panel = root.Q("voicePanel");
            _idLabel = root.Q<Label>("voiceIdLabel");
            _micLabel = root.Q<Label>("voiceMicLabel");
            _speakersLabel = root.Q<Label>("voiceSpeakersLabel");
            _statusLabel = root.Q<Label>("voiceStatusLabel");
            _volumeLabel = root.Q<Label>("voiceVolumeLabel");
            root.Q<Image>("voiceWaveform").image = _waveform;
        }

        private void OnDestroy() => Destroy(_waveform);

        private void Update()
        {
            var visible = _transport.IsConnected;
            _panel.SetVisible(visible);
            if (!visible) return;

            ShowLabels();
            if (!_voice.TryReadLatestSamples(_latest)) return;

            foreach (var sample in _latest)
            {
                _history[_historyPosition] = sample;
                _historyPosition = (_historyPosition + 1) % _history.Length;
            }

            RedrawWaveform();
        }

        private void ShowLabels()
        {
            var id = _transport.LocalId;
            if (id != _shownId)
            {
                _shownId = id;
                _idLabel.text = $"ID={id}";
            }

            var mic = _voice.MicStatus;
            if (mic != _shownMic)
            {
                _shownMic = mic;
                _micLabel.text = $"Mic: {mic}";
            }

            var speakers = _receiver.ActiveStreamCount;
            if (speakers != _shownSpeakers)
            {
                _shownSpeakers = speakers;
                _speakersLabel.text = $"Speakers: {speakers}";
            }

            var status = Status();
            if (!ReferenceEquals(status, _shownStatus))
            {
                _shownStatus = status;
                _statusLabel.text = $"Voice: {status}";
            }

            var volume = _settings.VoiceVolume;
            if (!Mathf.Approximately(volume, _shownVolume))
            {
                _shownVolume = volume;
                _volumeLabel.text = $"Vol: {volume:F1}  U=up  J=down";
            }
        }

        private string Status()
        {
            if (_voice.IsTesting) return "RECORDING (release R to play)";
            if (_voice.IsPlayingTest) return "PLAYBACK";
            if (_settings.MicMuted) return "MUTED";
            return _voice.IsTransmitting ? "ACTIVE" : "idle";
        }

        private void RedrawWaveform()
        {
            const int half = WaveformHeight / 2;
            for (var x = 0; x < WaveformWidth; x++)
            {
                var index = (_historyPosition - (WaveformWidth - x) + _history.Length) % _history.Length;
                var bar = (int)Mathf.Min(Mathf.Abs(_history[index]) * half, half);

                for (var y = 0; y < WaveformHeight; y++)
                {
                    var onBar = y >= half - bar && y <= half + bar;
                    _pixels[y * WaveformWidth + x] = onBar ? BarColor : BackgroundColor;
                }
            }

            _waveform.SetPixels32(_pixels);
            _waveform.Apply();
        }
    }
}
