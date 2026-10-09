using UnityEngine;
using UnityEngine.UI;
using Concentus.Structs;
using System.Collections.Generic;

public class VoiceReceiver : MonoBehaviour
{
    public static VoiceReceiver Instance { get; private set; }

    public Transform audioRoot;
    public Text notificationText;

    const float SpeakingThreshold = 0.03f;

    private Dictionary<ulong, VoiceStream> streams = new Dictionary<ulong, VoiceStream>();

    void Awake()
    {
        Instance = this;
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        Lobby.PlayerLeft += OnPlayerLeft;
    }

    void OnDestroy()
    {
        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        Lobby.PlayerLeft -= OnPlayerLeft;
        if (Instance == this) Instance = null;
    }

    // Пользователь сменил устройство вывода в ОС — аудио перезапускается, источники надо запустить заново
    private void OnAudioConfigurationChanged(bool deviceWasChanged)
    {
        foreach (var stream in streams.Values)
            stream.Restart();
    }

    private void OnPlayerLeft(ulong clientId)
    {
        if (!streams.TryGetValue(clientId, out var stream)) return;
        stream.Dispose();
        streams.Remove(clientId);
        ShowNotification($"player-{clientId} left");
    }

    public void ReceiveVoice(ulong clientId, byte[] opusData)
    {
        // Игрок заглушён в меню — голос просто не воспроизводим
        if (Lobby.IsMuted(clientId)) return;

        if (!streams.ContainsKey(clientId))
        {
            streams[clientId] = new VoiceStream(16000, clientId, audioRoot ? audioRoot : transform);

            ShowNotification($"player-{clientId} joined");
            Debug.Log($"player-{clientId} joined");
        }

        if (streams[clientId].Feed(opusData) > SpeakingThreshold)
            Lobby.MarkSpeaking(clientId);
    }

    public int ActiveStreamCount() => streams.Count;

    private void ShowNotification(string text)
    {
        if (notificationText == null) return;
        notificationText.text = text;
        CancelInvoke(nameof(ClearNotification));
        Invoke(nameof(ClearNotification), 3f);
    }

    private void ClearNotification()
    {
        if (notificationText != null)
            notificationText.text = "";
    }

    private class VoiceStream
    {
        private OpusDecoder decoder;
        private AudioSource source;
        private volatile int writePos;
        private volatile int readPos;
        private int bufferLength = 48000;
        private const int playoutDelay = 320;
        private float[] buffer;

        public VoiceStream(int sampleRate, ulong ownerId, Transform parent)
        {
            decoder = new OpusDecoder(sampleRate, 1);
            buffer = new float[bufferLength];
            writePos = playoutDelay % bufferLength;

            var go = new GameObject($"VoiceStream_{ownerId}");
            go.transform.SetParent(parent, false);
            source = go.AddComponent<AudioSource>();
            source.loop = true;
            source.volume = 1f;

            var clip = AudioClip.Create("VoiceStream", bufferLength, 1, sampleRate, true, OnAudioRead, OnAudioSetPosition);
            source.clip = clip;
            source.Play();
        }

        public void Dispose()
        {
            if (source == null) return;
            var clip = source.clip;
            source.Stop();
            Object.Destroy(source.gameObject);
            if (clip != null) Object.Destroy(clip);
            source = null;
        }

        public void Restart()
        {
            if (source != null && !source.isPlaying)
                source.Play();
        }

        // Возвращает пиковую амплитуду кадра (0..1)
        public float Feed(byte[] opusData)
        {
            short[] pcm = new short[640];
            int samples = decoder.Decode(opusData, 0, opusData.Length, pcm, 0, pcm.Length);

            int wp = writePos;
            float peak = 0f;
            for (int i = 0; i < samples; i++)
            {
                float v = pcm[i] / (float)short.MaxValue;
                buffer[(wp + i) % bufferLength] = v;
                peak = Mathf.Max(peak, Mathf.Abs(v));
            }
            writePos = (wp + samples) % bufferLength;
            return peak;
        }

        void OnAudioRead(float[] data)
        {
            int rp = readPos;
            int wp = writePos;
            float vol = GameSettings.VoiceVolume;
            for (int i = 0; i < data.Length; i++)
            {
                if (rp != wp)
                {
                    data[i] = buffer[rp] * vol;
                    rp = (rp + 1) % bufferLength;
                }
                else
                {
                    data[i] = 0f;
                }
            }
            readPos = rp;
        }

        void OnAudioSetPosition(int position) { }
    }
}
