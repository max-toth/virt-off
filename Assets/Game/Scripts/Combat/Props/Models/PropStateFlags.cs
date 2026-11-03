using System;

namespace Game.Scripts.Combat.Props.Models
{
    [Flags]
    public enum PropStateFlags : byte
    {
        None = 0,
        Held = 1,
        Settled = 2
    }
}
