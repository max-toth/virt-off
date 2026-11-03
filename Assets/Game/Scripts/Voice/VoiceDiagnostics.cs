using System.Diagnostics;

namespace Game.Scripts.Voice
{
    // Включается символом VOICE_DEBUG.
    internal static class VoiceDiagnostics
    {
        private const string Symbol = "VOICE_DEBUG";

        [Conditional(Symbol)]
        public static void Log(string message)
        {
#if VOICE_DEBUG
            UnityEngine.Debug.Log(message);
#endif
        }
    }
}
