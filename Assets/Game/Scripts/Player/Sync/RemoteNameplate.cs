using Game.Scripts.Net;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    /// <summary>Ник над чужим игроком: текст в мире над головой, всегда повёрнут к камере.</summary>
    public class RemoteNameplate : MonoBehaviour
    {
        private const string BuiltinFont = "LegacyRuntime.ttf";
        private const float HeightAboveFeet = 2.05f;
        private const int FontSize = 64;
        // Высота строки в мире ≈ FontSize * CharacterSize / 10: около 20 см.
        private const float CharacterSize = 0.03f;
        private const float MinScale = 0.01f;

        private PlayerNames _names;
        private string _id;
        private Transform _label;
        private TextMesh _text;
        private string _shown;

        public static void Attach(GameObject avatar, string id, PlayerNames names)
        {
            var label = new GameObject("Nameplate");
            label.transform.SetParent(avatar.transform, false);

            var text = label.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>(BuiltinFont);
            text.fontSize = FontSize;
            text.characterSize = CharacterSize;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;

            var plate = avatar.AddComponent<RemoteNameplate>();
            plate._id = id;
            plate._names = names;
            plate._label = label.transform;
            plate._text = text;
            plate.Refresh();
        }

        private void LateUpdate()
        {
            Refresh();

            // Модель может быть масштабирована: размер и высота таблички задаются в мировых единицах.
            _label.position = transform.position + Vector3.up * HeightAboveFeet;
            _label.localScale = Vector3.one / Mathf.Max(MinScale, transform.lossyScale.y);

            var view = Camera.main;
            if (view != null) _label.rotation = Quaternion.LookRotation(_label.position - view.transform.position);
        }

        private void Refresh()
        {
            var name = _names.DisplayName(_id);
            if (name == _shown) return;

            _shown = name;
            _text.text = name;
        }
    }
}
