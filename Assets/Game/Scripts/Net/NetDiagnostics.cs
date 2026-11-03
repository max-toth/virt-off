using System.Diagnostics;

namespace Game.Scripts.Net
{
    // Включается символом NET_DEBUG.
    internal static class NetDiagnostics
    {
        private const string Symbol = "NET_DEBUG";

        [Conditional(Symbol)]
        public static void Log(string message)
        {
#if NET_DEBUG
            UnityEngine.Debug.Log(message);
#endif
        }
    }
}
