using System.Diagnostics;

namespace Game.Scripts.Combat.Props
{
    // Включается символом NETPROP_DEBUG.
    internal static class NetPropDiagnostics
    {
        private const string Symbol = "NETPROP_DEBUG";

        [Conditional(Symbol)]
        public static void Log(string message)
        {
#if NETPROP_DEBUG
            UnityEngine.Debug.Log($"[NetProp] {message}");
#endif
        }
    }
}
