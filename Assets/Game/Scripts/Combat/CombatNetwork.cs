using System;
using Game.Scripts.Audio.Models;
using Game.Scripts.Combat.Models;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using UnityEngine;
using VContainer.Unity;

namespace Game.Scripts.Combat
{
    public sealed class CombatNetwork : IInitializable, IDisposable
    {
        private readonly INetTransport _transport;
        private readonly PacketRouter _router;
        private readonly PacketWriter _writer;

        public CombatNetwork(INetTransport transport, PacketRouter router, PacketWriter writer)
        {
            _transport = transport;
            _router = router;
            _writer = writer;
        }

        /// <summary>Удар другого игрока (свои удары не приходят).</summary>
        public event Action<HitMessage> HitReceived;

        /// <summary>Звук от другого игрока (свои не приходят).</summary>
        public event Action<SoundMessage> SoundReceived;

        public string LocalId => _transport.LocalId;

        public void Initialize()
        {
            _router.Subscribe(PacketType.Attack, OnAttack);
            _router.Subscribe(PacketType.Sound, OnSound);
        }

        public void Dispose()
        {
            _router.Unsubscribe(PacketType.Attack, OnAttack);
            _router.Unsubscribe(PacketType.Sound, OnSound);
        }

        // ATTACK: [type][attacker:str][kind:1][target:str][point:12][impulse:12][hitObject:1]
        public void SendHit(AttackKind kind, string target, Vector3 point, Vector3 impulse, bool hitObject = false)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.Attack);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteByte((byte)kind);
            _writer.WriteString(target);
            _writer.WriteVector3(point);
            _writer.WriteVector3(impulse);
            _writer.WriteBool(hitObject);
            _writer.SendTo(_transport);
        }

        // SOUND: [type][sender:str][sfx:1][position:12][volume:4]
        public void SendSound(Sfx sfx, Vector3 position, float volume)
        {
            if (!_transport.IsConnected) return;

            _writer.Begin(PacketType.Sound);
            _writer.WriteString(_transport.LocalId);
            _writer.WriteByte((byte)sfx);
            _writer.WriteVector3(position);
            _writer.WriteSingle(volume);
            _writer.SendTo(_transport);
        }

        public static bool TryReadHit(byte[] data, out HitMessage hit)
        {
            var reader = new PacketReader(data);
            var attacker = reader.ReadString();
            var kind = (AttackKind)reader.ReadByte();
            var target = reader.ReadString();
            var point = reader.ReadVector3();
            var impulse = reader.ReadVector3();
            var hitObject = reader.ReadBool();

            hit = reader.Failed ? default : new HitMessage(attacker, kind, target, point, impulse, hitObject);
            return !reader.Failed;
        }

        public static bool TryReadSound(byte[] data, out SoundMessage sound)
        {
            var reader = new PacketReader(data);
            var sender = reader.ReadString();
            var sfx = (Sfx)reader.ReadByte();
            var position = reader.ReadVector3();
            var volume = reader.ReadSingle();

            sound = reader.Failed ? default : new SoundMessage(sender, sfx, position, volume);
            return !reader.Failed;
        }

        private void OnAttack(string localId, byte[] data)
        {
            if (TryReadHit(data, out var hit) && hit.Attacker != localId) HitReceived?.Invoke(hit);
        }

        private void OnSound(string localId, byte[] data)
        {
            if (TryReadSound(data, out var sound) && sound.Sender != localId) SoundReceived?.Invoke(sound);
        }
    }
}
