using UnityEngine;

namespace Game.Scripts.Audio.Synthesis
{
    internal static class SynthesisMath
    {
        public const float TwoPi = 2f * Mathf.PI;

        public static float Noise(System.Random random) => (float)random.NextDouble() * 2f - 1f;
    }
}
