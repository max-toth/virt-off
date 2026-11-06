using UnityEngine;

namespace Game.Scripts.Player
{
    /// <summary>Табличка с текстом над персонажем: висит над его ногами и всегда повёрнута к камере игрока.</summary>
    public sealed class Nameplate : MonoBehaviour
    {
        private const string BuiltinFont = "LegacyRuntime.ttf";
        private const float HeightAboveFeet = 2.05f;
        private const int FontSize = 64;
        // Высота строки в мире ≈ FontSize * CharacterSize / 10: около 20 см.
        private const float CharacterSize = 0.03f;
        private const float MinScale = 0.01f;

        private Transform _owner;
        private Transform _view;
        private TextMesh _text;

        public string Text
        {
            get => _text.text;
            set => _text.text = value;
        }

        /// <param name="owner">Персонаж; его позиция — уровень ног.</param>
        /// <param name="view">Камера, к которой табличка поворачивается лицом.</param>
        public static Nameplate Create(Transform owner, Transform view)
        {
            var label = new GameObject("Nameplate");
            label.transform.SetParent(owner, false);

            var plate = label.AddComponent<Nameplate>();
            plate._owner = owner;
            plate._view = view;
            plate._text = CreateText(label);
            return plate;
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private void LateUpdate()
        {
            // Модель может быть масштабирована: размер и высота таблички задаются в мировых единицах.
            transform.position = _owner.position + Vector3.up * HeightAboveFeet;
            transform.localScale = Vector3.one / Mathf.Max(MinScale, _owner.lossyScale.y);

            if (_view != null) transform.rotation = Quaternion.LookRotation(transform.position - _view.position);
        }

        private static TextMesh CreateText(GameObject label)
        {
            var text = label.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>(BuiltinFont);
            text.fontSize = FontSize;
            text.characterSize = CharacterSize;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return text;
        }
    }
}
