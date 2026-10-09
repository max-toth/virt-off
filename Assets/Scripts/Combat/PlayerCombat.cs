using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Драка и предметы для своего игрока:
//   ЛКМ — удар кулаком, ПКМ — лещ,
//   E — взять предмет (стул и т.п.); с предметом в руках ЛКМ — бросить, E/ПКМ — положить.
// Попадание считает тот, кто бьёт, и рассылает пакет; жертва у себя получает отброс.
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    // 0x03 | attackerId(8) | kind(1) | targetId(8, 0 — мимо) | point(12) | impulse(12)
    public const byte PacketAttack = 0x03;

    public enum AttackKind : byte { Punch, Slap, Prop }

    [Header("Удары")]
    public float reach = 1.7f;
    public float punchCooldown = 0.45f;
    public float slapCooldown = 0.6f;
    [Tooltip("Отброс противника, м/с")]
    public float punchForce = 6f;
    public float slapForce = 3.5f;
    [Tooltip("Сила удара по предметам")]
    public float propHitImpulse = 4f;

    [Header("Предметы")]
    public float pickupDistance = 2.5f;
    public float holdDistance = 1.3f;
    public float throwSpeed = 13f;

    PlayerController player;
    CharacterController controller;
    Camera cam;
    ProceduralWalk walk;
    NetProp held;
    Quaternion heldRotOffset;
    float nextAttackTime;
    NetProp aimedProp;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
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
        if (held != null) Drop(Vector3.zero);
    }

    static ulong LocalId => GameNet.Instance != null ? GameNet.Instance.LocalId : 0;

    void Update()
    {
        if (walk == null) walk = GetComponentInChildren<ProceduralWalk>();

        var kb = Keyboard.current;
        var ms = Mouse.current;
        aimedProp = null;
        if (kb == null || ms == null || !player.enabled || GameMenu.IsOpen) return;

        if (held != null)
        {
            if (ms.leftButton.wasPressedThisFrame) Throw();
            else if (kb.eKey.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame) Drop(controller.velocity);
            return;
        }

        aimedProp = AimProp();
        if (kb.eKey.wasPressedThisFrame && aimedProp != null) { Pickup(aimedProp); return; }

        if (Time.time < nextAttackTime) return;
        if (ms.leftButton.wasPressedThisFrame) Attack(AttackKind.Punch);
        else if (ms.rightButton.wasPressedThisFrame) Attack(AttackKind.Slap);
    }

    void LateUpdate()
    {
        if (held == null) return;
        if (held.IsOwned == false) { held = null; return; } // предмет отобрали
        Vector3 pos = cam.transform.position + cam.transform.forward * holdDistance - Vector3.up * 0.35f;
        Quaternion rot = Quaternion.Euler(0, transform.eulerAngles.y, 0) * heldRotOffset;
        held.MoveHeld(pos, rot);
        if (walk != null) walk.holdUntil = Time.time + 0.1f;
    }

    // ---------- Удары ----------

    void Attack(AttackKind kind)
    {
        nextAttackTime = Time.time + (kind == AttackKind.Punch ? punchCooldown : slapCooldown);
        if (walk != null) walk.PlayAttack(kind == AttackKind.Slap);
        player.Shake(kind == AttackKind.Punch ? 1.5f : 1f);

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;
        ulong target = 0;
        Vector3 point = origin + dir * reach;
        Vector3 impulse = Vector3.zero;
        bool hitSomething = false;

        var hits = Physics.SphereCastAll(origin, 0.25f, dir, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (held != null && NetProp.Find(h.collider) == held) continue;
            hitSomething = true;
            point = h.distance > 0 ? h.point : h.collider.ClosestPoint(origin);

            var avatar = h.collider.GetComponentInParent<RemoteAvatar>();
            if (avatar != null)
            {
                target = avatar.Id;
                Vector3 flat = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
                impulse = kind == AttackKind.Punch
                    ? flat * punchForce + Vector3.up * 2.5f
                    : flat * slapForce + transform.right * slapForce * 0.6f + Vector3.up * 1f;
                break;
            }

            var prop = NetProp.Find(h.collider);
            if (prop != null && !prop.IsHeld)
            {
                prop.TakeOwnership(LocalId);
                float k = kind == AttackKind.Punch ? 1f : 0.6f;
                prop.Body.AddForceAtPosition(dir * propHitImpulse * k * Mathf.Max(1f, prop.Body.mass * 0.5f), point, ForceMode.Impulse);
            }
            break;
        }

        // Звук: по человеку — удар/лещ, по предмету/стене — тише, мимо — удар по воздуху
        if (target != 0)
            GameSounds.Play(kind == AttackKind.Punch ? Sfx.Punch : Sfx.Slap, point);
        else if (hitSomething)
            GameSounds.Play(kind == AttackKind.Punch ? Sfx.Punch : Sfx.Slap, point, 0.5f, 1.2f);
        else
            GameSounds.Play(Sfx.Miss, point, 0.6f);

        SendHit(kind, target, hitSomething ? point : origin, impulse, hitSomething && target == 0);
    }

    public static void SendHit(AttackKind kind, ulong target, Vector3 point, Vector3 impulse, bool hitObject = false)
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;
        var p = new byte[43];
        p[0] = PacketAttack;
        BitConverter.GetBytes(nm.LocalId).CopyTo(p, 1);
        p[9] = (byte)kind;
        BitConverter.GetBytes(target).CopyTo(p, 10);
        NetProp.WriteVec(p, 18, point);
        NetProp.WriteVec(p, 30, impulse);
        p[42] = (byte)(hitObject ? 1 : 0);
        nm.Send(p);
    }

    void OnPacket(ulong localId, byte[] data)
    {
        if (data.Length < 1) return;
        if (data[0] == NetProp.PacketSound && data.Length >= 26)
        {
            if (BitConverter.ToUInt64(data, 1) == localId) return;
            GameSounds.Play((Sfx)data[9], NetProp.ReadVec(data, 10), BitConverter.ToSingle(data, 22));
            return;
        }
        if (data[0] != PacketAttack || data.Length < 43) return;

        ulong attacker = BitConverter.ToUInt64(data, 1);
        if (attacker == localId) return;
        var kind = (AttackKind)data[9];
        ulong target = BitConverter.ToUInt64(data, 10);
        Vector3 point = NetProp.ReadVec(data, 18);
        Vector3 impulse = NetProp.ReadVec(data, 30);
        bool hitObject = data[42] != 0;

        // Замах у ударившего (для брошенного предмета замаха нет)
        if (kind != AttackKind.Prop && PositionSync.TryGetRemote(attacker, out _, out var attackerWalk) && attackerWalk != null)
            attackerWalk.PlayAttack(kind == AttackKind.Slap);

        if (kind == AttackKind.Prop) GameSounds.Play(Sfx.Punch, point, 1f, 0.8f);
        else if (target != 0) GameSounds.Play(kind == AttackKind.Punch ? Sfx.Punch : Sfx.Slap, point);
        else if (hitObject) GameSounds.Play(kind == AttackKind.Punch ? Sfx.Punch : Sfx.Slap, point, 0.5f, 1.2f);
        else GameSounds.Play(Sfx.Miss, point, 0.6f);

        if (target == localId && target != 0)
        {
            // Нас ударили
            player.AddImpulse(impulse);
            player.Shake(kind == AttackKind.Slap ? 4f : 6f);
            if (walk != null) walk.PlayHit();
            if (held != null) Drop(impulse * 0.5f); // от удара роняем предмет
        }
        else if (target != 0 && PositionSync.TryGetRemote(target, out _, out var victimWalk) && victimWalk != null)
            victimWalk.PlayHit();
    }

    // ---------- Предметы ----------

    NetProp AimProp()
    {
        var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, pickupDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            return NetProp.Find(h.collider);
        }
        return null;
    }

    void Pickup(NetProp prop)
    {
        held = prop;
        heldRotOffset = Quaternion.Inverse(Quaternion.Euler(0, transform.eulerAngles.y, 0)) * prop.transform.rotation;
        prop.Pickup(LocalId, controller);
    }

    void Throw()
    {
        var prop = held;
        Drop(cam.transform.forward * throwSpeed + controller.velocity);
        prop.Body.angularVelocity = UnityEngine.Random.insideUnitSphere * 6f;
        if (walk != null) walk.PlayAttack(false);
        GameSounds.Play(Sfx.Whoosh, prop.transform.position, 0.8f, 0.85f);
        NetProp.BroadcastSound(Sfx.Whoosh, prop.transform.position, 0.8f);
    }

    void Drop(Vector3 velocity)
    {
        var prop = held;
        held = null;
        if (prop != null) prop.Release(velocity, Vector3.zero, controller);
    }

    void OnGUI()
    {
        if (!player.enabled || GameMenu.IsOpen) return;
        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        GUI.Label(new Rect(cx - 5, cy - 11, 20, 20), "+");
        string hint = held != null ? "ЛКМ — бросить, E — положить"
                    : aimedProp != null ? "E — взять" : null;
        if (hint != null)
            GUI.Label(new Rect(cx - 100, cy + 20, 200, 22), hint, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter });
    }
}
