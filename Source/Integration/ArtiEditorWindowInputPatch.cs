using AdvancedRimTalk.UI;
using HarmonyLib;
using Verse;

namespace AdvancedRimTalk.Integration
{
    // Non-modal editor peers must not pass clicks/keys through to the game behind them.
    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.GetsInput))]
    internal static class ArtiEditorWindowInputPatch
    {
        private static void Postfix(Window window, ref bool __result)
        {
            if (window == null && ArtiEditorWindowManager.Shared.HasWindows) __result = false;
        }
    }
}
