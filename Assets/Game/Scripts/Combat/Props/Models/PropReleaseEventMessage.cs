namespace Game.Scripts.Combat.Props.Models
{
    public readonly struct PropReleaseEventMessage
    {
        public PropReleaseEventMessage(int propId, string previousOwner, bool isFree)
        {
            PropId = propId;
            PreviousOwner = previousOwner;
            IsFree = isFree;
        }

        public int PropId { get; }
        public string PreviousOwner { get; }

        /// <summary>Владение вернулось серверу: владельца нет или сервер выставил флаг.</summary>
        public bool IsFree { get; }
    }
}
