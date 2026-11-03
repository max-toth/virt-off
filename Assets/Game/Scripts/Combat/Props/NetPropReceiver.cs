using Game.Scripts.Combat.Props.Models;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Player.Sync;
using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    public static class NetPropReceiver
    {
        private const byte ReleaseEventFreeFlag = 1;
        private const int VelocitySize = 12;
        private const float RemoteHoldAnimationTime = 0.3f;

        private static readonly PacketSubscription Subscription = new(OnPacket);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Subscription.Unbind();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Subscribe() => Subscription.Bind(GameNet.Instance);

        private static void OnPacket(string localId, byte[] data)
        {
            if (data.Length < 2) return;

            switch (data[0])
            {
                case PacketType.PropManifestAck:
                    HandleManifestAck(data);
                    break;
                case PacketType.PropGrant:
                    HandleGrant(data, localId);
                    break;
                case PacketType.PropReleaseEvent:
                    HandleReleaseEvent(data, localId);
                    break;
                case PacketType.PropSnapshot:
                    HandleSnapshot(data);
                    break;
                case PacketType.PropState:
                    HandleState(data, localId);
                    break;
            }
        }

        // [count:2][serverId:4]*
        private static void HandleManifestAck(byte[] data)
        {
            var reader = new PacketReader(data);
            var count = reader.ReadUInt16();
            if (reader.Failed || reader.Remaining < count * 4) return;

            for (var i = 0; i < count; i++)
                NetPropRegistry.AssignServerId(i, reader.ReadInt32());

            NetPropDiagnostics.Log($"MANIFEST_ACK: {count} props mapped");
        }

        // [propId:4][holder:str][flags:1]
        private static void HandleGrant(byte[] data, string localId)
        {
            var reader = new PacketReader(data);
            var serverId = reader.ReadInt32();
            var holder = reader.ReadString();
            if (reader.Failed || !NetPropRegistry.TryGetByServerId(serverId, out var prop)) return;

            if (holder == localId)
            {
                prop.ApplyGrant();
                NetPropDiagnostics.Log($"PROP_GRANT: we own prop={prop.Id} held={prop.IsHeld}");
            }
            else
            {
                prop.ApplyGrantToOther(holder);
                NetPropDiagnostics.Log($"PROP_GRANT: {holder} owns prop={prop.Id}");
            }
        }

        // [propId:4][previousOwner:str][velocity:12][angularVelocity:12][flags:1]
        private static void HandleReleaseEvent(byte[] data, string localId)
        {
            var reader = new PacketReader(data);
            var serverId = reader.ReadInt32();
            var previousOwner = reader.ReadString();
            reader.Skip(VelocitySize * 2);
            var flags = reader.ReadByte();
            if (reader.Failed || !NetPropRegistry.TryGetByServerId(serverId, out var prop)) return;

            var isFree = string.IsNullOrEmpty(previousOwner) || (flags & ReleaseEventFreeFlag) != 0;
            prop.ApplyRelease(previousOwner == localId, isFree);
            NetPropDiagnostics.Log($"PROP_RELEASE_EVENT prop={prop.Id} owner={previousOwner} free={isFree}");
        }

        // [count:2]{[propId:4][owner:str][held:1]}*
        private static void HandleSnapshot(byte[] data)
        {
            var reader = new PacketReader(data);
            var count = reader.ReadUInt16();
            if (reader.Failed) return;

            for (var i = 0; i < count; i++)
            {
                var serverId = reader.ReadInt32();
                reader.SkipString();
                var held = reader.ReadBool();
                if (reader.Failed) break;

                if (NetPropRegistry.TryGetByServerId(serverId, out var prop))
                    prop.ApplySnapshot(held);
            }

            NetPropDiagnostics.Log($"PROP_SNAPSHOT: {count} entries");
        }

        // [sender:str][propId:4][position:12][rotation:16][velocity:12][flags:1]
        private static void HandleState(byte[] data, string localId)
        {
            var reader = new PacketReader(data);
            var sender = reader.ReadString();
            if (reader.Failed || sender == localId) return;

            var serverId = reader.ReadInt32();
            var position = reader.ReadVector3();
            var rotation = reader.ReadQuaternion();
            reader.Skip(VelocitySize);
            var flags = (PropStateFlags)reader.ReadByte();
            if (reader.Failed || !NetPropRegistry.TryGetByServerId(serverId, out var prop)) return;

            if (!prop.ApplyRemoteState(sender, position, rotation, flags)) return;

            if ((flags & PropStateFlags.Held) != 0 && RemotePlayers.TryGetWalk(sender, out var walk))
                walk.HoldItem(RemoteHoldAnimationTime);
        }
    }
}
