using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Предмет, который можно толкать, бить, брать и кидать (стулья, урны и т.п.).
// Физику считает только «владелец» — клиент, который последним трогал предмет; он рассылает позицию.
// У остальных предмет кинематический и плавно едет за пакетами. Успокоился — владелец отпускает его.
[RequireComponent(typeof(Rigidbody))]
public class NetProp : MonoBehaviour
{
    // 0x04 | idLen(1) | id | propId(4) | pos(12) | rot(16) | vel(12) | flags(1): бит 0 — в руках, бит 1 — остановился
    public const byte PacketState = 0x04;
    // 0x05 | idLen(1) | id | sfx(1) | pos(12) | volume(4)
    public const byte PacketSound = 0x05;

    const float SendInterval = 0.05f;
    const float RemoteTimeout = 1f;

    static readonly Dictionary<int, NetProp> props = new Dictionary<int, NetProp>();

    public int Id { get; private set; }
    public Rigidbody Body { get; private set; }
    public bool IsOwned { get; private set; }
    public bool IsHeld { get; private set; }
    // Кто кинул/толкнул предмет — чтобы засчитать попадание в игрока
    public string LastThrower { get; private set; }

    Collider[] colliders;
    Vector3 startPos;
    Quaternion startRot;
    float sendTimer, ownedSince, slowSince, lastSoundTime, lastRemotePacket = -10f;
    Vector3 targetPos;
    Quaternion targetRot;
    bool hasRemoteTarget;
    readonly Dictionary<string, float> lastHitTime = new Dictionary<string, float>(StringComparer.Ordinal);

    // ---------- Подготовка сцены ----------

    // Части имени объекта (без пробелов и _), по которым он становится бросаемым
    static readonly string[] Keywords = { "chair", "trashbin", "trash", "eraser", "notepad", "pencilholder" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SetupScene();
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m) => SetupScene();

    static bool Matches(Transform t)
    {
        string n = t.name.ToLowerInvariant().Replace(" ", "").Replace("_", "");
        foreach (var k in Keywords)
            if (n.Contains(k)) return true;
        return false;
    }

    // Находит в сцене стулья и мелкие предметы и делает их физическими.
    // ID выдаются по позиции, поэтому совпадают у всех клиентов с одной и той же сценой.
    static void SetupScene()
    {
        props.Clear();
        var candidates = new List<Transform>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (!Matches(t) || t.GetComponentInParent<PlayerController>() != null) continue;
            if (t.parent != null && Matches(t.parent)) continue; // колёса стула и т.п. — часть родителя
            if (t.GetComponentInChildren<MeshFilter>() == null) continue;
            candidates.Add(t);
        }

        candidates.Sort((a, b) =>
        {
            Vector3Int pa = Vector3Int.RoundToInt(a.position * 100f), pb = Vector3Int.RoundToInt(b.position * 100f);
            int c = pa.x.CompareTo(pb.x);
            if (c == 0) c = pa.z.CompareTo(pb.z);
            if (c == 0) c = pa.y.CompareTo(pb.y);
            if (c == 0) c = string.CompareOrdinal(a.name, b.name);
            return c;
        });

