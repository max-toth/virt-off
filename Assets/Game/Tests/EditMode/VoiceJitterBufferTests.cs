using System.Linq;
using Game.Scripts.Voice.Playback;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class VoiceJitterBufferTests
    {
        private const int Capacity = 1000;
        private const int TargetDelay = 10;
        private const int MaxDelay = 50;
        private const short Sample = short.MaxValue;
        private const float Tolerance = 1e-5f;

        private VoiceJitterBuffer _buffer;

        [SetUp]
        public void SetUp() => _buffer = new VoiceJitterBuffer(Capacity, TargetDelay, MaxDelay);

        [Test]
        public void FirstWrite_IsDelayedBySilence()
        {
            _buffer.Write(Frame(5), 5);

            var output = Read(TargetDelay + 5);
            Assert.That(output.Take(TargetDelay), Is.All.EqualTo(0f));
            Assert.That(output.Skip(TargetDelay), Is.All.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Read_WithoutData_ReturnsSilence()
        {
            Assert.That(Read(8), Is.All.EqualTo(0f));
        }

        [Test]
        public void Read_AppliesVolume()
        {
            _buffer.Write(Frame(5), 5);
            Read(TargetDelay);

            Assert.That(Read(5, 0.5f), Is.All.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void AfterUnderrun_SilenceIsInsertedAgain()
        {
            _buffer.Write(Frame(5), 5);
            Read(8);
            Read(7);
            Read(3);

            _buffer.Write(Frame(5), 5);

            Assert.That(_buffer.Buffered, Is.EqualTo(TargetDelay + 5));
        }

        [Test]
        public void WithoutUnderrun_NoSilenceIsInserted()
        {
            _buffer.Write(Frame(5), 5);
            Read(TargetDelay + 2);

            _buffer.Write(Frame(5), 5);

            Assert.That(_buffer.Buffered, Is.EqualTo(3 + 5));
        }

        [Test]
        public void Write_BeyondMaxDelay_DropsFrame()
        {
            Assert.That(_buffer.Write(Frame(30), 30), Is.True);
            Assert.That(_buffer.Write(Frame(30), 30), Is.False);
            Assert.That(_buffer.Buffered, Is.EqualTo(TargetDelay + 30));
        }

        [Test]
        public void LargeReadBlocks_RaiseTargetDelay()
        {
            Read(40);

            _buffer.Write(Frame(5), 5);

            Assert.That(_buffer.Buffered, Is.EqualTo(40 + 5));
        }

        [Test]
        public void Ring_WrapsAround()
        {
            for (var i = 0; i < 100; i++)
            {
                _buffer.Write(Frame(20), 20);
                Read(20);
            }

            Assert.That(_buffer.Buffered, Is.EqualTo(TargetDelay));
            Assert.That(Read(TargetDelay), Is.All.EqualTo(1f).Within(Tolerance));
        }

        private static short[] Frame(int count) => Enumerable.Repeat(Sample, count).ToArray();

        private float[] Read(int count, float volume = 1f)
        {
            var output = new float[count];
            _buffer.Read(output, volume);
            return output;
        }
    }
}
