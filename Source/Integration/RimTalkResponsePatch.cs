using HarmonyLib;
using System.Threading.Tasks;
using RimTalk.Client;
using RimTalk.Service;

namespace AdvancedRimTalk.Integration
{
    [HarmonyPatch(typeof(AIService), "ExecuteWithRetry")]
    internal static class RimTalkResponsePatch
    {
        private static void Postfix(ref Task<Payload> __result)
        {
            if (__result == null) return;
            __result = __result.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion && task.Result != null)
                {
                    Payload payload = task.Result;
                    payload.Response = RimTalkResponseProcessor.Process(
                        payload.Response,
                        payload.Model,
                        AdvancedRimTalk.AdvancedRimTalkMod.Settings);
                }
                return task.Result;
            }, TaskScheduler.Default);
        }
    }
}
