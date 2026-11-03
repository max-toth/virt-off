using UnityEngine.UIElements;

namespace Game.Scripts.UI
{
    public static class UiClasses
    {
        private const string Hidden = "hidden";

        public static void SetVisible(this VisualElement element, bool visible) => element.EnableInClassList(Hidden, !visible);
    }
}
