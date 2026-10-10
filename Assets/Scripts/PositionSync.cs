using System;
using System.Collections.Generic;
using UnityEngine;

public class PositionSync : MonoBehaviour
{
    public float sendInterval = 0.05f;

    [Tooltip("Модели для чужих игроков (выбирается по ID). Если пусто — копируется модель своего игрока")]
    public GameObject[] avatarPrefabs;
    [Tooltip("Масштаб моделей из avatarPrefabs")]
    public float avatarScale = 0.8f;
    [Tooltip("Скорость сглаживания позиции чужих игроков между пакетами")]
    public float smoothing = 15f;

    private class RemoteBody
    {
        public Transform Root;
        public ProceduralWalk Walk;
        public Vector3 TargetPos;
        public Quaternion TargetRot;
    }

    private static PositionSync instance;

    private Dictionary<string, RemoteBody> remoteBodies = new Dictionary<string, RemoteBody>(StringComparer.Ordinal);
    private PlayerController player;
    private GameObject avatarTemplate;
    private float timer;

    // Модель чужого игрока по его ID
    public static bool TryGetRemote(string id, out Transform root, out ProceduralWalk walk)
    {
        root = null; walk = null;
        if (instance == null || !instance.remoteBodies.TryGetValue(id, out var body) || body.Root == null) return false;
        root = body.Root;
        walk = body.Walk;
        return true;
    }

    void Awake() => instance = this;

    void Start()
    {
        player = GetComponent<PlayerController>();
        PrepareLocalAvatar();

        var nm = GameNet.Instance;
        if (nm != null)
            nm.OnPacketReceived += OnPacket;
        Lobby.PlayerLeft += OnPlayerLeft;
    }

    // Своей модели добавляем анимацию и делаем из неё неактивный шаблон для чужих игроков
    private void PrepareLocalAvatar()
    {
        var skin = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skin == null) return;

        Transform model = skin.transform;
        while (model.parent != transform) model = model.parent;

        if (model.GetComponent<ProceduralWalk>() == null)
            model.gameObject.AddComponent<ProceduralWalk>();

        avatarTemplate = Instantiate(model.gameObject);
        avatarTemplate.name = "RemoteAvatarTemplate";
        avatarTemplate.transform.localScale = model.lossyScale;
        avatarTemplate.SetActive(false);
    }

    // Игрок ушёл из лобби — убираем его аватар
    private void OnPlayerLeft(string id)
    {
        if (!remoteBodies.TryGetValue(id, out var body)) return;
        if (body.Root != null) Destroy(body.Root.gameObject);
        remoteBodies.Remove(id);
    }

    void Update()
    {
        SmoothRemoteBodies();

        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;

        timer += Time.deltaTime;
        if (timer < sendInterval) return;
        timer = 0;

        Vector3 pos = transform.position;
        float rotY = transform.eulerAngles.y;

        // 0x02 | idLen(1) | id | pos(12) | rotY(4) | flags(1): бит 0 — присед
        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(nm.LocalId);
        byte[] packet = new byte[1 + 1 + idBytes.Length + 12 + 4 + 1];
        packet[0] = 0x02;
        int off = IdCodec.Write(packet, 1, nm.LocalId);
        System.BitConverter.GetBytes(pos.x).CopyTo(packet, off);
        System.BitConverter.GetBytes(pos.y).CopyTo(packet, off + 4);
        System.BitConverter.GetBytes(pos.z).CopyTo(packet, off + 8);
        System.BitConverter.GetBytes(rotY).CopyTo(packet, off + 12);
        packet[off + 16] = (byte)(player != null && player.IsCrouching ? 1 : 0);

        nm.Send(packet);
    }

    // Пакеты приходят 20 раз в секунду — без сглаживания чужие игроки дёргаются
    private void SmoothRemoteBodies()
    {
        float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        foreach (var body in remoteBodies.Values)
        {
            if (body.Root == null) continue;
            if ((body.Root.position - body.TargetPos).sqrMagnitude > 25f)
                body.Root.position = body.TargetPos; // далеко (респаун) — телепортируем
            else
                body.Root.position = Vector3.Lerp(body.Root.position, body.TargetPos, k);
            body.Root.rotation = Quaternion.Slerp(body.Root.rotation, body.TargetRot, k);
        }
    }

    private void OnPacket(string senderId, byte[] data)
    {
        if (data.Length < 3 || data[0] != 0x02) return;

        string sourceId = IdCodec.Read(data, 1, out int off);
        if (sourceId == GameNet.Instance?.LocalId) return;
        if (data.Length < off + 17) return;

        Vector3 pos;
        pos.x = System.BitConverter.ToSingle(data, off);
        pos.y = System.BitConverter.ToSingle(data, off + 4);
        pos.z = System.BitConverter.ToSingle(data, off + 8);
        float rotY = System.BitConverter.ToSingle(data, off + 12);
        bool crouching = (data[off + 16] & 1) != 0;

        if (!remoteBodies.TryGetValue(sourceId, out var body))
        {
            body = CreateRemoteBody(sourceId, pos, rotY);
            remoteBodies.Add(sourceId, body);
        }

        body.TargetPos = pos;
        body.TargetRot = Quaternion.Euler(0, rotY, 0);
        if (body.Walk != null) body.Walk.crouching = crouching;
    }

    private RemoteBody CreateRemoteBody(string id, Vector3 pos, float rotY)
    {
        var rot = Quaternion.Euler(0, rotY, 0);
        GameObject go;

        if (avatarPrefabs != null && avatarPrefabs.Length > 0 && avatarPrefabs[IdCodec.StableIndex(id, avatarPrefabs.Length)] != null)
        {
            go = Instantiate(avatarPrefabs[IdCodec.StableIndex(id, avatarPrefabs.Length)], pos, rot);
            go.transform.localScale = Vector3.one * avatarScale;
            if (go.GetComponent<ProceduralWalk>() == null) go.AddComponent<ProceduralWalk>();
        }
        else if (avatarTemplate != null)
        {
            go = Instantiate(avatarTemplate, pos, rot);
            go.SetActive(true);
        }
        else
        {
            go = new GameObject();
            go.transform.SetPositionAndRotation(pos, rot);
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(go.transform, false);
            capsule.transform.localPosition = Vector3.up;
            Destroy(capsule.GetComponent<Collider>());
        }

        go.name = $"RemotePlayer_{id}";
        RemoteAvatar.Attach(go, id);
        return new RemoteBody
        {
            Root = go.transform,
            Walk = go.GetComponent<ProceduralWalk>(),
            TargetPos = go.transform.position,
            TargetRot = rot,
        };
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        var nm = GameNet.Instance;
        if (nm != null)
            nm.OnPacketReceived -= OnPacket;
        Lobby.PlayerLeft -= OnPlayerLeft;
        if (avatarTemplate != null) Destroy(avatarTemplate);
    }
}
