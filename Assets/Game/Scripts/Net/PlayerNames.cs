using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace Game.Scripts.Net
{
    /// <summary>Имена игроков по id из PLAYER_NAME; имя ушедшего игрока забывается.</summary>
    public sealed class PlayerNames : IInitializable, IDisposable
    {
        private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
        private readonly PlayerNameNetwork _network;
        private readonly Lobby _lobby;

        public PlayerNames(PlayerNameNetwork network, Lobby lobby)
        {
            _network = network;
            _lobby = lobby;
        }

        /// <summary>Чьё-то имя появилось, сменилось или исчезло.</summary>
        public event Action Changed;

        /// <returns>Имя игрока или null, если он его не задал.</returns>
        public string Get(string id) => id != null && _names.TryGetValue(id, out var name) ? name : null;

        /// <returns>Имя игрока или «Игрок id», если он его не задал.</returns>
        public string DisplayName(string id) => Get(id) ?? $"Игрок {id}";

        public void Initialize()
        {
            _network.Received += OnReceived;
            _lobby.PlayerLeft += OnPlayerLeft;
        }

        public void Dispose()
        {
            _network.Received -= OnReceived;
            _lobby.PlayerLeft -= OnPlayerLeft;
        }

        private void OnReceived(string id, string name)
        {
            if (_names.TryGetValue(id, out var known) && known == name) return;

            _names[id] = name;
            Changed?.Invoke();
        }

        private void OnPlayerLeft(string id)
        {
            if (_names.Remove(id)) Changed?.Invoke();
        }
    }
}
