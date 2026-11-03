namespace Game.Scripts.Combat.Props.Models
{
    public enum PropOwnershipState
    {
        /// <summary>Никто не держит; физику ведёт сервер или предмет следует за чужими PROP_STATE.</summary>
        Free,
        /// <summary>Мы взяли предмет и ждём PROP_GRANT.</summary>
        PickupPending,
        /// <summary>Предмет у нас в руках, сервер подтвердил: рассылаем состояние.</summary>
        Held,
        /// <summary>Сервер отдал нам физику брошенного или толкнутого предмета: рассылаем состояние, пока не успокоится.</summary>
        Simulating,
        /// <summary>Предмет в руках у другого игрока.</summary>
        HeldByOther
    }
}
