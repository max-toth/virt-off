namespace Game.Scripts.Combat.Props.Models
{
    public readonly struct PropGrantMessage
    {
        public PropGrantMessage(int propId, string holder)
        {
            PropId = propId;
            Holder = holder;
        }

        public int PropId { get; }
        public string Holder { get; }
    }
}
