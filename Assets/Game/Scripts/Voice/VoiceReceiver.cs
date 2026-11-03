using System;
using System.Collections.Generic;
using Game.Scripts.Net;
using Game.Scripts.Settings;
using Game.Scripts.Voice.Models;
using Game.Scripts.Voice.Playback;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Voice
{
    public class VoiceReceiver : MonoBehaviour
    {
        private const float SpeakingThreshold = 0.03f;

        [SerializeField] private Transform audioRoot;

        private readonly Dictionary<string, VoiceStream> _streams = new(StringComparer.Ordinal);
        private VoiceNetwork _network;
        private Lobby _lobby;
        private GameSettings _settings;

        public int ActiveStreamCount => _streams.Count;

        [Inject]
        public void Construct(VoiceNetwork network, Lobby lobby, GameSettings settings)
        {
            _network = network;
            _lobby = lobby;
            _settings = settings;
        }

        private void Start()
        {
            _network.Received += Receive;
            _lobby.PlayerLeft += OnPlayerLeft;
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        private void OnDestroy()
        {
            if (_network != null) _network.Received -= Receive;
            if (_lobby != null) _lobby.PlayerLeft -= OnPlayerLeft;
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;

            foreach (var stream in _streams.Values) stream.Dispose();
            _streams.Clear();
        }

        private void Receive(VoicePacket packet)
        {
            if (_lobby.IsMuted(packet.Sender)) return;

            if (!_streams.TryGetValue(packet.Sender, out var stream))
            {
                stream = new VoiceStream(packet.Sender, audioRoot != null ? audioRoot : transform, _settings);
                _streams.Add(packet.Sender, stream);
                VoiceDiagnostics.Log($"[Voice] {packet.Sender} joined");
            }

            if (stream.Feed(packet) > SpeakingThreshold) _lobby.MarkSpeaking(packet.Sender);
        }

        private void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            foreach (var stream in _streams.Values) stream.Restart();
        }

        private void OnPlayerLeft(string id)
        {
            if (!_streams.Remove(id, out var stream)) return;
            stream.Dispose();
            VoiceDiagnostics.Log($"[Voice] {id} left");
        }
    }
}
