using System;
using System.Reflection;
using AdvancedRimTalk.Integration;
using RimTalk.Prompt;
using Scriban;
using Scriban.Runtime;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class PreviewSessionPatchChecks
    {
        internal static void Run()
        {
            ScribanParser.SetSessionVar("key", "real");
            using (new PromptPreviewSession())
            {
                var globals = new ScriptObject();
                globals.Import("setvar", new Action<string, object>(Set));
                globals.Import("getvar", new Func<string, object>(Get));
                var context = new TemplateContext();
                context.PushGlobal(globals);
                string output = Template.Parse("{{ setvar 'key' 'local' }}{{ getvar 'key' }}").Render(context);
                if (output != "local" || !Equals(ScribanParser.GetSessionVar("key"), "real"))
                    throw new Exception("Native preview modified the real session.");
                bool runOriginal = Invoke(typeof(PreviewResetSessionVariablesPatch), new object[0]);
                if (runOriginal || PromptPreviewSession.Variables.Count != 0)
                    throw new Exception("Preview reset did not target local variables.");
                if (!Equals(ScribanParser.GetSessionVar("key"), "real"))
                    throw new Exception("Preview reset touched the real session.");
            }
            if (!Equals(Get("key"), "real")) throw new Exception("Normal getvar was not restored.");
            Set("key", "normal");
            if (!Equals(ScribanParser.GetSessionVar("key"), "normal"))
                throw new Exception("Normal setvar was intercepted.");
        }

        private static void Set(string key, object value)
        {
            if (Invoke(typeof(PreviewSetSessionVariablePatch), new[] { (object)key, value }))
                ScribanParser.SetSessionVar(key, value);
        }
        private static object Get(string key)
        {
            object[] arguments = { key, null };
            return Invoke(typeof(PreviewGetSessionVariablePatch), arguments)
                ? ScribanParser.GetSessionVar(key) : arguments[1];
        }
        private static bool Invoke(Type patch, object[] arguments)
        {
            return (bool)patch.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, arguments);
        }
    }
}
