using System;
using Game.Scripts.Audio;
using Game.Scripts.Audio.Models;
using Game.Scripts.Combat.Models;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using UnityEngine;

namespace Game.Scripts.Combat
{
    public static class CombatNetwork
    {
        private static INetTransport _transport;
        private static PacketRouter _router;

        /// <summary>Удар другого игрока (свои удары не приходят).</summary>
        public static event Action<HitMessage> HitReceived;

        public static string LocalId => _transport?.LocalId;

        public static void Initialize(INetTransport transport, PacketRouter router)
        {
            Unsubscribe();
            _transport = transport;
            _router = router;
            router.Subscribe(PacketType.Attack, OnAttack);
            router.Subscribe(PacketType.Sound, OnSound);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Shutdown()
        {
            Unsubscribe();
            _transport = null;
            HitReceived = null;
        }

        // ATTACK: [type][attacker:str][kind:1][target:str][point:12][impulse:12][hitObject:1]
        public static void SendHit(AttackKind kind, string target, Vector3 point, Vector3 impulse, bool hitObject = false)
        {
            if (_transport is not { IsConnected: true } transport) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.Attack);
            writer.WriteString(transport.LocalId);
            writer.WriteByte((byte)kind);
            writer.WriteString(target);
            writer.WriteVector3(point);
            writer.WriteVector3(impulse);
            writer.WriteBool(hitObject);
            writer.SendTo(transport);
        }

        // SOUND: [type][sender:str][sfx:1][position:12][volume:4]
        public static void SendSound(Sfx sfx, Vector3 position, float volume)
        {
            if (_transport is not { IsConnected: true } transport) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.Sound);
            writer.WriteString(transport.LocalId);
            writer.WriteByte((byte)sfx);
            writer.WriteVector3(position);
            writer.WriteSingle(volume);
            writer.SendTo(transport);
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

        private static void Unsubscribe()
        {
            if (_router == null) return;
            _router.Unsubscribe(PacketType.Attack, OnAttack);
            _router.Unsubscribe(PacketType.Sound, OnSound);
            _router = null;
        }

        private static void OnAttack(string localId, byte[] data)
        {
            if (TryReadHit(data, out var hit) && hit.Attacker != localId) HitReceived?.Invoke(hit);
        }

        private static void OnSound(string localId, byte[] data)
        {
            if (TryReadSound(data, out var sound) && sound.Sender != localId)
                GameSounds.Play(sound.Sfx, sound.Position, sound.Volume);
        }
    }
}
