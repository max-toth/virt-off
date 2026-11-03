using System;

namespace Game.Scripts.Controls
{
    /// <summary>Блокировка игрового ввода, пока открыто меню: геймплей проверяет её, не завися от UI.</summary>
    public sealed class GameplayInput
    {
        public bool IsBlocked { get; private set; }

        /// <summary>Аргумент — заблокирован ли ввод.</summary>
        public event Action<bool> BlockedChanged;

        public void SetBlocked(bool blocked)
        {
            if (IsBlocked == blocked) return;
            IsBlocked = blocked;
            BlockedChanged?.Invoke(blocked);
        }
    }
}
