using UnityEngine;

namespace Game.Scripts.Common
{
    public sealed class UnityClock : IClock
    {
        public float UnscaledTime => Time.unscaledTime;
    }
}
