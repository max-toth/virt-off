using Game.Scripts.Combat.Models;
using Game.Scripts.Net;
using Game.Scripts.Player;
using Game.Scripts.Player.Animation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Combat
{
    // Ввод драки своего игрока: ЛКМ — удар, ПКМ — лещ, E — взять предмет;
    // с предметом в руках ЛКМ — бросить, E/ПКМ — положить.
    [RequireComponent(typeof(PlayerController), typeof(CombatHud))]
    public class PlayerCombat : MonoBehaviour
    {
        private const float HoldAnimationTime = 0.1f;
        private const float KnockbackDropFactor = 0.5f;

        [SerializeField] private MeleeSettings melee = new();
        [SerializeField] private CarrySettings carry = new();

        private PlayerController _player;
        private CharacterController _controller;
        private MeleeAttacker _attacker;
        private PropCarrier _carrier;
        private ProceduralWalk _walk;
        private bool _hasWalk;

        public bool IsActive => _player.enabled && !GameMenu.IsOpen;
        public bool IsHolding => _carrier.IsHolding;
        public bool HasAimedProp => _carrier.HasAimedProp;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _controller = GetComponent<CharacterController>();
            var view = GetComponentInChildren<Camera>().transform;
            _attacker = new MeleeAttacker(transform, view, melee);
            _carrier = new PropCarrier(transform, view, _controller, carry);
        }

        private void OnEnable()
        {
            ProceduralWalk.Created += OnWalkCreated;
            CombatNetwork.HitReceived += OnHitReceived;
        }

        private void Start()
        {
            if (_hasWalk) return;
            _walk = GetComponentInChildren<ProceduralWalk>();
            _hasWalk = _walk != null;
        }

        private void OnDisable()
        {
            ProceduralWalk.Created -= OnWalkCreated;
            CombatNetwork.HitReceived -= OnHitReceived;
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
            if (_carrier.UpdateHold() && _hasWalk) _walk.HoldItem(HoldAnimationTime);
        }

        private void Attack(AttackKind kind)
        {
            if (_hasWalk) _walk.PlayAttack(kind == AttackKind.Slap);
            _player.Shake(melee.For(kind).AttackerShake);
            _attacker.Attack(kind);
        }

        private void Throw()
        {
            _carrier.Throw();
            if (_hasWalk) _walk.PlayAttack(false);
        }

        private void OnWalkCreated(ProceduralWalk walk)
        {
            if (_hasWalk || !walk.transform.IsChildOf(transform)) return;
            _walk = walk;
            _hasWalk = true;
        }

        private void OnHitReceived(HitMessage hit)
        {
            if (!hit.HasTarget || hit.Target != GameNet.Instance?.LocalId) return;

            _player.AddImpulse(hit.Impulse);
            _player.Shake(melee.For(hit.Kind).VictimShake);
            if (_hasWalk) _walk.PlayHit();
            _carrier.Drop(hit.Impulse * KnockbackDropFactor);
        }
    }
}
