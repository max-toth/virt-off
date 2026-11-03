using UnityEngine;

namespace Game.Scripts.Audio.Synthesis
{
    public sealed class PunchSynthesizer : ISfxSynthesizer
    {
        public float Duration => 0.28f;

        public void Synthesize(float[] samples, int sampleRate, System.Random random)
        {
            float phase = 0f, lowPass = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / sampleRate;
                phase += SynthesisMath.TwoPi * Mathf.Lerp(110f, 45f, t / Duration) / sampleRate;
                lowPass += (SynthesisMath.Noise(random) - lowPass) * 0.25f;
                samples[i] = Mathf.Sin(phase) * Mathf.Exp(-18f * t) + lowPass * 1.8f * Mathf.Exp(-55f * t);
            }
        }
    }
}
