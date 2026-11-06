using System.Collections.Generic;
using Game.Scripts.Combat.Props.Models;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    public sealed class NetPropSender
    {
        private const byte ReleaseFlagsNone = 0;

        private readonly INetTransport _transport;
        private readonly PacketWriter _writer;

        public NetPropSender(INetTransport transport, PacketWriter writer)
        {
            _transport = transport;
            _writer = writer;
        }

        public bool IsConnected => _transport.IsConnected;

        public string LocalId => _transport.LocalId;

        // MANIFEST: [type][sender:str][count:2]{[key:str][halfExtents:12][mass:4]}*
        public void SendManifest(IReadOnlyList<PropManifestEntry> entries)
        {
            if (!_transport.IsConnected || entries.Count == 0) return;

            _writer.Begin(PacketType.PropManifest);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteUInt16(entries.Count);
            foreach (var entry in entries)
            {
                _writer.WriteString(entry.Key);
                _writer.WriteVector3(entry.HalfExtents);
                _writer.WriteSingle(entry.Mass);
            }

            _writer.SendTo(_transport);
            NetPropDiagnostics.Log($"Manifest sent ({entries.Count} props)");
        }

        // PICKUP_REQ: [type][sender:str][propId:4]
        public void SendPickupRequest(int propId)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.PropPickupRequest);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteInt32(propId);
            _writer.SendTo(_transport);
        }

        // PROP_PUSH: [type][sender:str][propId:4]
        public void SendPushRequest(int propId)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.PropPush);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteInt32(propId);
            _writer.SendTo(_transport);
        }

        // PROP_RELEASE: [type][sender:str][propId:4][velocity:12][angularVelocity:12][flags:1]
        public void SendRelease(int propId, Vector3 velocity, Vector3 angularVelocity)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.PropRelease);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteInt32(propId);
            _writer.WriteVector3(velocity);
            _writer.WriteVector3(angularVelocity);
            _writer.WriteByte(ReleaseFlagsNone);
            _writer.SendTo(_transport);
        }

        // PROP_STATE: [type][sender:str][propId:4][position:12][rotation:16][velocity:12][flags:1]
        public void SendState(int propId, Vector3 position, Quaternion rotation, Vector3 velocity, PropStateFlags flags)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.PropState);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteInt32(propId);
            _writer.WriteVector3(position);
            _writer.WriteQuaternion(rotation);
            _writer.WriteVector3(velocity);
            _writer.WriteByte((byte)flags);
            _writer.SendTo(_transport);
        }
    }
}
