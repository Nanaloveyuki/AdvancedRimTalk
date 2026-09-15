using System;
using System.Collections.Generic;
using AdvancedRimTalk.Prompt;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.Prompt;
using Verse;

namespace AdvancedRimTalk.Integration
{
    [HarmonyPatch(typeof(PromptManager), nameof(PromptManager.BuildMessages))]
    internal static class PromptManagerBuildMessagesPatch
    {
        private static void Postfix(List<ValueTuple<Role, string>> __result, bool __state)
        {
            if (__result == null) return;
            try
            {
                UI.PromptPreviewPage.Capture(__result, __state);
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk could not capture the prompt preview: " + exception);
            }
        }

        private static bool Prefix(TalkRequest talkRequest, List<Pawn> pawns, string status, ref List<ValueTuple<Role, string>> __result, out bool __state)
        {
            __state = false;
            if (!AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism)
            {
                return true;
            }

            try
            {
                List<PromptMessageSegment> segments;
                __result = AdvancedPromptMessageBuilder.Build(talkRequest, pawns, status, out segments);
                if (talkRequest != null)
                {
                    talkRequest.PromptMessageSegments = segments.Count > 0 ? segments : null;
                }

                __state = true;
                return false;
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk prompt replacement failed; falling back to RimTalk prompt builder.\n" + exception);
                return true;
            }
        }
    }
}
