using Game.Scripts.Combat;
using Game.Scripts.Player;
using Game.Scripts.UI.Menu;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Hud
{
    // Прицел, подсказка про предмет в руках и подсказка про меню.
    [RequireComponent(typeof(UIDocument))]
    public class HudPresenter : MonoBehaviour
    {
        private const string HoldingHint = "ЛКМ — бросить, E — положить";
        private const string AimHint = "E — взять";

        private Label _crosshair;
        private Label _hint;
        private Label _menuHint;
        private PlayerCombat _combat;
        private bool _hasCombat;
        private string _shownHint;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _crosshair = root.Q<Label>("crosshair");
            _hint = root.Q<Label>("hintLabel");
            _menuHint = root.Q<Label>("menuHint");

            GameMenu.OpenChanged += OnMenuOpenChanged;
            OnMenuOpenChanged(GameMenu.IsOpen);
        }

        private void Start()
        {
            var player = PlayerController.Local;
            _hasCombat = player != null && player.TryGetComponent(out _combat);
        }

        private void OnDisable() => GameMenu.OpenChanged -= OnMenuOpenChanged;

        private void Update()
        {
            if (!_hasCombat) return;

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
