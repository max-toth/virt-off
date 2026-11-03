using UnityEngine;

namespace Game.Scripts.Audio.Synthesis
{
    public sealed class WhooshSynthesizer : ISfxSynthesizer
    {
        public float Duration => 0.32f;

        public void Synthesize(float[] samples, int sampleRate, System.Random random)
        {
            var lowPass = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / sampleRate;
                var envelope = Mathf.Sin(Mathf.PI * t / Duration);
                lowPass += (SynthesisMath.Noise(random) - lowPass) * Mathf.Lerp(0.03f, 0.25f, envelope);
                samples[i] = lowPass * 2.2f * envelope;
            }
        }
    }
}
