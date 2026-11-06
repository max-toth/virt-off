using System;
using Game.Scripts.Common;
using Game.Scripts.Settings;
using VContainer.Unity;

namespace Game.Scripts.Net
{
    /// <summary>Отправляет свой ник при подключении, при смене и периодически: UDP теряет пакеты,
    /// а сервер в ответ заново рассылает имя и присылает имена остальных.</summary>
    public sealed class NicknamePublisher : IInitializable, ITickable, IDisposable
    {
        private const float ResendInterval = 10f;

        private readonly PlayerNameNetwork _network;
        private readonly INetTransport _transport;
        private readonly GameSettings _settings;
        private readonly IClock _clock;
        private float _nextSendTime;

        public NicknamePublisher(PlayerNameNetwork network, INetTransport transport, GameSettings settings, IClock clock)
        {
            _network = network;
            _transport = transport;
            _settings = settings;
            _clock = clock;
        }

        public void Initialize()
        {
            _transport.IdAssigned += SendSoon;
            _settings.NicknameChanged += SendSoon;
        }

        public void Dispose()
        {
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
    }
}
