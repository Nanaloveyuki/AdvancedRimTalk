using System;
using System.Threading;
using System.Threading.Tasks;
using AdvancedRimTalk.Integration;
using AdvancedRimTalk.Settings;
using RimTalk.Client;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ResponseProcessingChecks
    {
        internal static void Run()
        {
            Equal("{\"name\":\"Ada\",\"text\":\"Hi\"}", ResponseJsonRepair.TryRepair("```json\n{name:'Ada',text:'Hi',}\n```"));
            Equal("{\"a\":1}", ResponseJsonRepair.TryRepair("{a:1"));
            Equal("[{\"a\":1}]", ResponseJsonRepair.TryRepair("[{a:1}]"));
            Equal("{\"a\":1}\n{\"a\":2}", ResponseJsonRepair.TryRepair("{a:1}\n{a:2}"));
            Equal("{\"a\":\"{not:a,key}\"}", ResponseJsonRepair.TryRepair("{\"a\":\"{not:a,key}\"}"));
            Equal("{\"a\":\"\u201chello\u201d\"}", ResponseJsonRepair.TryRepair("{\"a\":\"\u201chello\u201d\"}"));
            Equal("{\"a\":\"hello\"}", ResponseJsonRepair.TryRepair("{\u201ca\u201d:\u201chello\u201d}"));
            Equal(null, ResponseJsonRepair.TryRepair("{a:'unfinished"));
            Equal(null, ResponseJsonRepair.TryRepair("{a:}"));
            Equal(null, ResponseJsonRepair.TryRepair("{a:1]"));
            Equal(null, ResponseJsonRepair.TryRepair("ordinary text"));
            Equal(null, ResponseJsonRepair.TryRepair("{a:1}\n{a:'unfinished"));

            var settings = new AdvancedRimTalkSettings { EnableJsonFormatting = true };
            settings.IgnoreByInterval = true;
            settings.IgnoreIntervalSeconds = 3600;
            int changed = 0;
            Parallel.For(0, 24, _ =>
            {
                if (RimTalkResponseProcessor.Process("{a:1}", "another-model", settings) != "{a:1}")
                    Interlocked.Increment(ref changed);
            });
            Check(changed == 1, "only one concurrent response may enter the global interval");
            settings.IgnoreByInterval = false;
            settings.IgnoreByModel = true;
            settings.ResponseModelIds = "model";
            Equal("{a:1}", RimTalkResponseProcessor.Process("{a:1}", null, settings));
            settings.IgnoreByModel = false;
            settings.IgnoreByRegex = true;
            settings.RegexBlacklist = true;
            settings.ResponseBlacklistRegex = "[\na:1";
            Equal("{a:1}", RimTalkResponseProcessor.Process("{a:1}", null, settings));
            settings.ResponseBlacklistRegex = "nomatch";
            Equal("{\"a\":1}", RimTalkResponseProcessor.Process("{a:1}", null, settings));

            AdvancedRimTalkMod.Settings = new AdvancedRimTalkSettings { EnableJsonFormatting = true };
            var payload = new Payload { Response = "{a:1}", Model = "fixture" };
            Check(ReferenceEquals(payload, RimTalkResponsePatch.ProcessAsync(Task.FromResult(payload)).GetAwaiter().GetResult()), "payload preserved");
            Equal("{\"a\":1}", payload.Response);
            var error = new InvalidOperationException("host failure");
            try
            {
                RimTalkResponsePatch.ProcessAsync(Task.FromException<Payload>(error)).GetAwaiter().GetResult();
                throw new Exception("Host failure swallowed");
            }
            catch (InvalidOperationException actual) { Check(ReferenceEquals(actual, error), "original exception preserved"); }
            var cancelled = RimTalkResponsePatch.ProcessAsync(Task.FromCanceled<Payload>(new CancellationToken(true)));
            try { cancelled.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { }
            Check(cancelled.IsCanceled, "cancellation state preserved");
            var hostFault = new TaskCompletionSource<Payload>();
            var cancelledException = new OperationCanceledException("fault, not cancellation");
            hostFault.SetException(new Exception[] { cancelledException, error });
            var forwarded = RimTalkResponsePatch.ProcessAsync(hostFault.Task);
            try { forwarded.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { }
            Check(forwarded.IsFaulted && forwarded.Exception.InnerExceptions.Count == 2
                && ReferenceEquals(forwarded.Exception.InnerExceptions[0], cancelledException), "all host faults retained without converting fault to cancellation");
            payload = new Payload { Response = "{a:1}", ErrorMessage = "host error" };
            RimTalkResponsePatch.ProcessAsync(Task.FromResult(payload)).GetAwaiter().GetResult();
            Equal("{a:1}", payload.Response);
            Check(RimTalkResponsePatch.ProcessAsync(Task.FromResult<Payload>(null)).GetAwaiter().GetResult() == null, "null preserved");
            AdvancedRimTalkMod.Settings = new AdvancedRimTalkSettings();
        }

        private static void Equal(string expected, string actual)
        {
            Check(expected == actual, "Expected " + expected + "; actual " + actual);
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Response processing: " + message);
        }
    }
}

namespace RimTalk.Client
{
    internal sealed class Payload
    {
        public string Response;
        public string Model;
        public string ErrorMessage;
    }
}
namespace RimTalk.Service
{
    internal static class AIService { }
}
