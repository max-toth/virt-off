using System.Collections.Generic;
using Game.Scripts.Combat.Props.Models;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    public static class NetPropSender
    {
        private const byte ReleaseFlagsNone = 0;

        private static readonly List<PropManifestEntry> Manifest = new();
        private static INetTransport _transport;
        private static bool _manifestSent;

        public static bool IsConnected => _transport is { IsConnected: true };

        public static string LocalId => _transport?.LocalId;

        /// <summary>Манифест предметов сцены уйдёт на сервер, как только тот выдаст id.</summary>
        public static void Initialize(INetTransport transport)
        {
            UnsubscribeIdAssigned();
            _transport = transport;
            if (transport != null) transport.IdAssigned += OnIdAssigned;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Shutdown()
        {
            UnsubscribeIdAssigned();
            _transport = null;
            _manifestSent = false;
        }

        // MANIFEST: [type][sender:str][count:2]{[key:str][halfExtents:12][mass:4]}*
        public static void SendManifest(IReadOnlyList<PropManifestEntry> entries)
        {
            if (_transport is not { IsConnected: true } transport || entries.Count == 0) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.PropManifest);
            writer.WriteString(transport.LocalId);
            writer.WriteUInt16(entries.Count);
            foreach (var entry in entries)
            {
                writer.WriteString(entry.Key);
                writer.WriteVector3(entry.HalfExtents);
                writer.WriteSingle(entry.Mass);
            }

            writer.SendTo(transport);
            NetPropDiagnostics.Log($"Manifest sent ({entries.Count} props)");
        }

        // PICKUP_REQ: [type][sender:str][propId:4]
        public static void SendPickupRequest(int propId)
        {
            if (_transport is not { IsConnected: true } transport) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.PropPickupRequest);
            writer.WriteString(transport.LocalId);
            writer.WriteInt32(propId);
            writer.SendTo(transport);
        }

        // PROP_RELEASE: [type][sender:str][propId:4][velocity:12][angularVelocity:12][flags:1]
        public static void SendRelease(int propId, Vector3 velocity, Vector3 angularVelocity)
        {
            if (_transport is not { IsConnected: true } transport) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.PropRelease);
            writer.WriteString(transport.LocalId);
            writer.WriteInt32(propId);
            writer.WriteVector3(velocity);
            writer.WriteVector3(angularVelocity);
            writer.WriteByte(ReleaseFlagsNone);
            writer.SendTo(transport);
        }

        // PROP_STATE: [type][sender:str][propId:4][position:12][rotation:16][velocity:12][flags:1]
        public static void SendState(int propId, Vector3 position, Quaternion rotation, Vector3 velocity, PropStateFlags flags)
        {
            if (_transport is not { IsConnected: true } transport) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.PropState);
            writer.WriteString(transport.LocalId);
            writer.WriteInt32(propId);
            writer.WriteVector3(position);
            writer.WriteQuaternion(rotation);
            writer.WriteVector3(velocity);
            writer.WriteByte((byte)flags);
            writer.SendTo(transport);
        }

        private static void UnsubscribeIdAssigned()
        {
            if (_transport != null) _transport.IdAssigned -= OnIdAssigned;
        }

        private static void OnIdAssigned()
        {
            if (_manifestSent) return;
            _manifestSent = true;
            SendManifest(BuildManifest());
        }

        private static List<PropManifestEntry> BuildManifest()
        {
            Manifest.Clear();
            foreach (var prop in NetPropRegistry.All)
                Manifest.Add(new PropManifestEntry(GetHierarchyPath(prop.transform), GetHalfExtents(prop), prop.Body.mass));
            return Manifest;
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
