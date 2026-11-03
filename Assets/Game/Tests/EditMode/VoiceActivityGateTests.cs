using Game.Scripts.Voice.Capture;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class VoiceActivityGateTests
    {
        private const float Threshold = 0.1f;
        private const int Hangover = 2;

        private static readonly float[] Silence = { 0f, 0.01f, -0.02f };
        private static readonly float[] Speech = { 0f, 0.5f, -0.3f };
        private static readonly float[] Quiet = { 0.05f, -0.06f };

        private VoiceActivityGate _gate;

        [SetUp]
        public void SetUp() => _gate = new VoiceActivityGate(Threshold, Hangover);

        [Test]
        public void Silence_IsNotSent()
        {
            Assert.That(_gate.ShouldSend(Silence, 1f), Is.False);
            Assert.That(_gate.IsOpen, Is.False);
        }

        [Test]
        public void Speech_IsSent()
        {
            Assert.That(_gate.ShouldSend(Speech, 1f), Is.True);
            Assert.That(_gate.IsOpen, Is.True);
        }

        [Test]
        public void NegativePeak_CountsAsSpeech()
        {
            Assert.That(_gate.ShouldSend(new[] { -0.4f }, 1f), Is.True);
        }

        [Test]
        public void Gain_IsApplied()
        {
            Assert.That(_gate.ShouldSend(Quiet, 1f), Is.False);
            Assert.That(_gate.ShouldSend(Quiet, 2f), Is.True);
        }

        [Test]
        public void AfterSpeech_HangoverFramesAreSent_ThenClosed()
        {
            _gate.ShouldSend(Speech, 1f);

            for (var i = 0; i < Hangover; i++)
                Assert.That(_gate.ShouldSend(Silence, 1f), Is.True, $"кадр {i} после речи");

            Assert.That(_gate.ShouldSend(Silence, 1f), Is.False);
        }

        [Test]
        public void SpeechDuringHangover_ExtendsIt()
        {
            _gate.ShouldSend(Speech, 1f);
            _gate.ShouldSend(Silence, 1f);
            _gate.ShouldSend(Speech, 1f);

            for (var i = 0; i < Hangover; i++)
                Assert.That(_gate.ShouldSend(Silence, 1f), Is.True);
            Assert.That(_gate.ShouldSend(Silence, 1f), Is.False);
        }

        [Test]
        public void Close_DropsHangover()
        {
            _gate.ShouldSend(Speech, 1f);

            _gate.Close();

            Assert.That(_gate.IsOpen, Is.False);
            Assert.That(_gate.ShouldSend(Silence, 1f), Is.False);
        }
    }
}
