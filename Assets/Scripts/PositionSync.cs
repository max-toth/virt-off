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

    private Dictionary<ulong, RemoteBody> remoteBodies = new Dictionary<ulong, RemoteBody>();
    private PlayerController player;
    private GameObject avatarTemplate;
    private float timer;

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
    private void OnPlayerLeft(ulong id)
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

        // 0x02 | id(8) | pos(12) | rotY(4) | flags(1): бит 0 — присед
        byte[] packet = new byte[1 + 8 + 12 + 4 + 1];
        packet[0] = 0x02;
        System.BitConverter.GetBytes(nm.LocalId).CopyTo(packet, 1);
        System.BitConverter.GetBytes(pos.x).CopyTo(packet, 9);
        System.BitConverter.GetBytes(pos.y).CopyTo(packet, 13);
        System.BitConverter.GetBytes(pos.z).CopyTo(packet, 17);
        System.BitConverter.GetBytes(rotY).CopyTo(packet, 21);
        packet[25] = (byte)(player != null && player.IsCrouching ? 1 : 0);

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

    private void OnPacket(ulong senderId, byte[] data)
    {
        if (data.Length < 25 || data[0] != 0x02) return;

        ulong sourceId = System.BitConverter.ToUInt64(data, 1);
        if (sourceId == GameNet.Instance?.LocalId) return;

        Vector3 pos;
        pos.x = System.BitConverter.ToSingle(data, 9);
        pos.y = System.BitConverter.ToSingle(data, 13);
        pos.z = System.BitConverter.ToSingle(data, 17);
        float rotY = System.BitConverter.ToSingle(data, 21);
        bool crouching = data.Length > 25 && (data[25] & 1) != 0;

        if (!remoteBodies.TryGetValue(sourceId, out var body))
        {
            body = CreateRemoteBody(sourceId, pos, rotY);
            remoteBodies.Add(sourceId, body);
        }

        body.TargetPos = pos;
        body.TargetRot = Quaternion.Euler(0, rotY, 0);
        if (body.Walk != null) body.Walk.crouching = crouching;
    }

    private RemoteBody CreateRemoteBody(ulong id, Vector3 pos, float rotY)
    {
        var rot = Quaternion.Euler(0, rotY, 0);
        GameObject go;

        if (avatarPrefabs != null && avatarPrefabs.Length > 0 && avatarPrefabs[id % (ulong)avatarPrefabs.Length] != null)
        {
            go = Instantiate(avatarPrefabs[id % (ulong)avatarPrefabs.Length], pos, rot);
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
        var nm = GameNet.Instance;
        if (nm != null)
            nm.OnPacketReceived -= OnPacket;
        Lobby.PlayerLeft -= OnPlayerLeft;
        if (avatarTemplate != null) Destroy(avatarTemplate);
    }
}
