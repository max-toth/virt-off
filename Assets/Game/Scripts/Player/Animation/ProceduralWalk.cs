using System;
using Game.Scripts.Player.Animation.Models;
using UnityEngine;

namespace Game.Scripts.Player.Animation
{
    /// <summary>
    /// Процедурная анимация для моделей без клипов: скорость считается по смещению transform,
    /// поэтому одинаково работает для своего и для чужих игроков.
    /// </summary>
    public class ProceduralWalk : MonoBehaviour
    {
        private const float TeleportSpeed = 20f;
        private const float SpeedSmoothing = 10f;
        private const float MovingSpeed = 0.1f;
        private const float BackwardDot = -0.01f;
        private const float FullCircle = Mathf.PI * 2f;

        [SerializeField] private GaitSettings gait = new();
        [SerializeField] private PoseAmplitudes amplitudes = new();

        private readonly HitReactionLayer _hitReaction = new();
        private readonly CarryLayer _carry = new();
        private readonly AttackLayer _attack = new();
        private IPoseLayer[] _layers;
        private WalkRig _rig;
        private PlayerController _player;
        private bool _hasPlayer;
        private Vector3 _lastPosition;
        private float _speed;
        private float _forwardSign = 1f;
        private float _phase;
        private float _crouchBlend;
        private bool _crouching;

        public static event Action<ProceduralWalk> Created;

        /// <summary>Замах правой рукой: кулак или, если slap, лещ.</summary>
        public void PlayAttack(bool slap) => _attack.Play(slap);

        public void PlayHit() => _hitReaction.Play();

        /// <summary>Держать руки перед собой ещё duration секунд; вызывать каждый кадр, пока предмет в руках.</summary>
        public void HoldItem(float duration) => _carry.Hold(duration);

        /// <summary>Присед для чужих игроков; для своего значение берётся из PlayerController.</summary>
        public void SetCrouching(bool crouching) => _crouching = crouching;

        private void Awake()
        {
            Created?.Invoke(this);

            if (!WalkRig.TryCreate(transform, out _rig))
            {
                Debug.LogWarning($"[ProceduralWalk] {name}: не найдены кости metarig, анимация отключена");
                enabled = false;
                return;
            }

            _layers = new IPoseLayer[] { new LocomotionLayer(amplitudes), _hitReaction, _carry, _attack };
            _player = GetComponentInParent<PlayerController>();
            _hasPlayer = _player != null;
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            UpdateSpeed(deltaTime);
            if (_hasPlayer) _crouching = _player.IsCrouching;
            _crouchBlend = Mathf.MoveTowards(_crouchBlend, _crouching ? 1f : 0f, gait.CrouchBlendSpeed * deltaTime);

            var state = AdvanceGait(deltaTime);
            _rig.ResetPose();
            foreach (var layer in _layers) layer.Apply(_rig, state);
        }

        private void UpdateSpeed(float deltaTime)
        {
            var position = transform.position;
            var delta = position - _lastPosition;
            _lastPosition = position;
            delta.y = 0f;

            var rawSpeed = delta.magnitude / deltaTime;
            if (rawSpeed > TeleportSpeed) rawSpeed = 0f;
            _speed = Mathf.Lerp(_speed, rawSpeed, 1f - Mathf.Exp(-SpeedSmoothing * deltaTime));

            if (rawSpeed > MovingSpeed)
                _forwardSign = Vector3.Dot(delta, transform.forward) < BackwardDot ? -1f : 1f;
        }

        private GaitState AdvanceGait(float deltaTime)
        {
            var move = Mathf.Clamp01(_speed / gait.WalkSpeed);
            var run = Mathf.Clamp01((_speed - gait.WalkSpeed) / (gait.RunSpeed - gait.WalkSpeed));

            var cycle = Mathf.Lerp(gait.WalkCycleLength, gait.RunCycleLength, run);
            _phase = Mathf.Repeat(_phase + _forwardSign * _speed / cycle * FullCircle * deltaTime, FullCircle);

            return new GaitState(
                move,
                run,
                Mathf.Sin(_phase),
                Mathf.Cos(_phase),
                gait.CrouchKneeAngle * _crouchBlend,
                Mathf.Lerp(amplitudes.WalkElbowBend, amplitudes.RunElbowBend, run),
                deltaTime);
        }
    }
}
