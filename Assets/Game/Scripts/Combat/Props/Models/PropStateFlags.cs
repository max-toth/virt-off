using System;

namespace Game.Scripts.Combat.Props.Models
{
    [Flags]
    public enum PropStateFlags : byte
    {
        None = 0,
        /// <summary>Предмет в руках у отправителя.</summary>
        Held = 1,
        /// <summary>Предмет остановился, владение возвращается серверу: получатель ставит его сразу, без сглаживания.</summary>
        Settled = 2
    }
}
