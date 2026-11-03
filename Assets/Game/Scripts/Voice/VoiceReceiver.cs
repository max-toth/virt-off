using System;
using System.Collections.Generic;
using Game.Scripts.Net;
using Game.Scripts.Voice.Models;
using Game.Scripts.Voice.Playback;
using UnityEngine;

namespace Game.Scripts.Voice
{
    public class VoiceReceiver : MonoBehaviour
    {
        private const float SpeakingThreshold = 0.03f;

        [SerializeField] private Transform audioRoot;

        private readonly Dictionary<string, VoiceStream> _streams = new(StringComparer.Ordinal);

        public int ActiveStreamCount => _streams.Count;

        private void Awake()
        {
            VoiceNetwork.Received += Receive;
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
            Lobby.PlayerLeft += OnPlayerLeft;
        }

        private void OnDestroy()
        {
            VoiceNetwork.Received -= Receive;
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
            Lobby.PlayerLeft -= OnPlayerLeft;

            foreach (var stream in _streams.Values) stream.Dispose();
            _streams.Clear();
        }

        private void Receive(VoicePacket packet)
        {
            if (Lobby.IsMuted(packet.Sender)) return;

            if (!_streams.TryGetValue(packet.Sender, out var stream))
            {
                stream = new VoiceStream(packet.Sender, audioRoot != null ? audioRoot : transform);
                _streams.Add(packet.Sender, stream);
                VoiceDiagnostics.Log($"[Voice] {packet.Sender} joined");
            }

            if (stream.Feed(packet.Data, packet.Offset, packet.Count) > SpeakingThreshold)
                Lobby.MarkSpeaking(packet.Sender);
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
