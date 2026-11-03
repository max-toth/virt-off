using UnityEngine;

namespace Game.Scripts.Player.Sync.Models
{
    public readonly struct PositionMessage
    {
        public PositionMessage(string sender, Vector3 position, float yaw, bool crouching)
        {
            Sender = sender;
            Position = position;
            Yaw = yaw;
            Crouching = crouching;
        }

        public string Sender { get; }
        public Vector3 Position { get; }
        public float Yaw { get; }
        public bool Crouching { get; }
    }
}
