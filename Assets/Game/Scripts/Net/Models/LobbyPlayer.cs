namespace Game.Scripts.Net.Models
{
    public sealed class LobbyPlayer
    {
        private const float SpeakingHoldSeconds = 0.3f;

        private float _lastVoiceTime = -100f;

        public LobbyPlayer(string id) => Id = id;

        public string Id { get; }
        public bool Muted { get; set; }
        internal float LastSeen { get; set; }

        /// <param name="now">Время по IClock.UnscaledTime.</param>
        public bool IsSpeaking(float now) => now - _lastVoiceTime < SpeakingHoldSeconds;

        internal void MarkSpeaking(float now) => _lastVoiceTime = now;
    }
}
