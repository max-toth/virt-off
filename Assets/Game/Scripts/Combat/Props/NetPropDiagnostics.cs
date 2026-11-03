using System.Diagnostics;

namespace Game.Scripts.Combat.Props
{
    // Логи сетевых событий предметов. Включаются символом NETPROP_DEBUG
    // (Project Settings → Player → Scripting Define Symbols); без него вызовы вырезаются компилятором.
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
