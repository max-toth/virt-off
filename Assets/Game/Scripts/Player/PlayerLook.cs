using UnityEngine;

namespace Game.Scripts.Player
{
    public sealed class PlayerLook
    {
        private const float ShakeDecay = 8f;
        private const float ShakeFrequency = 40f;

        private readonly Transform _body;
        private readonly Transform _camera;
        private readonly float _maxPitch;
        private float _pitch;
        private float _shake;

        public PlayerLook(Transform body, Transform camera, float maxPitch)
        {
            _body = body;
            _camera = camera;
            _maxPitch = maxPitch;
        }

        public void AddShake(float degrees) => _shake = Mathf.Max(_shake, degrees);

        public void Reset()
        {
            _pitch = 0f;
            _camera.localRotation = Quaternion.identity;
        }

        public void Update(Vector2 delta)
        {
            _body.Rotate(0f, delta.x, 0f);
            _pitch = Mathf.Clamp(_pitch - delta.y, -_maxPitch, _maxPitch);

            _shake = Mathf.MoveTowards(_shake, 0f, _shake * ShakeDecay * Time.deltaTime + Time.deltaTime);
            var t = Time.time * ShakeFrequency;
            _camera.localRotation = Quaternion.Euler(
                _pitch + (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * _shake,
                (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * _shake,
                (Mathf.PerlinNoise(t, t) - 0.5f) * _shake);
        }
    }
}
