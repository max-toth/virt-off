namespace Game.Scripts.Combat.Props.Models
{
    public enum PropOwnershipState
    {
        /// <summary>Никто не держит; предмет лежит или следует за чужими PROP_STATE.</summary>
        Free,
        /// <summary>Мы взяли предмет и ждём PROP_GRANT.</summary>
        PickupPending,
        /// <summary>Предмет у нас в руках, сервер подтвердил: рассылаем состояние.</summary>
        Held,
        /// <summary>Мы бросили, толкнули или ударили предмет и сервер оставил его физику нам: рассылаем состояние, пока не успокоится.</summary>
        Simulating,
        /// <summary>Предмет в руках у другого игрока.</summary>
        HeldByOther,
        /// <summary>Мы толкнули свободный предмет и ждём PROP_GRANT, чтобы вести его физику.</summary>
        SimulationPending
    }
}
