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
        private static readonly PacketSubscription Subscription = new(OnPacket);

        public static event Action<HitMessage> HitReceived;

        // ATTACK: [type][attacker:str][kind:1][target:str][point:12][impulse:12][hitObject:1]
        public static void SendHit(AttackKind kind, string target, Vector3 point, Vector3 impulse, bool hitObject = false)
        {
            if (!GameNet.TryGetConnected(out var manager)) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.Attack);
            writer.WriteString(manager.LocalId);
            writer.WriteByte((byte)kind);
            writer.WriteString(target);
            writer.WriteVector3(point);
            writer.WriteVector3(impulse);
            writer.WriteBool(hitObject);
            writer.SendTo(manager);
        }

        // SOUND: [type][sender:str][sfx:1][position:12][volume:4]
        public static void SendSound(Sfx sfx, Vector3 position, float volume)
        {
            if (!GameNet.TryGetConnected(out var manager)) return;

            var writer = PacketWriter.Shared;
            writer.Begin(PacketType.Sound);
            writer.WriteString(manager.LocalId);
            writer.WriteByte((byte)sfx);
            writer.WriteVector3(position);
            writer.WriteSingle(volume);
            writer.SendTo(manager);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Subscription.Unbind();
            HitReceived = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Subscribe() => Subscription.Bind(GameNet.Instance);

        private static void OnPacket(string localId, byte[] data)
        {
            if (data.Length == 0) return;

            switch (data[0])
            {
                case PacketType.Sound when TryReadSound(data, out var sound) && sound.Sender != localId:
                    GameSounds.Play(sound.Sfx, sound.Position, sound.Volume);
                    break;
                case PacketType.Attack when TryReadHit(data, out var hit) && hit.Attacker != localId:
                    HitReceived?.Invoke(hit);
                    break;
            }
        }

        private static bool TryReadHit(byte[] data, out HitMessage hit)
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

        private static bool TryReadSound(byte[] data, out SoundMessage sound)
        {
            var reader = new PacketReader(data);
            var sender = reader.ReadString();
            var sfx = (Sfx)reader.ReadByte();
            var position = reader.ReadVector3();
            var volume = reader.ReadSingle();

            sound = reader.Failed ? default : new SoundMessage(sender, sfx, position, volume);
            return !reader.Failed;
        }
    }
}
