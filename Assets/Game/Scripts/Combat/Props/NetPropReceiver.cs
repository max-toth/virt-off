using System;
using System.Collections.Generic;
using Game.Scripts.Combat.Props.Models;
using Game.Scripts.Net.Packets;
using Game.Scripts.Player.Sync;
using VContainer.Unity;

namespace Game.Scripts.Combat.Props
{
    /// <summary>Пакеты предметов от сервера и других клиентов: применяет их к предметам сцены.</summary>
    public sealed class NetPropReceiver : IInitializable, IDisposable
    {
        private const byte ReleaseEventFreeFlag = 1;
        private const int ServerIdSize = 4;
        private const int VelocitySize = 12;
        private const float RemoteHoldAnimationTime = 0.3f;

        private readonly PacketRouter _router;
        private readonly NetPropRegistry _registry;
        private readonly RemotePlayers _remotePlayers;
        private readonly List<int> _serverIds = new();
        private readonly List<PropSnapshotEntry> _snapshotEntries = new();

        public NetPropReceiver(PacketRouter router, NetPropRegistry registry, RemotePlayers remotePlayers)
        {
            _router = router;
            _registry = registry;
            _remotePlayers = remotePlayers;
        }

        public void Initialize()
        {
            _router.Subscribe(PacketType.PropManifestAck, OnManifestAck);
            _router.Subscribe(PacketType.PropGrant, OnGrant);
            _router.Subscribe(PacketType.PropReleaseEvent, OnReleaseEvent);
            _router.Subscribe(PacketType.PropSnapshot, OnSnapshot);
            _router.Subscribe(PacketType.PropState, OnState);
        }

        public void Dispose()
        {
            _router.Unsubscribe(PacketType.PropManifestAck, OnManifestAck);
            _router.Unsubscribe(PacketType.PropGrant, OnGrant);
            _router.Unsubscribe(PacketType.PropReleaseEvent, OnReleaseEvent);
            _router.Unsubscribe(PacketType.PropSnapshot, OnSnapshot);
            _router.Unsubscribe(PacketType.PropState, OnState);
        }

        // MANIFEST_ACK: [type][count:2][serverId:4]*
        /// <param name="serverIds">Заполняется id в порядке манифеста; предварительно очищается.</param>
        public static bool TryReadManifestAck(byte[] data, List<int> serverIds)
        {
            serverIds.Clear();
            var reader = new PacketReader(data);
            var count = reader.ReadUInt16();
            if (reader.Failed || reader.Remaining < count * ServerIdSize) return false;

            for (var i = 0; i < count; i++)
                serverIds.Add(reader.ReadInt32());
            return true;
        }

        // PROP_GRANT: [type][propId:4][holder:str][flags:1]
        public static bool TryReadGrant(byte[] data, out PropGrantMessage grant)
        {
            var reader = new PacketReader(data);
            var serverId = reader.ReadInt32();
            var holder = reader.ReadString();

            grant = reader.Failed ? default : new PropGrantMessage(serverId, holder);
            return !reader.Failed;
        }

        // PROP_RELEASE_EVENT: [type][propId:4][previousOwner:str][velocity:12][angularVelocity:12][flags:1]
        public static bool TryReadReleaseEvent(byte[] data, out PropReleaseEventMessage release)
        {
            var reader = new PacketReader(data);
            var serverId = reader.ReadInt32();
            var previousOwner = reader.ReadString();
            reader.Skip(VelocitySize * 2);
            var flags = reader.ReadByte();

            var isFree = string.IsNullOrEmpty(previousOwner) || (flags & ReleaseEventFreeFlag) != 0;
            release = reader.Failed ? default : new PropReleaseEventMessage(serverId, previousOwner, isFree);
            return !reader.Failed;
        }

        // PROP_SNAPSHOT: [type][count:2]{[propId:4][owner:str][held:1]}*
        /// <param name="entries">Заполняется прочитанными записями; при обрезанном пакете — теми, что успели прочитаться.</param>
        /// <returns>false, если пакет обрезан.</returns>
        public static bool TryReadSnapshot(byte[] data, List<PropSnapshotEntry> entries)
        {
            entries.Clear();
            var reader = new PacketReader(data);
            var count = reader.ReadUInt16();

            for (var i = 0; i < count && !reader.Failed; i++)
            {
                var serverId = reader.ReadInt32();
                reader.SkipString();
                var held = reader.ReadBool();
                if (!reader.Failed) entries.Add(new PropSnapshotEntry(serverId, held));
            }

            return !reader.Failed;
        }

        // PROP_STATE: [type][sender:str][propId:4][position:12][rotation:16][velocity:12][flags:1]
        public static bool TryReadState(byte[] data, out PropStateMessage state)
        {
            var reader = new PacketReader(data);
            var sender = reader.ReadString();
            var serverId = reader.ReadInt32();
            var position = reader.ReadVector3();
            var rotation = reader.ReadQuaternion();
            reader.Skip(VelocitySize);
            var flags = (PropStateFlags)reader.ReadByte();

            state = reader.Failed ? default : new PropStateMessage(sender, serverId, position, rotation, flags);
            return !reader.Failed;
        }

        private void OnManifestAck(string localId, byte[] data)
        {
            if (!TryReadManifestAck(data, _serverIds)) return;

            for (var i = 0; i < _serverIds.Count; i++)
                _registry.AssignServerId(i, _serverIds[i]);

            NetPropDiagnostics.Log($"MANIFEST_ACK: {_serverIds.Count} props mapped");
        }

        private void OnGrant(string localId, byte[] data)
        {
            if (!TryReadGrant(data, out var grant) || !_registry.TryGetByServerId(grant.PropId, out var prop)) return;

            if (grant.Holder == localId)
            {
                prop.ApplyGrant();
                NetPropDiagnostics.Log($"PROP_GRANT: we own prop={prop.Id} held={prop.IsHeld}");
            }
            else
            {
                prop.ApplyGrantToOther(grant.Holder);
                NetPropDiagnostics.Log($"PROP_GRANT: {grant.Holder} owns prop={prop.Id}");
            }
        }

        private void OnReleaseEvent(string localId, byte[] data)
        {
            if (!TryReadReleaseEvent(data, out var release) || !_registry.TryGetByServerId(release.PropId, out var prop))
                return;

            prop.ApplyRelease(release.PreviousOwner == localId, release.IsFree);
            NetPropDiagnostics.Log($"PROP_RELEASE_EVENT prop={prop.Id} owner={release.PreviousOwner} free={release.IsFree}");
        }

        // Обрезанный снимок применяется частично: прочитанные записи достоверны.
        private void OnSnapshot(string localId, byte[] data)
        {
            TryReadSnapshot(data, _snapshotEntries);

            foreach (var entry in _snapshotEntries)
            {
                if (_registry.TryGetByServerId(entry.PropId, out var prop))
                    prop.ApplySnapshot(entry.Held);
            }

            NetPropDiagnostics.Log($"PROP_SNAPSHOT: {_snapshotEntries.Count} entries");
        }

        private void OnState(string localId, byte[] data)
        {
            if (!TryReadState(data, out var state) || state.Sender == localId) return;
            if (!_registry.TryGetByServerId(state.PropId, out var prop)) return;
            if (!prop.ApplyRemoteState(state.Sender, state.Position, state.Rotation, state.Flags)) return;

            if ((state.Flags & PropStateFlags.Held) != 0 && _remotePlayers.TryGetWalk(state.Sender, out var walk))
                walk.HoldItem(RemoteHoldAnimationTime);
        }
    }
}
