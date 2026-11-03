using UnityEngine;

namespace Game.Scripts.Player
{
    public sealed class PlayerCrouch
    {
        private const float HeadCheckRadiusFactor = 0.95f;
        private const float MinStandUpDistance = 0.01f;

        private readonly CharacterController _controller;
        private readonly Transform _body;
        private readonly Transform _camera;
        private readonly float _crouchHeight;
        private readonly float _transitionSpeed;
        private readonly float _standHeight;
        private readonly Vector3 _standCenter;
        private readonly Vector3 _standCameraPosition;

        public PlayerCrouch(CharacterController controller, Transform camera, float crouchHeight, float transitionSpeed)
        {
            _controller = controller;
            _body = controller.transform;
            _camera = camera;
            _crouchHeight = crouchHeight;
            _transitionSpeed = transitionSpeed;
            _standHeight = controller.height;
            _standCenter = controller.center;
            _standCameraPosition = camera.localPosition;
        }

        public bool IsCrouching { get; private set; }

        public void Update(bool wantsCrouch)
        {
            if (wantsCrouch) IsCrouching = true;
            else if (IsCrouching && CanStandUp()) IsCrouching = false;

            var target = IsCrouching ? _crouchHeight : _standHeight;
            if (Mathf.Approximately(_controller.height, target)) return;
            SetHeight(Mathf.MoveTowards(_controller.height, target, _transitionSpeed * Time.deltaTime));
        }

        public void Stand()
        {
            IsCrouching = false;
            SetHeight(_standHeight);
        }

        private bool CanStandUp()
        {
            var distance = _standHeight - _controller.height;
            if (distance <= MinStandUpDistance) return true;

            var radius = _controller.radius * HeadCheckRadiusFactor;
            var origin = _body.position + _controller.center + Vector3.up * (_controller.height / 2f - radius);
            return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        // Ноги остаются на полу, камера опускается вместе с головой.
        private void SetHeight(float height)
        {
            var delta = _standHeight - height;
            _controller.height = height;
            _controller.center = _standCenter - Vector3.up * (delta / 2f);
            _camera.localPosition = _standCameraPosition - Vector3.up * delta;
        }
    }
}
