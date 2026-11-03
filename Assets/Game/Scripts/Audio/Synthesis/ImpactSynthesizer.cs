using UnityEngine;

namespace Game.Scripts.Audio.Synthesis
{
    public sealed class ImpactSynthesizer : ISfxSynthesizer
    {
        public float Duration => 0.35f;

        public void Synthesize(float[] samples, int sampleRate, System.Random random)
        {
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / sampleRate;
                samples[i] = Mathf.Sin(SynthesisMath.TwoPi * 170f * t) * Mathf.Exp(-22f * t)
                             + Mathf.Sin(SynthesisMath.TwoPi * 420f * t) * 0.6f * Mathf.Exp(-35f * t)
                             + Mathf.Sin(SynthesisMath.TwoPi * 780f * t) * 0.35f * Mathf.Exp(-50f * t)
                             + SynthesisMath.Noise(random) * 0.5f * Mathf.Exp(-90f * t);
            }
        }
    }
}
