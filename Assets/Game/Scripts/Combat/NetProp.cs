using System;
using System.Collections.Generic;
using Game.Scripts.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class NetProp : MonoBehaviour
{
    public const byte PacketState = 0x04;
    public const byte PacketSound = 0x05;
    public const byte PacketManifest = 0x06;
    public const byte PacketPickupReq = 0x07;
    public const byte PacketRelease = 0x08;
    public const byte PacketManifestAck = 0x09;
    public const byte PacketPropGrant = 0x0A;
    public const byte PacketPropReleaseEvent = 0x0B;
    public const byte PacketPropSnapshot = 0x0C;

    const float SendInterval = 0.05f;
    const float RemoteTimeout = 1f;
    const float GrantTimeout = 0.5f;

    static readonly Dictionary<int, NetProp> props = new Dictionary<int, NetProp>();

    public int Id { get; private set; }

    /// Server-assigned prop id (-1 until MANIFEST_ACK).
    public int ServerId { get; private set; } = -1;

    public Rigidbody Body { get; private set; }
    public bool IsHeld { get; private set; }

    /// True after receiving PROP_GRANT from server.
    public bool IsServerOwned => serverOwned;
    bool serverOwned;

    /// True while waiting for PROP_GRANT after PICKUP_REQ.
    bool pendingGrant;
    float grantRequestTime;

    public string LastThrower { get; private set; }

    Collider[] colliders;
    Vector3 startPos;
    Quaternion startRot;
    float sendTimer, ownedSince, slowSince, lastSoundTime, lastRemotePacket = -10f;
    Vector3 targetPos;
    Quaternion targetRot;
    bool hasRemoteTarget;
    readonly Dictionary<string, float> lastHitTime = new Dictionary<string, float>(StringComparer.Ordinal);

    static readonly string[] Keywords = { "chair", "trashbin", "trash", "eraser", "notepad", "pencilholder" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SetupScene();
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        SetupScene();
    }

    static bool Matches(Transform t)
    {
        string n = t.name.ToLowerInvariant().Replace(" ", "").Replace("_", "");
        foreach (var k in Keywords)
            if (n.Contains(k)) return true;
        return false;
    }

    static void SetupScene()
    {
        props.Clear();
        var candidates = new List<Transform>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (!Matches(t) || t.GetComponentInParent<PlayerController>() != null) continue;
            if (t.parent != null && Matches(t.parent)) continue;
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
        rb.isKinematic = true;

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

    /// Local-only physics ownership (punch, push). Does NOT send to server.
    public void TakeOwnership(string thrower)
    {
        LastThrower = thrower;
        ownedSince = Time.time;
        slowSince = -1f;
        hasRemoteTarget = false;
        if (serverOwned) return;
        Body.isKinematic = false;
        Body.WakeUp();
    }

    // ---------- Manifest ----------

    /// Builds and sends 0x06 MANIFEST: [count:2][{keyLen:1|key|halfExtents:12|mass:4}*]
    public static void SendManifest()
    {
        var nm = GameNet.Instance;
        if (nm == null || props.Count == 0) return;

        var keys = new string[props.Count];
        var hf = new Vector3[props.Count];
        var masses = new float[props.Count];
        for (int i = 0; i < props.Count; i++)
        {
            var prop = props[i];
            keys[i] = prop.GetHierarchyPath();
            var box = prop.GetComponent<BoxCollider>();
            hf[i] = box != null ? box.size * 0.5f : Vector3.one * 0.5f;
            masses[i] = prop.Body.mass;
        }

        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(nm.LocalId ?? "");
        int size = 1 + idBytes.Length + 2;
        for (int i = 0; i < props.Count; i++)
        {
            byte[] kb = System.Text.Encoding.UTF8.GetBytes(keys[i]);
            size += 1 + kb.Length + 12 + 4;
        }
        var p = new byte[1 + size];
        p[0] = PacketManifest;
        int off = IdCodec.Write(p, 1, nm.LocalId);
        p[off++] = (byte)(props.Count & 0xFF);
        p[off++] = (byte)((props.Count >> 8) & 0xFF);
        for (int i = 0; i < props.Count; i++)
        {
            byte[] kb = System.Text.Encoding.UTF8.GetBytes(keys[i]);
            p[off++] = (byte)kb.Length;
            System.Array.Copy(kb, 0, p, off, kb.Length); off += kb.Length;
            WriteVec(p, off, hf[i]); off += 12;
            BitConverter.GetBytes(masses[i]).CopyTo(p, off); off += 4;
        }
        nm.Send(p);
        Debug.Log($"[NetProp] Manifest sent ({props.Count} props)");
    }

    /// Returns a stable hierarchy path from the root scene object.
    string GetHierarchyPath()
    {
        var parts = new List<string>();
        var t = transform;
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    // ---------- Network ----------

    /// Sends PICKUP_REQ (0x07) and starts holding optimistically.
    public void RequestPickup(Collider ignore)
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;

        // Optimistic hold.
        IsHeld = true;
        serverOwned = false;
        pendingGrant = true;
        grantRequestTime = Time.time;
        Body.isKinematic = true;
        SetIgnore(ignore, true);

        // Send 0x07 [id:str][propId:4]
        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(nm.LocalId ?? "");
        int sid = ServerId >= 0 ? ServerId : Id;
        var p = new byte[1 + 1 + idBytes.Length + 4];
        p[0] = PacketPickupReq;
        int off = IdCodec.Write(p, 1, nm.LocalId);
        BitConverter.GetBytes(sid).CopyTo(p, off);
        nm.Send(p);
        Debug.Log($"[NetProp] PICKUP_REQ prop={Id} serverId={sid}");
    }

    /// Sends PROP_RELEASE (0x08) and drops the prop.
    public void RequestRelease(Vector3 velocity, Vector3 angularVelocity, Collider ignore)
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;

        IsHeld = false;
        pendingGrant = false;
        serverOwned = false;
        Body.isKinematic = false;
        Body.linearVelocity = velocity;
        Body.angularVelocity = angularVelocity;
        StartCoroutine(RestoreCollision(ignore, 0.4f));

        // Send 0x08 [id:str][propId:4][vel:12][angVel:12][flags:1]
        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(nm.LocalId ?? "");
        int sid = ServerId >= 0 ? ServerId : Id;
        var p = new byte[1 + 1 + idBytes.Length + 4 + 12 + 12 + 1];
        p[0] = PacketRelease;
        int off = IdCodec.Write(p, 1, nm.LocalId);
        BitConverter.GetBytes(sid).CopyTo(p, off); off += 4;
        WriteVec(p, off, velocity); off += 12;
        WriteVec(p, off, angularVelocity); off += 12;
        p[off] = 0; // flags
        nm.Send(p);
        Debug.Log($"[NetProp] PROP_RELEASE prop={Id} serverId={sid}");
    }

    // ---------- Synced wielding ----------

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

    // ---------- Sync loop ----------

    void Update()
    {
        // Grant timeout rollback.
        if (pendingGrant && Time.time - grantRequestTime > GrantTimeout)
        {
            pendingGrant = false;
            if (!serverOwned)
            {
                IsHeld = false;
                Body.isKinematic = true;
                transform.SetPositionAndRotation(startPos, startRot);
                Debug.Log($"[NetProp] Grant timeout, rollback prop={Id}");
            }
        }

        if (transform.position.y < startPos.y - 50f && (serverOwned || !hasRemoteTarget))
        {
            Body.isKinematic = true;
            transform.SetPositionAndRotation(startPos, startRot);
            if (serverOwned) { Send(2); serverOwned = false; IsHeld = false; }
            return;
        }

        if (serverOwned)
        {
            if (!IsHeld && Settled()) { Send(2); serverOwned = false; Body.isKinematic = true; return; }
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

    bool Settled()
    {
        if (Time.time - ownedSince < 0.5f) return false;
        if (Time.time - ownedSince > 10f && serverOwned) return true;
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
        int sid = ServerId >= 0 ? ServerId : Id;
        var p = new byte[1 + 1 + idBytes.Length + 4 + 12 + 16 + 12 + 1];
        p[0] = PacketState;
        int off = IdCodec.Write(p, 1, nm.LocalId);
        BitConverter.GetBytes(sid).CopyTo(p, off); off += 4;
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
        if (data.Length < 2) return;

        switch (data[0])
        {
            case PacketManifestAck:
                HandleManifestAck(data);
                return;

            case PacketPropGrant:
                HandlePropGrant(data, localId);
                return;

            case PacketPropReleaseEvent:
                HandlePropReleaseEvent(data, localId);
                return;

            case PacketPropSnapshot:
                HandlePropSnapshot(data);
                return;

            case PacketState:
                HandlePropState(data, localId);
                return;
        }
    }

    void HandleManifestAck(byte[] data)
    {
        // [count:2][propId:4 * count]
        if (data.Length < 3) return;
        int count = data[1] | (data[2] << 8);
        if (data.Length < 3 + count * 4) return;

        for (int i = 0; i < count && i < props.Count; i++)
        {
            int serverId = BitConverter.ToInt32(data, 3 + i * 4);
            if (props.TryGetValue(i, out var prop))
                prop.ServerId = serverId;
        }
        Debug.Log($"[NetProp] MANIFEST_ACK: {count} props mapped");
    }

    void HandlePropGrant(byte[] data, string localId)
    {
        // [propId:4][holderId:str][flags:1]
        if (data.Length < 6) return;
        int sid = BitConverter.ToInt32(data, 1);
        var prop = FindByServerId(sid);
        if (prop == null) return;

        string holder = IdCodec.Read(data, 5, out _);
        bool isUs = holder == localId;

        if (isUs)
        {
            prop.pendingGrant = false;
            prop.serverOwned = true;
            prop.ownedSince = Time.time;
            prop.slowSince = -1f;
            prop.hasRemoteTarget = false;
            prop.Body.isKinematic = prop.IsHeld;
            if (!prop.IsHeld)
            {
                prop.Body.isKinematic = false;
                prop.Body.WakeUp();
            }
            Debug.Log($"[NetProp] PROP_GRANT: we own prop={prop.Id} held={prop.IsHeld}");
        }
        else
        {
            // Someone else took it — rollback if we were waiting.
            if (prop.pendingGrant)
            {
                prop.pendingGrant = false;
                prop.IsHeld = false;
                prop.Body.isKinematic = true;
                prop.transform.SetPositionAndRotation(prop.startPos, prop.startRot);
            }
            prop.serverOwned = false;
            prop.LastThrower = holder;
            Debug.Log($"[NetProp] PROP_GRANT: {holder} owns prop={prop.Id}");
        }
    }

    void HandlePropReleaseEvent(byte[] data, string localId)
    {
        // [propId:4][ownerId:str][vel:12][angVel:12][flags:1]
        if (data.Length < 5) return;
        int sid = BitConverter.ToInt32(data, 1);
        var prop = FindByServerId(sid);
        if (prop == null) return;

        string prevOwner = IdCodec.Read(data, 5, out int off);
        if (off + 25 > data.Length) return;

        Vector3 vel = ReadVec(data, off); off += 12;
        Vector3 angVel = ReadVec(data, off); off += 12;
        byte flags = data[off];
        bool isFree = string.IsNullOrEmpty(prevOwner) || (flags & 1) != 0;

        if (prevOwner == localId)
        {
            prop.serverOwned = false;
            prop.pendingGrant = false;
        }

        prop.IsHeld = false;

        if (isFree)
        {
            prop.serverOwned = false;
            prop.Body.isKinematic = true;
            prop.transform.SetPositionAndRotation(prop.startPos, prop.startRot);
        }
        else
        {
            // Remote owner released — follow the physics.
            prop.hasRemoteTarget = true;
            prop.lastRemotePacket = Time.time;
            prop.Body.isKinematic = true;
            prop.targetPos = prop.transform.position;
            prop.targetRot = prop.transform.rotation;
        }

        Debug.Log($"[NetProp] PROP_RELEASE_EVENT prop={prop.Id} owner={prevOwner} free={isFree}");
    }

    void HandlePropSnapshot(byte[] data)
    {
        // [count:2][{propId:4, ownerId:str, held:1}*]
        if (data.Length < 3) return;
        int count = data[1] | (data[2] << 8);
        int off = 3;
        for (int i = 0; i < count; i++)
        {
            if (off + 5 > data.Length) break;
            int sid = BitConverter.ToInt32(data, off); off += 4;
            string owner = IdCodec.Read(data, off, out off);
            if (off >= data.Length) break;
            byte held = data[off++];
            var prop = FindByServerId(sid);
            if (prop == null) continue;
            prop.serverOwned = false;
            prop.pendingGrant = false;
            prop.IsHeld = held != 0;
            prop.Body.isKinematic = true;
            prop.hasRemoteTarget = false;
            prop.transform.SetPositionAndRotation(prop.startPos, prop.startRot);
        }
        Debug.Log($"[NetProp] PROP_SNAPSHOT: {count} entries");
    }

    void HandlePropState(byte[] data, string localId)
    {
        if (data.Length < 3) return;
        string sender = IdCodec.Read(data, 1, out int off);
        if (sender == localId) return;
        if (data.Length < off + 4) return;
        int sid = BitConverter.ToInt32(data, off); off += 4;
        var prop = FindByServerId(sid);
        if (prop == null) return;

        if (data.Length < off + 41) return;

        if (prop.IsHeld) return;
        prop.serverOwned = false;
        prop.Body.isKinematic = true;

        prop.targetPos = ReadVec(data, off); off += 12;
        prop.targetRot = new Quaternion(BitConverter.ToSingle(data, off), BitConverter.ToSingle(data, off + 4),
                                        BitConverter.ToSingle(data, off + 8), BitConverter.ToSingle(data, off + 12));
        off += 16;
        off += 12;
        byte flags = data[off];
        prop.lastRemotePacket = Time.time;
        prop.hasRemoteTarget = (flags & 2) == 0;
        prop.LastThrower = sender;
        if ((flags & 2) != 0) prop.transform.SetPositionAndRotation(prop.targetPos, prop.targetRot);

        if ((flags & 1) != 0 && PositionSync.TryGetRemote(sender, out _, out var walk) && walk != null)
            walk.holdUntil = Time.time + 0.3f;
    }

    static NetProp FindByServerId(int sid)
    {
        foreach (var kv in props)
            if (kv.Value.ServerId == sid) return kv.Value;
        return null;
    }

    // ---------- Collisions ----------

    void OnCollisionEnter(Collision col)
    {
        if (!serverOwned || IsHeld) return;
        float speed = col.relativeVelocity.magnitude;

        var other = Find(col.collider);
        if (other != null && !other.serverOwned && speed > 1f)
        {
            other.LastThrower = LastThrower;
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
