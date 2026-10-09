using System;
using System.Collections.Generic;
using UnityEngine;

// Список игроков в лобби. Отдельного списка сервер не присылает, поэтому игрок
// считается присутствующим, пока от него приходят пакеты позиции или голоса.
public class Lobby : MonoBehaviour
{
    public const float TimeoutSeconds = 5f;
    const float SpeakingHoldSeconds = 0.3f;

    public class Player
    {
        public ulong Id;
        public float LastSeen;
        public float LastVoice = -100f;
        public bool Muted;
        public bool IsSpeaking => Time.unscaledTime - LastVoice < SpeakingHoldSeconds;
    }

    public static event Action<ulong> PlayerJoined;
    public static event Action<ulong> PlayerLeft;

    static readonly Dictionary<ulong, Player> players = new Dictionary<ulong, Player>();
    static readonly List<ulong> expired = new List<ulong>();

    public static ICollection<Player> Players => players.Values;

    public static bool IsMuted(ulong id) => players.TryGetValue(id, out var p) && p.Muted;

    // Сброс статики до загрузки сцены, чтобы компоненты сцены могли подписаться в Awake
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        players.Clear();
        PlayerJoined = null;
        PlayerLeft = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("Lobby");
        DontDestroyOnLoad(go);
        go.AddComponent<Lobby>();
    }

    void Start()
    {
        var nm = GameNet.Instance;
        if (nm != null)
            nm.OnPacketReceived += OnPacket;
    }

    void OnDestroy()
    {
        var nm = GameNet.Instance;
        if (nm != null)
            nm.OnPacketReceived -= OnPacket;
    }

    // 0x01 — голос, 0x02 — позиция; в обоих ID отправителя лежит в байтах 1..8
    private void OnPacket(ulong localId, byte[] data)
    {
        if (data.Length < 9 || (data[0] != 0x01 && data[0] != 0x02)) return;

        ulong id = BitConverter.ToUInt64(data, 1);
        if (id == localId) return;

        if (!players.TryGetValue(id, out var p))
        {
            p = new Player { Id = id };
            players.Add(id, p);
            Debug.Log($"[Lobby] player-{id} joined");
            PlayerJoined?.Invoke(id);
        }

        p.LastSeen = Time.unscaledTime;
    }

    // Клиенты шлют голос непрерывно, поэтому «говорит» определяет VoiceReceiver по громкости
    public static void MarkSpeaking(ulong id)
    {
        if (players.TryGetValue(id, out var p))
            p.LastVoice = Time.unscaledTime;
    }

    void Update()
    {
        float now = Time.unscaledTime;
        expired.Clear();
        foreach (var p in players.Values)
            if (now - p.LastSeen > TimeoutSeconds)
                expired.Add(p.Id);

        foreach (var id in expired)
        {
            players.Remove(id);
            Debug.Log($"[Lobby] player-{id} left");
            PlayerLeft?.Invoke(id);
        }
    }
}
