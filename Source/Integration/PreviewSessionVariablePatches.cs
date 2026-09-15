using HarmonyLib;
using RimTalk.Prompt;

namespace AdvancedRimTalk.Integration
{
    [HarmonyPatch(typeof(ScribanParser), nameof(ScribanParser.SetSessionVar))]
    internal static class PreviewSetSessionVariablePatch
    {
        private static bool Prefix(string key, object value)
        {
            if (PromptPreviewSession.Variables == null) return true;
            PromptPreviewSession.Variables[key] = value;
            return false;
        }
    }

    [HarmonyPatch(typeof(ScribanParser), nameof(ScribanParser.GetSessionVar))]
    internal static class PreviewGetSessionVariablePatch
    {
        private static bool Prefix(string key, ref object __result)
        {
            if (PromptPreviewSession.Variables == null) return true;
            PromptPreviewSession.Variables.TryGetValue(key, out __result);
            return false;
        }
    }

    [HarmonyPatch(typeof(ScribanParser), nameof(ScribanParser.ResetSessionVariables))]
    internal static class PreviewResetSessionVariablesPatch
    {
        private static bool Prefix()
        {
            if (PromptPreviewSession.Variables == null) return true;
            PromptPreviewSession.Variables.Clear();
            return false;
        }
    }
}
