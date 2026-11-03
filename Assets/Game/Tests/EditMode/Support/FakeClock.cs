using Game.Scripts.Common;

namespace Game.Tests.EditMode.Support
{
    public sealed class FakeClock : IClock
    {
        public float UnscaledTime { get; set; }
    }
}
