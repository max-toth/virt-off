using UnityEngine;

namespace Game.Scripts.Combat
{
    public class CombatHud : MonoBehaviour
    {
        private const string Crosshair = "+";
        private const string HoldingHint = "ЛКМ — бросить, E — положить";
        private const string AimHint = "E — взять";

        private PlayerCombat _combat;
        private bool _hasCombat;
        private GUIStyle _hintStyle;

        // PlayerCombat добавляет этот компонент через RequireComponent раньше себя, поэтому ищем его в Start.
        private void Start() => _hasCombat = TryGetComponent(out _combat);

        private void OnGUI()
        {
            if (!_hasCombat || !_combat.IsActive) return;

            float centerX = Screen.width / 2f, centerY = Screen.height / 2f;
            GUI.Label(new Rect(centerX - 5f, centerY - 11f, 20f, 20f), Crosshair);

            var hint = _combat.IsHolding ? HoldingHint : _combat.HasAimedProp ? AimHint : null;
            if (hint == null) return;

            _hintStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter };
            GUI.Label(new Rect(centerX - 100f, centerY + 20f, 200f, 22f), hint, _hintStyle);
        }
    }
}
