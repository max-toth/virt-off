using Game.Scripts.Audio;
using Game.Scripts.Combat.Models;
using Game.Scripts.Combat.Props;
using Game.Scripts.Controls;
using Game.Scripts.Player;
using Game.Scripts.Player.Animation;
using Game.Scripts.Player.Sync;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.Scripts.Combat
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        private const float HoldAnimationTime = 0.1f;
        private const float KnockbackDropFactor = 0.5f;

        [SerializeField] private MeleeSettings melee = new();
        [SerializeField] private CarrySettings carry = new();

        private PlayerController _player;
        private CharacterController _controller;
        private CombatNetwork _network;
        private GameplayInput _input;
        private MeleeAttacker _attacker;
        private PropCarrier _carrier;
        private ProceduralWalk _walk;
        private bool _hasWalk;

        public bool IsActive => _player.enabled && !_input.IsBlocked;
        public bool IsHolding => _carrier.IsHolding;
        public bool HasAimedProp => _carrier.HasAimedProp;

        [Inject]
        public void Construct(CombatNetwork network, GameplayInput input, RemotePlayers remotePlayers, NetPropRegistry props,
            NetPropSender propSender, CombatSounds combatSounds, GameSounds sounds)
        {
            _network = network;
            _input = input;
            _player = GetComponent<PlayerController>();
            _controller = GetComponent<CharacterController>();

            var view = _player.View;
            _attacker = new MeleeAttacker(transform, view, melee, remotePlayers, props, propSender, combatSounds, network);
            _carrier = new PropCarrier(transform, view, _controller, carry, props, sounds, network);
        }

        private void OnEnable() => _network.HitReceived += OnHitReceived;

        private void OnDisable()
        {
            _network.HitReceived -= OnHitReceived;
            _carrier.Drop(Vector3.zero);
        }

        private void Update()
        {
            _carrier.ClearAim();

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || !IsActive) return;

            var interactPressed = keyboard.eKey.wasPressedThisFrame;

            if (_carrier.IsHolding)
            {
                if (mouse.leftButton.wasPressedThisFrame) Throw();
                else if (interactPressed || mouse.rightButton.wasPressedThisFrame) _carrier.Drop(_controller.velocity);
                return;
            }

            _carrier.UpdateAim();
            if (interactPressed && _carrier.TryPickupAimed()) return;

            if (!_attacker.IsReady) return;
            if (mouse.leftButton.wasPressedThisFrame) Attack(AttackKind.Punch);
            else if (mouse.rightButton.wasPressedThisFrame) Attack(AttackKind.Slap);
        }

        private void LateUpdate()
        {
            if (_carrier.UpdateHold() && TryGetWalk(out var walk)) walk.HoldItem(HoldAnimationTime);
        }

        private void Attack(AttackKind kind)
        {
            if (TryGetWalk(out var walk)) walk.PlayAttack(kind == AttackKind.Slap);
            _player.Shake(melee.For(kind).AttackerShake);
            _attacker.Attack(kind);
        }

        private void Throw()
        {
            _carrier.Throw();
            if (TryGetWalk(out var walk)) walk.PlayAttack(false);
        }

        // Анимация появляется на своей модели позже: её добавляет RemoteAvatarFactory при создании шаблона для чужих игроков.
        private bool TryGetWalk(out ProceduralWalk walk)
        {
            if (!_hasWalk)
            {
                _walk = GetComponentInChildren<ProceduralWalk>();
                _hasWalk = _walk != null;
            }

            walk = _walk;
            return _hasWalk;
        }

        private void OnHitReceived(HitMessage hit)
        {
            if (!hit.HasTarget || hit.Target != _network.LocalId) return;

            _player.AddImpulse(hit.Impulse);
            _player.Shake(melee.For(hit.Kind).VictimShake);
            if (TryGetWalk(out var walk)) walk.PlayHit();
            _carrier.Drop(hit.Impulse * KnockbackDropFactor);
        }
    }
}
