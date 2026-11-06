using Game.Scripts.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    /// <summary>Поле ника в меню паузы. Ник применяется по Enter, при уходе из поля и при закрытии меню,
    /// а не на каждую букву: каждая смена рассылается всем игрокам.</summary>
    internal sealed class NicknameEditor
    {
        private readonly TextField _field;
        private readonly GameSettings _settings;
        // Ник правили в этом поле: иначе Apply ничего не трогает, ник могли сменить на стартовой странице.
        private bool _edited;

        public NicknameEditor(TextField field, GameSettings settings)
        {
            _field = field;
            _settings = settings;
            _field.maxLength = GameSettings.MaxNicknameLength;
            _field.RegisterValueChangedCallback(_ => _edited = true);
            _field.RegisterCallback<FocusOutEvent>(_ => Apply());
            _field.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        /// <summary>Показывает текущий ник.</summary>
        public void Load()
        {
            _field.SetValueWithoutNotify(_settings.Nickname);
            _edited = false;
        }

        public void Apply()
        {
            if (!_edited) return;

            _settings.Nickname = _field.value;
            _settings.Save();
            // Сеттер обрезает пробелы и длину: в поле остаётся то, что увидят другие.
            Load();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;

            Apply();
            _field.Blur();
        }
    }
}
