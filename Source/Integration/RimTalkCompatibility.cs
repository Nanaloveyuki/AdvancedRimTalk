using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using RimTalk;
using RimTalk.Client;
using RimTalk.Data;
using RimTalk.Prompt;
using RimTalk.Service;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkCompatibility
    {
        internal const string MinimumVersion = "1.2.13";

        internal static void Validate()
        {
            string reported = ModLister.GetActiveModWithIdentifier("cj.rimtalk", false)?.ModVersion;
            if (!Version.TryParse(reported, out Version version) || version < new Version(MinimumVersion))
                throw new InvalidOperationException("RimTalk " + MinimumVersion + "+ required; found " + (reported ?? "unknown"));
            ValidateApi();
        }

        internal static void ValidateApi()
        {
            Require(typeof(RimTalk.Settings), "Get", true, typeof(RimTalkSettings));
            Require(typeof(PromptManager), "BuildMessages", false, typeof(List<ValueTuple<Role, string>>),
                typeof(TalkRequest), typeof(List<Pawn>), typeof(string));
            Require(typeof(ScribanParser), "Render", true, typeof(string), typeof(string), typeof(PromptContext), typeof(bool));
            Require(typeof(ScribanParser), "SetSessionVar", true, typeof(void), typeof(string), typeof(object));
            Require(typeof(ScribanParser), "GetSessionVar", true, typeof(object), typeof(string));
            Require(typeof(ScribanParser), "ResetSessionVariables", true, typeof(void));
            Require(typeof(AIService), "ExecuteWithRetry", true, typeof(Task<Payload>),
                typeof(ApiLog), typeof(Func<IAIClient, Task<Payload>>), typeof(bool));
        }

        private static void Require(Type type, string name, bool isStatic, Type result, params Type[] parameters)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            MethodInfo method = type.GetMethod(name, flags, null, parameters, null);
            int overloads = 0;
            foreach (MethodInfo candidate in type.GetMethods(flags))
                if (candidate.Name == name) overloads++;
            // The current Harmony attributes select by name, so extra overloads are incompatible too.
            if (method == null || method.ReturnType != result || method.IsStatic != isStatic || overloads != 1)
                throw new MissingMethodException("Unsupported RimTalk API: " + type.FullName + "." + name);
        }
    }
}
