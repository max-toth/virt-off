using Game.Scripts.Audio.Models;
using UnityEngine;

namespace Game.Scripts.Combat.Models
{
    public readonly struct SoundMessage
    {
        public SoundMessage(string sender, Sfx sfx, Vector3 position, float volume)
        {
            Sender = sender;
            Sfx = sfx;
            Position = position;
            Volume = volume;
        }

        public string Sender { get; }
        public Sfx Sfx { get; }
        public Vector3 Position { get; }
        public float Volume { get; }
    }
}
