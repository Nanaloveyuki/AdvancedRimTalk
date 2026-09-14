using System;
using HarmonyLib;
using RimTalk.Service;

namespace AdvancedRimTalk.Integration
{
    [HarmonyPatch(typeof(TalkService), nameof(TalkService.GenerateTalk))]
    internal static class TalkServiceGenerateTalkPatch
    {
        private static void Prefix(ref IDisposable __state)
        {
            __state = RimTalkTakeoverContextScope.Enter();
        }

        private static void Finalizer(IDisposable __state)
        {
            if (__state != null)
            {
                __state.Dispose();
            }
        }
    }
}
