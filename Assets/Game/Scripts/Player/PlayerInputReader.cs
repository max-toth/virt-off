using Game.Scripts.Player.Models;
using Game.Scripts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Player
{
    public sealed class PlayerInputReader
    {
        private const float MouseScale = 0.01f;

        private readonly GameSettings _settings;

        public PlayerInputReader(GameSettings settings) => _settings = settings;

        public bool TryRead(out PlayerInputState input)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null)
            {
                input = default;
                return false;
            }

            var look = mouse.delta.ReadValue() * (_settings.MouseSensitivity * MouseScale);
            if (_settings.InvertY) look.y = -look.y;

            input = new PlayerInputState(
                look,
                ReadMove(keyboard),
                keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed,
                keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed,
                keyboard.spaceKey.wasPressedThisFrame,
                keyboard.vKey.wasPressedThisFrame);
            return true;
        }

        private static Vector2 ReadMove(Keyboard keyboard)
        {
            var move = Vector2.zero;
            if (keyboard.wKey.isPressed) move.y = 1f;
            if (keyboard.sKey.isPressed) move.y = -1f;
            if (keyboard.aKey.isPressed) move.x = -1f;
            if (keyboard.dKey.isPressed) move.x = 1f;
            return move.normalized;
        }
    }
}
