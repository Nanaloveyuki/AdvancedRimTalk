using HarmonyLib;
using System;
using System.Threading;
using System.Threading.Tasks;
using RimTalk.Client;
using RimTalk.Service;
using Verse;

namespace AdvancedRimTalk.Integration
{
    [HarmonyPatch(typeof(AIService), "ExecuteWithRetry")]
    internal static class RimTalkResponsePatch
    {
        private static void Postfix(ref Task<Payload> __result)
        {
            if (__result == null) return;
            __result = ProcessAsync(__result);
        }

        internal static Task<Payload> ProcessAsync(Task<Payload> task)
        {
            // Preserve even faulted OperationCanceledExceptions and multiple host exceptions.
            var completion = new TaskCompletionSource<Payload>(TaskCreationOptions.RunContinuationsAsynchronously);
            task.ContinueWith(completed =>
            {
                if (completed.IsCanceled) completion.SetCanceled();
                else if (completed.IsFaulted) completion.SetException(completed.Exception.InnerExceptions);
                else completion.SetResult(ProcessPayload(completed.Result));
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return completion.Task;
        }

        private static Payload ProcessPayload(Payload payload)
        {
            if (payload == null || !string.IsNullOrEmpty(payload.ErrorMessage)) return payload;
            try
            {
                payload.Response = RimTalkResponseProcessor.Process(
                    payload.Response, payload.Model, AdvancedRimTalkMod.Settings);
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk response processing failed; keeping the original response: " + exception);
            }
            return payload;
        }
    }
}
