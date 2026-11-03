using UnityEngine;

namespace Game.Scripts.Player
{
    public sealed class PlayerMotor
    {
        private const float GroundStickVelocity = -2f;
        private const float AirKnockbackDamping = 1f;

        private readonly CharacterController _controller;
        private readonly float _gravity;
        private readonly float _jumpHeight;
        private readonly float _knockbackDamping;
        private float _verticalVelocity;
        private Vector3 _knockback;

        public PlayerMotor(CharacterController controller, float gravity, float jumpHeight, float knockbackDamping)
        {
            _controller = controller;
            _gravity = gravity;
            _jumpHeight = jumpHeight;
            _knockbackDamping = knockbackDamping;
        }

        public void AddImpulse(Vector3 velocity)
        {
            _knockback += new Vector3(velocity.x, 0f, velocity.z);
            if (velocity.y > 0f) _verticalVelocity = Mathf.Max(_verticalVelocity, 0f) + velocity.y;
        }

        public void Jump()
        {
            if (_controller.isGrounded) _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        public void Stop()
        {
            _verticalVelocity = 0f;
            _knockback = Vector3.zero;
        }

        public void Move(Vector3 horizontalVelocity)
        {
            var deltaTime = Time.deltaTime;
            _verticalVelocity += _gravity * deltaTime;

            var damping = _controller.isGrounded ? _knockbackDamping : AirKnockbackDamping;
            _knockback = Vector3.MoveTowards(_knockback, Vector3.zero, _knockback.magnitude * damping * deltaTime);

            var velocity = horizontalVelocity + _knockback;
            velocity.y = _verticalVelocity;
            _controller.Move(velocity * deltaTime);

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = GroundStickVelocity;
        }
    }
}
