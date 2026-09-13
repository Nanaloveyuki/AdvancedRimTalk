using System;
using AdvancedRimTalk.Prompt;
using HarmonyLib;
using RimTalk.Prompt;
using Verse;

namespace AdvancedRimTalk.Integration
{
    [HarmonyPatch(typeof(ScribanParser), nameof(ScribanParser.Render))]
    internal static class ScribanRenderPatch
    {
        private sealed class ScribanRenderState
        {
            public ArtiPromptRenderResult Arti;
            public PromptExpansionResult Placeholders;
        }

        private static void Prefix(ref string templateText, PromptContext context, out ScribanRenderState __state)
        {
            __state = null;

            ArtiPromptRenderResult arti = null;
            if (AdvancedRimTalkMod.IsArtiPromptEmbeddingEnabled
                && ArtiPromptDocumentRenderer.HasArtiBlocks(templateText))
            {
                try
                {
                    arti = ArtiPromptDocumentRenderer.Render(templateText, context);
                    templateText = arti.Text;
                }
                catch (Exception exception)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Warning("Advanced RimTalk Arti preprocessing failed: " + exception);
                    }
                }
            }

            PromptExpansionResult expansion = null;
            if (!AdvancedRimTalkMod.IsPlaceholderLayerEnabled
                || !PromptTemplateExpander.HasAdvancedExpressions(templateText))
            {
                if (arti != null)
                {
                    __state = new ScribanRenderState { Arti = arti };
                }

                return;
            }

            try
            {
                PromptSnapshot snapshot = PromptContextSnapshotFactory.From(context);
                expansion = PromptTemplateExpander.Expand(templateText, snapshot);
                templateText = expansion.Template;
            }
            catch (Exception exception)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("Advanced RimTalk placeholder preprocessing failed: " + exception);
                }
            }

            if (arti != null || expansion != null)
            {
                __state = new ScribanRenderState
                {
                    Arti = arti,
                    Placeholders = expansion
                };
            }
        }

        private static void Postfix(ref string __result, ScribanRenderState __state)
        {
            if (__state == null)
            {
                return;
            }

            if (__state.Placeholders != null)
            {
                __result = __state.Placeholders.Restore(__result);
            }

            if (Prefs.DevMode)
            {
                if (__state.Arti != null && __state.Arti.Diagnostics.Count > 0)
                {
                    Log.Warning(
                        "Advanced RimTalk Arti diagnostics:\n"
                        + string.Join("\n", __state.Arti.Diagnostics));
                }

                if (__state.Placeholders != null && __state.Placeholders.Errors.Count > 0)
                {
                    Log.Warning("Advanced RimTalk placeholders:\n" + string.Join("\n", __state.Placeholders.Errors));
                }
            }
        }
    }
}
