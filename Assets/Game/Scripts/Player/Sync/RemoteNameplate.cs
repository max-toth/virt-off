using Game.Scripts.Net;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    /// <summary>Ник над чужим игроком: обновляет табличку, когда приходит или меняется его имя.</summary>
    public class RemoteNameplate : MonoBehaviour
    {
        private PlayerNames _names;
        private string _id;
        private Nameplate _plate;

        /// <param name="view">Камера локального игрока.</param>
        public static void Attach(GameObject avatar, string id, PlayerNames names, Transform view)
        {
            var binding = avatar.AddComponent<RemoteNameplate>();
            binding._id = id;
            binding._names = names;
            binding._plate = Nameplate.Create(avatar.transform, view);
            binding.Refresh();
            names.Changed += binding.Refresh;
        }

        private void OnDestroy()
        {
            if (_names != null) _names.Changed -= Refresh;
        }

        private void Refresh() => _plate.Text = _names.DisplayName(_id);
    }
}
