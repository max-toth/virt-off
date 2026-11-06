using UnityEngine;

namespace Game.Scripts.Player.Models
{
    public readonly struct PlayerInputState
    {
        public PlayerInputState(Vector2 look, Vector2 move, bool crouch, bool run, bool jump, bool toggleView)
        {
            Look = look;
            Move = move;
            Crouch = crouch;
            Run = run;
            Jump = jump;
            ToggleView = toggleView;
        }

        public Vector2 Look { get; }
        public Vector2 Move { get; }
        public bool Crouch { get; }
        public bool Run { get; }
        public bool Jump { get; }

        /// <summary>Нажата V: переключить вид от первого/третьего лица.</summary>
        public bool ToggleView { get; }
    }
}