        for (int i = 0; i < candidates.Count; i++)
        {
            var prop = candidates[i].GetComponent<NetProp>();
            if (prop == null) prop = MakeProp(candidates[i].gameObject);
            prop.Id = i;
            props[i] = prop;
        }
        if (candidates.Count > 0) Debug.Log($"[NetProp] Physical props: {candidates.Count}");
    }

    static NetProp MakeProp(GameObject go)
    {
        // Невыпуклые MeshCollider нельзя на динамическом теле — заменяем одной коробкой по габаритам
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;

        var root = go.transform;
        var bounds = new Bounds();
        bool first = true;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var b = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = root.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                else bounds.Encapsulate(p);
            }
        }

        var box = go.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = Vector3.Max(bounds.size, Vector3.one * 0.02f);

        Vector3 worldSize = Vector3.Scale(box.size, root.lossyScale);
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.mass = Mathf.Clamp(Mathf.Abs(worldSize.x * worldSize.y * worldSize.z) * 25f, 0.3f, 10f);
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.3f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = true; // спит, пока его не тронули — иначе задвинутые под стол стулья разлетятся

        return go.AddComponent<NetProp>();
    }

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void OnEnable()
    {
        var nm = GameNet.Instance;
        if (nm != null) nm.OnPacketReceived += OnPacket;
    }

    void OnDisable()
    {
        var nm = GameNet.Instance;
        if (nm != null) nm.OnPacketReceived -= OnPacket;
    }

    public static NetProp Find(Collider c) => c != null ? c.GetComponentInParent<NetProp>() : null;

    // ---------- Управление ----------

    // Начать считать физику у себя (толкнули, ударили, взяли)
    public void TakeOwnership(string thrower)
    {
        LastThrower = thrower;
        ownedSince = Time.time;
        slowSince = -1f;
        hasRemoteTarget = false;
        if (IsOwned) return;
        IsOwned = true;
        Body.isKinematic = false;
        Body.WakeUp();
    }

    public void Pickup(string holder, Collider ignore)
    {
        TakeOwnership(holder);
        IsHeld = true;
        Body.isKinematic = true;
        SetIgnore(ignore, true);
    }

    // Отпустить из рук с заданной скоростью (0 — просто положить)
    public void Release(Vector3 velocity, Vector3 angular, Collider ignore)
    {
        IsHeld = false;
        ownedSince = Time.time;
        Body.isKinematic = false;
        Body.linearVelocity = velocity;
        Body.angularVelocity = angular;
        // Сначала дать предмету отлететь, потом вернуть столкновения с бросившим
        StartCoroutine(RestoreCollision(ignore, 0.4f));
    }

    public void MoveHeld(Vector3 pos, Quaternion rot)
    {
        Body.MovePosition(pos);
        Body.MoveRotation(rot);
    }

    System.Collections.IEnumerator RestoreCollision(Collider c, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!IsHeld) SetIgnore(c, false);
    }

    void SetIgnore(Collider other, bool ignore)
    {
        if (other == null) return;
        foreach (var c in colliders)
            if (c != null && c.enabled) Physics.IgnoreCollision(c, other, ignore);
    }

    // ---------- Синхронизация ----------

    void Update()
    {
        if (transform.position.y < startPos.y - 50f && (IsOwned || !hasRemoteTarget))
        {
            // Улетел за карту — вернуть на место
            Body.isKinematic = true;
            transform.SetPositionAndRotation(startPos, startRot);
            if (IsOwned) { Send(2); IsOwned = false; IsHeld = false; }
            return;
        }

        if (IsOwned)
        {
            if (!IsHeld && Settled()) { Send(2); IsOwned = false; Body.isKinematic = true; return; }
            sendTimer += Time.deltaTime;
            if (sendTimer >= SendInterval) { sendTimer = 0; Send((byte)(IsHeld ? 1 : 0)); }
        }
        else if (hasRemoteTarget)
        {
            float k = 1f - Mathf.Exp(-15f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, k);
            if (Time.time - lastRemotePacket > RemoteTimeout) hasRemoteTarget = false;
        }
    }

    // Предмет остановился (или его гоняют слишком долго) — отпускаем владение
    bool Settled()
    {
        if (Time.time - ownedSince < 0.5f) return false;
        if (Time.time - ownedSince > 10f) return true;
        if (Body.IsSleeping()) return true;
        bool slow = Body.linearVelocity.sqrMagnitude < 0.01f && Body.angularVelocity.sqrMagnitude < 0.04f;
        if (!slow) { slowSince = -1f; return false; }
        if (slowSince < 0) slowSince = Time.time;
        return Time.time - slowSince > 0.4f;
    }

    void Send(byte flags)
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;
        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(nm.LocalId ?? "");
        var p = new byte[1 + 1 + idBytes.Length + 4 + 12 + 16 + 12 + 1];
        p[0] = PacketState;
        int off = IdCodec.Write(p, 1, nm.LocalId);
        BitConverter.GetBytes(Id).CopyTo(p, off); off += 4;
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        Vector3 vel = Body.isKinematic ? Vector3.zero : Body.linearVelocity;
        WriteVec(p, off, pos); off += 12;
        BitConverter.GetBytes(rot.x).CopyTo(p, off); off += 4;
        BitConverter.GetBytes(rot.y).CopyTo(p, off); off += 4;
        BitConverter.GetBytes(rot.z).CopyTo(p, off); off += 4;
        BitConverter.GetBytes(rot.w).CopyTo(p, off); off += 4;
        WriteVec(p, off, vel); off += 12;
        p[off] = flags;
        nm.Send(p);
    }

    void OnPacket(string localId, byte[] data)
    {
        if (data.Length < 3 || data[0] != PacketState) return;
        string sender = IdCodec.Read(data, 1, out int off);
        if (sender == localId) return;
        if (data.Length < off + 4) return;
        if (BitConverter.ToInt32(data, off) != Id) return;
        off += 4;

        if (data.Length < off + 41) return; // pos(12) + rot(16) + vel(12) + flags(1)

        if (IsHeld) return; // у нас в руках — не отдаём
        IsOwned = false;
        Body.isKinematic = true;

        targetPos = ReadVec(data, off); off += 12;
        targetRot = new Quaternion(BitConverter.ToSingle(data, off), BitConverter.ToSingle(data, off + 4),
                                   BitConverter.ToSingle(data, off + 8), BitConverter.ToSingle(data, off + 12));
        off += 16;
        off += 12; // vel не используется приёмником
        byte flags = data[off];
        lastRemotePacket = Time.time;
        hasRemoteTarget = (flags & 2) == 0;
        LastThrower = sender;
        if ((flags & 2) != 0) transform.SetPositionAndRotation(targetPos, targetRot);

        // Чужой игрок держит предмет — поднять ему руки
        if ((flags & 1) != 0 && PositionSync.TryGetRemote(sender, out _, out var walk) && walk != null)
            walk.holdUntil = Time.time + 0.3f;
    }

    // ---------- Удары о мир ----------

    void OnCollisionEnter(Collision col)
    {
        if (!IsOwned || IsHeld) return;
        float speed = col.relativeVelocity.magnitude;

        // Толкнуть другой предмет, в который врезались
        var other = Find(col.collider);
        if (other != null && !other.IsOwned && speed > 1f)
        {
            other.TakeOwnership(LastThrower);
            other.Body.AddForceAtPosition(-col.relativeVelocity * Body.mass * 0.5f, col.GetContact(0).point, ForceMode.Impulse);
        }

        if (speed > 1.5f && Time.time - lastSoundTime > 0.12f)
        {
            lastSoundTime = Time.time;
            float vol = Mathf.Clamp01(speed / 8f);
            Vector3 point = col.GetContact(0).point;
            GameSounds.Play(Sfx.Impact, point, vol, Mathf.Lerp(1.3f, 0.8f, Mathf.Clamp01(Body.mass / 8f)));
            BroadcastSound(Sfx.Impact, point, vol);
        }

        // Попали в другого игрока
        var avatar = col.collider.GetComponentInParent<RemoteAvatar>();
        if (avatar != null && speed > 3f && !string.IsNullOrEmpty(LastThrower))
        {
            if (lastHitTime.TryGetValue(avatar.Id, out float t) && Time.time - t < 0.5f) return;
            lastHitTime[avatar.Id] = Time.time;
            Vector3 dir = Body.linearVelocity.sqrMagnitude > 0.01f ? Body.linearVelocity.normalized : -col.relativeVelocity.normalized;
            dir.y = Mathf.Max(dir.y, 0.25f);
            float force = Mathf.Clamp(speed * Body.mass * 0.25f, 3f, 12f);
            Vector3 point = col.GetContact(0).point;
            GameSounds.Play(Sfx.Punch, point, 1f, 0.8f);
            PlayerCombat.SendHit(PlayerCombat.AttackKind.Prop, avatar.Id, point, dir.normalized * force);
        }
    }

    public static void BroadcastSound(Sfx sfx, Vector3 pos, float volume)
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;
        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(nm.LocalId ?? "");
        var p = new byte[1 + 1 + idBytes.Length + 1 + 12 + 4];
        p[0] = PacketSound;
        int off = IdCodec.Write(p, 1, nm.LocalId);
        p[off++] = (byte)sfx;
        WriteVec(p, off, pos); off += 12;
        BitConverter.GetBytes(volume).CopyTo(p, off);
        nm.Send(p);
    }

    public static void WriteVec(byte[] p, int offset, Vector3 v)
    {
        BitConverter.GetBytes(v.x).CopyTo(p, offset);
        BitConverter.GetBytes(v.y).CopyTo(p, offset + 4);
        BitConverter.GetBytes(v.z).CopyTo(p, offset + 8);
    }

    public static Vector3 ReadVec(byte[] p, int offset) =>
        new Vector3(BitConverter.ToSingle(p, offset), BitConverter.ToSingle(p, offset + 4), BitConverter.ToSingle(p, offset + 8));
}
