using UnityEngine;

namespace Game.Scripts.Audio.Synthesis
{
    public static class ProceduralClip
    {
        private const int SampleRate = 44100;
        private const float AttackRate = 2000f;
        private const float Gain = 0.7f;

        public static AudioClip Create(string name, ISfxSynthesizer synthesizer, int seed)
        {
            var samples = new float[(int)(synthesizer.Duration * SampleRate)];
            synthesizer.Synthesize(samples, SampleRate, new System.Random(seed));
            ApplyAttack(samples);

            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void ApplyAttack(float[] samples)
        {
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / SampleRate;
                samples[i] = Mathf.Clamp(samples[i] * Mathf.Clamp01(t * AttackRate) * Gain, -1f, 1f);
            }
        }
    }
}
