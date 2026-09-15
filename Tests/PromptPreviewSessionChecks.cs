using System;
using System.Threading.Tasks;
using AdvancedRimTalk.Integration;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class PromptPreviewSessionChecks
    {
        internal static void Run()
        {
            using (new PromptPreviewSession())
            {
                PromptPreviewSession.Variables["key"] = "outer";
                var nested = new PromptPreviewSession();
                if (PromptPreviewSession.Variables.Count != 0) throw new Exception("Nested preview leaked variables.");
                nested.Dispose();
                nested.Dispose();
                if (!Equals(PromptPreviewSession.Variables["key"], "outer")) throw new Exception("Outer preview lost variables.");
                Task.Run(() =>
                {
                    if (PromptPreviewSession.Variables != null) throw new Exception("Preview variables crossed threads.");
                }).GetAwaiter().GetResult();
            }
            if (PromptPreviewSession.Variables != null) throw new Exception("Preview variables survived disposal.");
        }
    }
}
