using Game.Scripts.Player.Sync;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class PositionSendFilterTests
    {
        private const float KeepAlive = 1f;

        private PositionSendFilter _filter;

        [SetUp]
        public void SetUp()
        {
            _filter = new PositionSendFilter(KeepAlive);
            _filter.ShouldSend(Vector3.zero, 0f, false, 0f);
        }

        [Test]
        public void FirstPosition_IsSent()
        {
            Assert.That(new PositionSendFilter(KeepAlive).ShouldSend(Vector3.one, 90f, true, 0f), Is.True);
        }

        [Test]
        public void StandingStill_IsNotSent()
        {
            Assert.That(_filter.ShouldSend(Vector3.zero, 0f, false, 0.1f), Is.False);
        }

        [Test]
        public void StandingStill_IsSentOnKeepAlive()
        {
            Assert.That(_filter.ShouldSend(Vector3.zero, 0f, false, KeepAlive - 0.01f), Is.False);
            Assert.That(_filter.ShouldSend(Vector3.zero, 0f, false, KeepAlive), Is.True);
            Assert.That(_filter.ShouldSend(Vector3.zero, 0f, false, KeepAlive + 0.1f), Is.False);
        }

        [Test]
        public void Movement_IsSent()
        {
            Assert.That(_filter.ShouldSend(new Vector3(0.1f, 0f, 0f), 0f, false, 0.05f), Is.True);
        }

        [Test]
        public void Jitter_IsNotSent()
        {
            Assert.That(_filter.ShouldSend(new Vector3(0.001f, 0f, 0f), 0.1f, false, 0.05f), Is.False);
        }

        [Test]
        public void SlowDrift_IsSentOnceItAddsUp()
        {
            var sent = false;
            for (var i = 1; i <= 20 && !sent; i++)
                sent = _filter.ShouldSend(new Vector3(0.002f * i, 0f, 0f), 0f, false, 0.05f * i);

            Assert.That(sent, Is.True, "сравнение идёт с последним отправленным, а не с прошлым кадром");
        }

        [Test]
        public void Turn_IsSent()
        {
            Assert.That(_filter.ShouldSend(Vector3.zero, 5f, false, 0.05f), Is.True);
        }

        [Test]
        public void TurnAcrossZero_IsMeasuredByShortestAngle()
        {
            var filter = new PositionSendFilter(KeepAlive);
            filter.ShouldSend(Vector3.zero, 359.9f, false, 0f);

            Assert.That(filter.ShouldSend(Vector3.zero, 0.1f, false, 0.05f), Is.False);
        }

        [Test]
        public void CrouchChange_IsSent()
        {
            Assert.That(_filter.ShouldSend(Vector3.zero, 0f, true, 0.05f), Is.True);
        }
    }
}
