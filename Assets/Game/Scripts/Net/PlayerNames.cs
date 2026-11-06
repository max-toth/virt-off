using System;
using System.Collections.Generic;
using Game.Scripts.Common;
using Game.Scripts.Settings;
using VContainer.Unity;

namespace Game.Scripts.Net
{
    /// <summary>Имена игроков по id. Свой ник отправляется при подключении, при смене и периодически:
    /// UDP теряет пакеты, а сервер в ответ заново рассылает имя и присылает имена остальных.</summary>
    public sealed class PlayerNames : IInitializable, ITickable, IDisposable
    {
        private const float ResendInterval = 10f;

        private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
        private readonly PlayerNameNetwork _network;
        private readonly Lobby _lobby;
        private readonly INetTransport _transport;
        private readonly GameSettings _settings;
        private readonly IClock _clock;
        private float _nextSendTime;

        public PlayerNames(PlayerNameNetwork network, Lobby lobby, INetTransport transport, GameSettings settings, IClock clock)
        {
            _network = network;
            _lobby = lobby;
            _transport = transport;
            _settings = settings;
            _clock = clock;
        }

        /// <summary>Чьё-то имя появилось, сменилось или исчезло.</summary>
        public event Action Changed;

        /// <returns>Имя игрока или null, если он его не задал.</returns>
        public string Get(string id) => id != null && _names.TryGetValue(id, out var name) ? name : null;

        public string DisplayName(string id) => Get(id) ?? $"Игрок {id}";

        public void Initialize()
        {
            _network.Received += OnReceived;
            _lobby.PlayerLeft += OnPlayerLeft;
            _transport.IdAssigned += SendSoon;
            _settings.NicknameChanged += SendSoon;
        }

        public void Dispose()
        {
            _network.Received -= OnReceived;
            _lobby.PlayerLeft -= OnPlayerLeft;
            _transport.IdAssigned -= SendSoon;
            _settings.NicknameChanged -= SendSoon;
        }

        public void Tick()
        {
            if (!_transport.IsConnected || _settings.Nickname.Length == 0) return;

            var now = _clock.UnscaledTime;
            if (now < _nextSendTime) return;

            _nextSendTime = now + ResendInterval;
            _network.Send(_settings.Nickname);
        }

        private void SendSoon() => _nextSendTime = 0f;

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
