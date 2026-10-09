using UnityEngine;
using Concentus.Structs;
using Concentus.Enums;

public class VoiceManager : MonoBehaviour
{
    private AudioClip micClip;
    private OpusEncoder encoder;
    private int lastSamplePos;
    private const int sampleRate = 16000;
    private const int frameSize = 640;
    private const float sendInterval = 0.04f;
    private float timer;
    private string micDevice;
    private float sendTimer;
    private bool initialized;
    private bool muted;
    private float[] waveformBuffer = new float[200];
    private int waveformPos;
    private Texture2D waveformTex;

    private AudioSource testSource;
    private bool testing;
    private float[] testBuffer;
    private int testWritePos;

    void Awake()
    {
        encoder = new OpusEncoder(sampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = 24000;
        encoder.Complexity = 5;
        waveformTex = new Texture2D(200, 60, TextureFormat.RGB24, false);
        testSource = gameObject.AddComponent<AudioSource>();
        testSource.loop = false;
        testSource.volume = 1f;
    }

    void Start()
    {
        var nm = GameNet.Instance;
        if (nm == null) return;

        nm.OnPacketReceived += OnVoicePacket;
        TryInitMic();
    }

    void TryInitMic()
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected)
        {
            Invoke(nameof(TryInitMic), 0.1f);
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[Voice] No microphone found");
            return;
        }

