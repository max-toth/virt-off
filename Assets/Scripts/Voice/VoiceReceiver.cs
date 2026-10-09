using UnityEngine;
using UnityEngine.UI;
using Concentus.Structs;
using System.Collections.Generic;

public class VoiceReceiver : MonoBehaviour
{
    public static VoiceReceiver Instance { get; private set; }

    public Transform audioRoot;
    public Text notificationText;
    [Range(0f, 2f)] public float volume = 1f;

    private Dictionary<ulong, VoiceStream> streams = new Dictionary<ulong, VoiceStream>();

    void Awake()
    {
        Instance = this;
    }

    public void ReceiveVoice(ulong clientId, byte[] opusData)
    {
        if (!streams.ContainsKey(clientId))
        {
            streams[clientId] = new VoiceStream(16000, clientId, audioRoot ? audioRoot : transform);

            if (notificationText != null)
            {
                notificationText.text = $"player-{clientId} joined";
                CancelInvoke(nameof(ClearNotification));
                Invoke(nameof(ClearNotification), 3f);
            }
            Debug.Log($"player-{clientId} joined");
        }

        streams[clientId].Feed(opusData);
    }

    public int ActiveStreamCount() => streams.Count;

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
        private const int playoutDelay = 1280;
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

        public void Feed(byte[] opusData)
        {
            short[] pcm = new short[640];
            int samples = decoder.Decode(opusData, 0, opusData.Length, pcm, 0, pcm.Length);

            int wp = writePos;
            for (int i = 0; i < samples; i++)
            {
                buffer[(wp + i) % bufferLength] = pcm[i] / (float)short.MaxValue;
            }
            writePos = (wp + samples) % bufferLength;
        }

        void OnAudioRead(float[] data)
        {
            int rp = readPos;
            int wp = writePos;
            float vol = Instance ? Instance.volume : 1f;
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
