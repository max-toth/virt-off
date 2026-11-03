using Game.Scripts.Voice.Playback;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class VoiceSequenceTrackerTests
    {
        private VoiceSequenceTracker _tracker;

        [SetUp]
        public void SetUp() => _tracker = new VoiceSequenceTracker();

        [Test]
        public void FirstFrame_IsAccepted()
        {
            Assert.That(_tracker.Accept(500), Is.Zero);
        }

        [Test]
        public void ConsecutiveFrames_HaveNoLoss()
        {
            _tracker.Accept(10);

            Assert.That(_tracker.Accept(11), Is.Zero);
            Assert.That(_tracker.Accept(12), Is.Zero);
        }

        [Test]
        public void Gap_ReportsLostFrames()
        {
            _tracker.Accept(10);

            Assert.That(_tracker.Accept(13), Is.EqualTo(2));
        }

        [Test]
        public void LongGap_IsNotConcealed()
        {
            _tracker.Accept(10);

            Assert.That(_tracker.Accept((ushort)(11 + VoiceSequenceTracker.MaxConcealedFrames + 1)), Is.Zero);
        }

        [Test]
        public void Duplicate_IsDropped()
        {
            _tracker.Accept(10);

            Assert.That(_tracker.Accept(10), Is.EqualTo(VoiceSequenceTracker.Drop));
        }

        [Test]
        public void LateFrame_IsDropped_AndDoesNotMoveBack()
        {
            _tracker.Accept(10);
            _tracker.Accept(12);

            Assert.That(_tracker.Accept(11), Is.EqualTo(VoiceSequenceTracker.Drop));
            Assert.That(_tracker.Accept(13), Is.Zero);
        }

        [Test]
        public void Counter_WrapsAround()
        {
            _tracker.Accept(ushort.MaxValue);

            Assert.That(_tracker.Accept(0), Is.Zero);
            Assert.That(_tracker.Accept(2), Is.EqualTo(1));
        }

        [Test]
        public void SenderRestart_Resynchronizes()
        {
            _tracker.Accept(5000);

            Assert.That(_tracker.Accept(0), Is.Zero);
            Assert.That(_tracker.Accept(1), Is.Zero);
        }
    }
}
