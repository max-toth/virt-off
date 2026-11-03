namespace Game.Scripts.Combat.Props.Models
{
    public readonly struct PropSnapshotEntry
    {
        public PropSnapshotEntry(int propId, bool held)
        {
            PropId = propId;
            Held = held;
        }

        public int PropId { get; }
        public bool Held { get; }
    }
}
