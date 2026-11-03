using System;
using UnityEngine.UIElements;

namespace Game.Scripts.UI.Menu
{
    internal sealed class PlayerRowView
    {
        private const string SpeakingClass = "player-row__dot--speaking";
        private const string MutedClass = "player-row__dot--muted";

        private readonly VisualElement _dot;
        private readonly Label _name;
        private readonly Label _state;
        private readonly Button _action;

        public PlayerRowView(VisualTreeAsset template, Action onAction)
        {
            Root = template.Instantiate();
            _dot = Root.Q("dot");
            _name = Root.Q<Label>("nameLabel");
            _state = Root.Q<Label>("stateLabel");
            _action = Root.Q<Button>("actionButton");
            _action.clicked += onAction;
        }

        public VisualElement Root { get; }

        public void SetName(string text) => _name.text = text;

        public void Show(bool muted, bool speaking, string stateText, string actionText)
        {
            _dot.EnableInClassList(MutedClass, muted);
            _dot.EnableInClassList(SpeakingClass, !muted && speaking);
            _state.text = stateText;
            _action.text = actionText;
        }
    }
}
