# Multiplayer + Voice Chat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Добавить голосовой чат и мультиплеер в офисную сцену на бесплатном стеке.

**Architecture:** Unity Netcode for GameObjects (NGO) + Concentus (Opus-кодек) — сервер отдельным headless билдом, клиент — standalone игрок. Голос передаётся через CustomMessage NGO.

**Tech Stack:** Unity Netcode, Concentus Opus, Unity Transport

## Global Constraints

- Ничего не покупать — только бесплатные библиотеки
- Толстый клиент (игровой билд)
- Сервер — отдельный headless build
- Voice chat через Opus (Concentus), передача поверх NGO CustomMessage

---

### Task 1: Установка пакетов

**Files:**
- Modify: `Packages/manifest.json`

- [ ] **Step 1: Установить Netcode**

Через Package Manager: `Window → Package Manager → Add package by name → com.unity.netcode.gameobjects`

Либо добавить в `Packages/manifest.json`:
```json
"com.unity.netcode.gameobjects": "2.2.0"
```

- [ ] **Step 2: Скачать Concentus**

Скачать `Concentus.dll` (только managed-сборку, без нативных) из релизов: https://github.com/lostromb/concentus/releases/latest

Положить в `Assets/Plugins/Concentus.dll`.

---

### Task 2: Скрипты голосового чата

**Files:**
- Create: `Assets/Scripts/Voice/VoiceManager.cs`
- Create: `Assets/Scripts/Voice/VoiceReceiver.cs`

#### VoiceManager.cs

Захватывает микрофон, сжимает Opus через Concentus, отправляет через NetworkManager.CustomMessagingManager.

```csharp
using UnityEngine;
using Unity.Netcode;
using Concentus.Structs;
using Concentus.Enums;
using System.Collections.Generic;

public class VoiceManager : NetworkBehaviour
{
    private AudioClip micClip;
    private OpusEncoder encoder;
    private int lastSamplePos;
    private const int sampleRate = 16000;
    private const int frameSize = 640; // 40ms @ 16kHz
    private const float sendInterval = 0.04f; // 40ms
    private float timer;

    void Awake()
    {
        encoder = new OpusEncoder(sampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = 24000;
        encoder.Complexity = 5;
    }

    void OnEnable()
    {
        if (!IsOwner) return;
        string device = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (device == null) return;
        micClip = Microphone.Start(device, true, 10, sampleRate);
    }

    void OnDisable()
    {
        if (micClip != null)
        {
            Microphone.End(Microphone.devices[0]);
            micClip = null;
        }
    }

    void Update()
    {
        if (!IsOwner || micClip == null || !Microphone.IsRecording(Microphone.devices[0]))
            return;

        timer += Time.deltaTime;
        if (timer < sendInterval) return;
        timer = 0;

        int currentPos = Microphone.GetPosition(Microphone.devices[0]);
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

        byte[] packet = new byte[len];
        System.Array.Copy(opus, packet, len);

        SendVoicePacketServerRpc(packet);
    }

    [ServerRpc]
    void SendVoicePacketServerRpc(byte[] data)
    {
        BroadcastVoiceClientRpc(data, new ClientRpcSendParams { TargetClientIds = GetNotOwnerIds() });
    }

    [ClientRpc]
    void BroadcastVoiceClientRpc(byte[] data, ClientRpcSendParams sendParams)
    {
        if (IsOwner) return;
        VoiceReceiver.Instance?.ReceiveVoice(data);
    }

    private List<ulong> GetNotOwnerIds()
    {
        var ids = new List<ulong>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != OwnerClientId)
                ids.Add(client.ClientId);
        }
        return ids;
    }
}
```

#### VoiceReceiver.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
using Concentus.Structs;

public class VoiceReceiver : MonoBehaviour
{
    public static VoiceReceiver Instance { get; private set; }
    public int sampleRate = 16000;

    private OpusDecoder decoder;
    private AudioSource audioSource;
    private int writePos;
    private int bufferLength = 48000; // 3 seconds
    private float[] audioBuffer;

    void Awake()
    {
        Instance = this;
        decoder = new OpusDecoder(sampleRate, 1);
        audioBuffer = new float[bufferLength];

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 1f;

        var clip = AudioClip.Create("VoiceStream", bufferLength, 1, sampleRate, false, OnAudioRead, OnAudioSetPosition);
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void ReceiveVoice(byte[] opusData)
    {
        short[] pcm = new short[640];
        int samples = decoder.Decode(opusData, 0, opusData.Length, pcm, 0, pcm.Length);

        for (int i = 0; i < samples; i++)
        {
            int idx = (writePos + i) % bufferLength;
            audioBuffer[idx] = pcm[i] / (float)short.MaxValue;
        }
        writePos = (writePos + samples) % bufferLength;
    }

    void OnAudioRead(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            int idx = (writePos - data.Length + i + bufferLength) % bufferLength;
            data[i] = audioBuffer[idx];
        }
    }

    void OnAudioSetPosition(int position) { }
}
```

---

### Task 3: NetworkPlayerController

**Files:**
- Modify: `Assets/Scripts/PlayerController.cs` (адаптировать под NetworkBehaviour)
- Create: `Assets/Scripts/NetworkPlayerController.cs`

```csharp
using Unity.Netcode;

public class NetworkPlayerController : NetworkBehaviour
{
    public PlayerController controller;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        controller.enabled = IsOwner;
        if (!IsOwner)
        {
            GetComponentInChildren<Camera>().enabled = false;
            GetComponentInChildren<AudioListener>().enabled = false;
        }

        if (IsOwner)
        {
            var voice = GetComponent<VoiceManager>();
            if (voice != null) voice.enabled = true;
        }
    }
}
```

PlayerController остаётся без изменений, только добавляется проверка `NetworkPlayerController` сверху.

---

### Task 4: NetworkManager + Server

**Files:**
- Create: `Assets/Scripts/NetworkManagerSetup.cs`

```csharp
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerSetup : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    void Awake()
    {
        var nm = NetworkManager.Singleton;
        nm.NetworkConfig.PlayerPrefab = playerPrefab;

#if UNITY_SERVER
        Debug.Log("Starting as server...");
        nm.StartServer();
#else
        string ip = PlayerPrefs.GetString("server_ip", "127.0.0.1");
        nm.StartClient();
#endif
    }
}
```

**Build settings:**
- Server: `Build Settings → Add Scenes → Build` с опцией `Server Build` в Player Settings
- Client: обычный standalone build

---

### Task 5: Сборка и тестирование

- [ ] **Step 1:** Собрать Server Build (headless)
- [ ] **Step 2:** Собрать Client Build
- [ ] **Step 3:** Запустить сервер
- [ ] **Step 4:** Запустить 2 клиента, проверить голос
- [ ] **Step 5:** Проверить, что в SceneRoots не конфликтуют NetworkObject
