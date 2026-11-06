using UnityEngine;

namespace Game.Scripts.Player
{
    /// <summary>Вид от третьего лица: камера отъезжает назад от глаз по линии взгляда.
    /// Центр экрана остаётся на луче из глаз, поэтому прицел и подбор работают как от первого лица.</summary>
    public sealed class ThirdPersonCamera
    {
        private const float CollisionRadius = 0.2f;
        private const float WallGap = 0.05f;
        private const float MoveOutSpeed = 10f;

        private readonly Transform _eyes;
        private readonly Transform _camera;
        private readonly float _distance;
        private float _current;

        /// <param name="eyes">Точка обзора: от неё целятся и считается отъезд.</param>
        /// <param name="camera">Камера, дочерняя к eyes.</param>
        /// <param name="distance">Расстояние от глаз до камеры, м.</param>
        public ThirdPersonCamera(Transform eyes, Transform camera, float distance)
        {
            _eyes = eyes;
            _camera = camera;
            _distance = distance;
        }

        public bool IsActive { get; private set; }

        public void Toggle() => IsActive = !IsActive;

        /// <summary>Вызывается в LateUpdate, после поворота взгляда.</summary>
        public void Update()
        {
            var target = IsActive ? FreeDistance() : 0f;
            // К стене и в глаза — сразу, иначе камера на кадр окажется внутри стены; назад — плавно.
            _current = target < _current ? target : Mathf.MoveTowards(_current, target, MoveOutSpeed * Time.deltaTime);
            _camera.localPosition = Vector3.back * _current;
        }

        // Сфера стартует внутри своего CharacterController, а такие коллайдеры SphereCast не видит.
        private float FreeDistance()
        {
            var hit = Physics.SphereCast(_eyes.position, CollisionRadius, -_eyes.forward, out var info, _distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return hit ? Mathf.Max(0f, info.distance - WallGap) : _distance;
        }
    }
}
