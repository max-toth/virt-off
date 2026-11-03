namespace Game.Scripts.Player.Animation.Models
{
    public readonly struct GaitState
    {
        public GaitState(float move, float run, float phaseSin, float phaseCos, float crouchAngle, float elbowAngle, float deltaTime)
        {
            Move = move;
            Run = run;
            PhaseSin = phaseSin;
            PhaseCos = phaseCos;
            CrouchAngle = crouchAngle;
            ElbowAngle = elbowAngle;
            DeltaTime = deltaTime;
        }

        /// <summary>0 — стоит, 1 — идёт.</summary>
        public float Move { get; }

        /// <summary>0 — шаг, 1 — бег.</summary>
        public float Run { get; }

        public float PhaseSin { get; }
        public float PhaseCos { get; }
        public float CrouchAngle { get; }
        public float ElbowAngle { get; }
        public float DeltaTime { get; }
    }
}
