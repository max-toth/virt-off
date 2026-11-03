using UnityEngine;

namespace Game.Scripts.Controls
{
    /// <summary>Блокировка игрового ввода, пока открыто меню: геймплей проверяет её, не завися от UI.</summary>
    public static class GameplayInput
    {
        public static bool IsBlocked { get; private set; }

        public static void SetBlocked(bool blocked) => IsBlocked = blocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => IsBlocked = false;
    }
}
