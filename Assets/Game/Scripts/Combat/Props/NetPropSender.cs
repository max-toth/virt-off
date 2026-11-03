using Game.Scripts.Combat.Props.Models;
using Game.Scripts.Net;
using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    public static class NetPropSender
    {
        private const byte ReleaseFlagsNone = 0;

        public static bool IsConnected => GameNet.TryGetConnected(out _);

        // MANIFEST: [type][sender:str][count:2]{[key:str][halfExtents:12][mass:4]}*
        public static void SendManifest()
        {
            var manager = GameNet.Instance;
            var props = NetPropRegistry.All;
            if (manager == null || props.Count == 0) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.PropManifest);
            writer.WriteString(manager.LocalId);
            writer.WriteUInt16(props.Count);
            foreach (var prop in props)
            {
                writer.WriteString(GetHierarchyPath(prop.transform));
                writer.WriteVector3(GetHalfExtents(prop));
                writer.WriteSingle(prop.Body.mass);
            }

            writer.SendTo(manager);
            NetPropDiagnostics.Log($"Manifest sent ({props.Count} props)");
        }

        // PICKUP_REQ: [type][sender:str][propId:4]
        public static void SendPickupRequest(NetProp prop)
        {
            if (!GameNet.TryGetConnected(out var manager)) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.PropPickupRequest);
            writer.WriteString(manager.LocalId);
            writer.WriteInt32(prop.NetworkId);
            writer.SendTo(manager);
        }

        // PROP_RELEASE: [type][sender:str][propId:4][velocity:12][angularVelocity:12][flags:1]
        public static void SendRelease(NetProp prop, Vector3 velocity, Vector3 angularVelocity)
        {
            if (!GameNet.TryGetConnected(out var manager)) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.PropRelease);
            writer.WriteString(manager.LocalId);
            writer.WriteInt32(prop.NetworkId);
            writer.WriteVector3(velocity);
            writer.WriteVector3(angularVelocity);
            writer.WriteByte(ReleaseFlagsNone);
            writer.SendTo(manager);
        }

        // PROP_STATE: [type][sender:str][propId:4][position:12][rotation:16][velocity:12][flags:1]
        public static void SendState(NetProp prop, PropStateFlags flags)
        {
            if (!GameNet.TryGetConnected(out var manager)) return;

            prop.transform.GetPositionAndRotation(out var position, out var rotation);
            var body = prop.Body;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.PropState);
            writer.WriteString(manager.LocalId);
            writer.WriteInt32(prop.NetworkId);
            writer.WriteVector3(position);
            writer.WriteQuaternion(rotation);
            writer.WriteVector3(body.isKinematic ? Vector3.zero : body.linearVelocity);
            writer.WriteByte((byte)flags);
            writer.SendTo(manager);
        }

        private static string GetHierarchyPath(Transform target)
        {
            var path = target.name;
            for (var parent = target.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }

        private static Vector3 GetHalfExtents(NetProp prop) =>
            prop.TryGetComponent(out BoxCollider box) ? box.size * 0.5f : Vector3.one * 0.5f;
    }
}
