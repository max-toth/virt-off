using Game.Scripts.Net;
using Game.Scripts.Settings;
using UnityEngine;

namespace Game.Scripts.Voice
{
    // Отладочная панель голоса в левом верхнем углу с волной микрофона.
    [RequireComponent(typeof(VoiceManager))]
    public class VoiceDebugHud : MonoBehaviour
    {
        private const int WaveformWidth = 200;
        private const int WaveformHeight = 60;
        private const int SamplesPerFrame = 8;
        private const float Left = 10f;
        private const float LineHeight = 25f;
        private const float LabelWidth = 400f;
        private const string HotkeysHint = "R=test mic  M=mute";

        private static readonly Color32 BarColor = new(0, 255, 0, 255);
        private static readonly Color32 BackgroundColor = new(0, 0, 0, 255);

        private readonly float[] _latest = new float[SamplesPerFrame];
        private readonly float[] _history = new float[WaveformWidth];
        private readonly Color32[] _pixels = new Color32[WaveformWidth * WaveformHeight];
        private VoiceManager _voice;
        private VoiceReceiver _receiver;
        private bool _hasReceiver;
        private Texture2D _waveform;
        private int _historyPosition;

        private void Awake()
        {
            _voice = GetComponent<VoiceManager>();
            _hasReceiver = TryGetComponent(out _receiver);
            _waveform = new Texture2D(WaveformWidth, WaveformHeight, TextureFormat.RGB24, false);
        }

        private void OnDestroy() => Destroy(_waveform);

        private void Update()
        {
            if (!GameNet.TryGetConnected(out _) || !_voice.TryReadLatestSamples(_latest)) return;

            foreach (var sample in _latest)
            {
                _history[_historyPosition] = sample;
                _historyPosition = (_historyPosition + 1) % _history.Length;
            }

            RedrawWaveform();
        }

        private void OnGUI()
        {
            if (!GameNet.TryGetConnected(out var manager)) return;

            var y = Left;
            Label(ref y, $"ID={manager.LocalId}");
            Label(ref y, $"Mic: {_voice.MicDevice ?? "none"}");
            Label(ref y, $"Speakers: {(_hasReceiver ? _receiver.ActiveStreamCount : 0)}");
            Label(ref y, $"Voice: {Status()}");
            Label(ref y, $"Vol: {GameSettings.VoiceVolume:F1}  U=up  J=down");
            Label(ref y, HotkeysHint);
            GUI.DrawTexture(new Rect(Left, y, WaveformWidth, WaveformHeight), _waveform);
        }

        private string Status()
        {
            if (_voice.IsTesting) return "RECORDING (release R to play)";
            if (_voice.IsPlayingTest) return "PLAYBACK";
            if (GameSettings.MicMuted) return "MUTED";
            return _voice.IsTransmitting ? "ACTIVE" : "idle";
        }

        private static void Label(ref float y, string text)
        {
            GUI.Label(new Rect(Left, y, LabelWidth, 20f), text);
            y += LineHeight;
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
