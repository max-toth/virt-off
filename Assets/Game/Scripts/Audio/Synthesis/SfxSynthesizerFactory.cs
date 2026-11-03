namespace Game.Scripts.Audio.Synthesis
{
    public static class SfxSynthesizerFactory
    {
        public static bool TryCreate(Sfx sfx, out ISfxSynthesizer synthesizer)
        {
            synthesizer = sfx switch
            {
                Sfx.Punch => new PunchSynthesizer(),
                Sfx.Slap => new SlapSynthesizer(),
                Sfx.Whoosh => new WhooshSynthesizer(),
                Sfx.Impact => new ImpactSynthesizer(),
                _ => null
            };
            return synthesizer != null;
        }
    }
}
