using System.Collections.Generic;
using UnityEngine;

public class PositionSync : MonoBehaviour
{
    public float sendInterval = 0.05f;

    private Dictionary<ulong, Transform> remoteBodies = new Dictionary<ulong, Transform>();
    private float timer;

    void Start()
    {
        var nm = GameNet.Instance;
        if (nm != null)
            nm.OnPacketReceived += OnPacket;
    }

    void Update()
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;

        timer += Time.deltaTime;
        if (timer < sendInterval) return;
        timer = 0;

        Vector3 pos = transform.position;
        float rotY = transform.eulerAngles.y;

        byte[] packet = new byte[1 + 8 + 12 + 4];
        packet[0] = 0x02;
        System.BitConverter.GetBytes(nm.LocalId).CopyTo(packet, 1);
        System.BitConverter.GetBytes(pos.x).CopyTo(packet, 9);
        System.BitConverter.GetBytes(pos.y).CopyTo(packet, 13);
        System.BitConverter.GetBytes(pos.z).CopyTo(packet, 17);
        System.BitConverter.GetBytes(rotY).CopyTo(packet, 21);

        nm.Send(packet);
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

        if (remoteBodies.TryGetValue(sourceId, out var body))
        {
            body.position = pos;
            body.rotation = Quaternion.Euler(0, rotY, 0);
        }
        else
        {
            CreateRemoteBody(sourceId, pos, rotY);
        }
    }

    private void CreateRemoteBody(ulong id, Vector3 pos, float rotY)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = $"RemotePlayer_{id}";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.5f;
        go.transform.rotation = Quaternion.Euler(0, rotY, 0);
        Destroy(go.GetComponent<Collider>());
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.material.color = Color.green;

        remoteBodies.Add(id, go.transform);
    }

    void OnDestroy()
    {
        var nm = GameNet.Instance;
        if (nm != null)
            nm.OnPacketReceived -= OnPacket;
    }
}
