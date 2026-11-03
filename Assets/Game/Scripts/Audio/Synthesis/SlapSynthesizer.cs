using UnityEngine;

namespace Game.Scripts.Audio.Synthesis
{
    public sealed class SlapSynthesizer : ISfxSynthesizer
    {
        public float Duration => 0.2f;

        public void Synthesize(float[] samples, int sampleRate, System.Random random)
        {
            var previous = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / sampleRate;
                var noise = SynthesisMath.Noise(random);
                var highPass = noise - previous;
                previous = noise;
                samples[i] = (highPass * 0.8f + noise * 0.4f) * Mathf.Exp(-38f * t)
                             + Mathf.Sin(SynthesisMath.TwoPi * 1900f * t) * 0.15f * Mathf.Exp(-60f * t);
            }
        }
    }
}
