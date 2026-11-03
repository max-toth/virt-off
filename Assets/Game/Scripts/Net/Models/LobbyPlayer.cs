using UnityEngine;

namespace Game.Scripts.Net.Models
{
    public sealed class LobbyPlayer
    {
        private const float SpeakingHoldSeconds = 0.3f;

        private float _lastVoiceTime = -100f;

        public LobbyPlayer(string id) => Id = id;

        public string Id { get; }
        public bool Muted { get; set; }
        public bool IsSpeaking => Time.unscaledTime - _lastVoiceTime < SpeakingHoldSeconds;
        internal float LastSeen { get; set; }

        internal void MarkSpeaking() => _lastVoiceTime = Time.unscaledTime;
    }
}
