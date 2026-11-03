using UnityEngine;

namespace Game.Scripts.Combat.Props.Models
{
    public readonly struct PropStateMessage
    {
        public PropStateMessage(string sender, int propId, Vector3 position, Quaternion rotation, PropStateFlags flags)
        {
            Sender = sender;
            PropId = propId;
            Position = position;
            Rotation = rotation;
            Flags = flags;
        }

        public string Sender { get; }
        public int PropId { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public PropStateFlags Flags { get; }
    }
}
