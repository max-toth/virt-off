using Game.Scripts.Combat;
using Game.Scripts.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Game.Scripts.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public class HudPresenter : MonoBehaviour
    {
        private const string HoldingHint = "ЛКМ — бросить, E — положить";
        private const string AimHint = "E — взять";

        private Label _crosshair;
        private Label _hint;
        private Label _menuHint;
        private PlayerCombat _combat;
        private GameplayInput _input;
        private string _shownHint;

        [Inject]
        public void Construct(PlayerCombat combat, GameplayInput input)
        {
            _combat = combat;
            _input = input;
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _crosshair = root.Q<Label>("crosshair");
            _hint = root.Q<Label>("hintLabel");
            _menuHint = root.Q<Label>("menuHint");

            _input.BlockedChanged += OnMenuOpenChanged;
            OnMenuOpenChanged(_input.IsBlocked);
        }

        private void OnDisable() => _input.BlockedChanged -= OnMenuOpenChanged;

        private void Update()
        {
            var active = _combat.IsActive;
            _crosshair.SetVisible(active);

            var hint = !active ? null : _combat.IsHolding ? HoldingHint : _combat.HasAimedProp ? AimHint : null;
            if (ReferenceEquals(hint, _shownHint)) return;

            _shownHint = hint;
            _hint.text = hint ?? string.Empty;
            _hint.SetVisible(hint != null);
        }

        private void OnMenuOpenChanged(bool open) => _menuHint.SetVisible(!open);
    }
}
