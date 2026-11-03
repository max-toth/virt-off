namespace Game.Scripts.Audio.Synthesis
{
    public interface ISfxSynthesizer
    {
        float Duration { get; }

        void Synthesize(float[] samples, int sampleRate, System.Random random);
    }
}