        micDevice = Microphone.devices[0];
        micClip = Microphone.Start(micDevice, true, 10, sampleRate);
        initialized = true;
        Debug.Log("[Voice] Microphone started");
    }

    void OnDisable()
    {
        CancelInvoke(nameof(TryInitMic));

        if (micClip != null && micDevice != null)
        {
            Microphone.End(micDevice);
            micClip = null;
        }
    }

    void Update()
    {
        if (!initialized) return;

        var kb = UnityEngine.InputSystem.Keyboard.current;

        if (kb != null && kb.mKey.wasPressedThisFrame)
        {
            muted = !muted;
            Debug.Log($"[Voice] {(muted ? "MUTED" : "Unmuted")}");
        }

        if (kb != null && kb.rKey.wasPressedThisFrame)
            StartMicTest();
        else if (kb != null && kb.rKey.wasReleasedThisFrame)
            StopMicTest();

        if (kb != null)
        {
            if (kb.uKey.wasPressedThisFrame)
            {
                var vr = VoiceReceiver.Instance;
                if (vr) vr.volume = Mathf.Clamp(vr.volume + 0.1f, 0f, 2f);
            }
            if (kb.jKey.wasPressedThisFrame)
            {
                var vr = VoiceReceiver.Instance;
                if (vr) vr.volume = Mathf.Clamp(vr.volume - 0.1f, 0f, 2f);
            }
        }

        if (testing && micClip != null && Microphone.IsRecording(micDevice))
        {
            int mp = Microphone.GetPosition(micDevice);
            int avail = mp - lastSamplePos;
            if (avail < 0) avail += micClip.samples * micClip.channels;
            if (avail > 0 && testWritePos + avail < testBuffer.Length)
            {
                float[] chunk = new float[avail];
                micClip.GetData(chunk, lastSamplePos % micClip.samples);
                System.Array.Copy(chunk, 0, testBuffer, testWritePos, avail);
                testWritePos += avail;
            }
            lastSamplePos = mp;
            return;
        }

        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;

        if (micClip == null || !Microphone.IsRecording(micDevice))
            return;

        int micPos = Microphone.GetPosition(micDevice);
        float[] waveSamples = new float[8];
        micClip.GetData(waveSamples, micPos - waveSamples.Length < 0 ? 0 : micPos - waveSamples.Length);
        for (int i = 0; i < waveSamples.Length; i++)
        {
            waveformBuffer[waveformPos] = waveSamples[i];
            waveformPos = (waveformPos + 1) % waveformBuffer.Length;
        }

        int w = waveformTex.width, h = waveformTex.height;
        int halfH = h / 2;
        for (int x = 0; x < w; x++)
        {
            int idx = (waveformPos - (w - x) + waveformBuffer.Length) % waveformBuffer.Length;
            float val = Mathf.Abs(waveformBuffer[idx]) * halfH;
            if (val > halfH) val = halfH;
            int barH = (int)val;
            for (int py = 0; py < h; py++)
            {
                bool onBar = py >= halfH - barH && py <= halfH + barH;
                waveformTex.SetPixel(x, py, onBar ? Color.green : Color.black);
            }
        }
        waveformTex.Apply();

        if (muted) return;

        timer += Time.deltaTime;
        if (timer < sendInterval) return;
        timer = 0;

        int currentPos = Microphone.GetPosition(micDevice);
        int available = currentPos - lastSamplePos;
        if (available < 0) available += micClip.samples * micClip.channels;
        if (available < frameSize) return;

        float[] samples = new float[frameSize];
        micClip.GetData(samples, lastSamplePos % micClip.samples);
        lastSamplePos = (lastSamplePos + frameSize) % micClip.samples;

        short[] pcm = new short[frameSize];
        for (int i = 0; i < frameSize; i++)
            pcm[i] = (short)(Mathf.Clamp01(samples[i]) * short.MaxValue);

        byte[] opus = new byte[4000];
        int len = encoder.Encode(pcm, 0, frameSize, opus, 0, opus.Length);

        byte[] packet = new byte[1 + 8 + len];
        packet[0] = 0x01;
        System.BitConverter.GetBytes(nm.LocalId).CopyTo(packet, 1);
        System.Array.Copy(opus, 0, packet, 9, len);

        nm.Send(packet);

        if (sendTimer > 2f)
        {
            sendTimer = 0;
            Debug.Log($"[Voice] Sending ({packet.Length}B)");
        }
        sendTimer += Time.deltaTime;
    }

    void StartMicTest()
    {
        if (!initialized || micClip == null) return;
        testBuffer = new float[16000 * 5];
        testWritePos = 0;
        testing = true;
        Debug.Log("[Voice] Mic test RECORDING");
    }

    void StopMicTest()
    {
        if (!testing) return;
        testing = false;

        if (testWritePos == 0) return;

        var clip = AudioClip.Create("MicTest", testWritePos, 1, sampleRate, false);
        clip.SetData(testBuffer, 0);
        testSource.clip = clip;
        testSource.Play();
        Debug.Log($"[Voice] Mic test PLAYBACK ({testWritePos} samples)");
    }

    private void OnVoicePacket(ulong senderId, byte[] data)
    {
        if (data.Length < 10 || data[0] != 0x01) return;

        ulong sourceId = System.BitConverter.ToUInt64(data, 1);
        byte[] opusData = new byte[data.Length - 9];
        System.Array.Copy(data, 9, opusData, 0, opusData.Length);

        VoiceReceiver.Instance?.ReceiveVoice(sourceId, opusData);
    }

    void OnGUI()
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;

        int y = 10;
        GUI.Label(new Rect(10, y, 400, 20), $"ID={nm.LocalId}");
        y += 25;

        GUI.Label(new Rect(10, y, 400, 20), $"Mic: {micDevice ?? "none"}");
        y += 25;

        int streamCount = VoiceReceiver.Instance ? VoiceReceiver.Instance.ActiveStreamCount() : 0;
        GUI.Label(new Rect(10, y, 400, 20), $"Speakers: {streamCount}");
        y += 25;

        string status;
        if (testing)
            status = "RECORDING (release R to play)";
        else if (testSource.isPlaying)
            status = "PLAYBACK";
        else
        {
            var active = !muted && micClip != null && Microphone.IsRecording(micDevice);
            status = muted ? "MUTED" : (active ? "ACTIVE" : "idle");
        }
        GUI.Label(new Rect(10, y, 400, 20), $"Voice: {status}");
        y += 25;

        float vol = VoiceReceiver.Instance ? VoiceReceiver.Instance.volume : 1f;
        GUI.Label(new Rect(10, y, 400, 20), $"Vol: {vol:F1}  U=up  J=down");
        y += 25;

        GUI.Label(new Rect(10, y, 400, 20), "R=test mic  M=mute");
        y += 25;

        GUI.DrawTexture(new Rect(10, y, 200, 60), waveformTex);
    }

    void OnDestroy()
    {
        CancelInvoke(nameof(TryInitMic));
        if (waveformTex != null) DestroyImmediate(waveformTex);
        var nm = GameNet.Instance;
        if (nm != null)
            nm.OnPacketReceived -= OnVoicePacket;
    }
}
