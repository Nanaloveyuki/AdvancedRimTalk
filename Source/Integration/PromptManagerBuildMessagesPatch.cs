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
        private static bool Prefix(TalkRequest talkRequest, List<Pawn> pawns, string status, ref List<ValueTuple<Role, string>> __result)
        {
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
