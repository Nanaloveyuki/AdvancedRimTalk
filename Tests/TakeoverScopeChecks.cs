using System;
using System.Reflection;
using System.Threading.Tasks;
using AdvancedRimTalk.Integration;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class TakeoverScopeChecks
    {
        internal static void Run()
        {
            AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism = true;
            RimTalk.RimTalkSettings original = RimTalk.Settings.Shared;
            var context = original.Context;
            using (var outer = RimTalkTakeoverContextScope.Enter())
            {
                var scoped = RimTalk.Settings.Get();
                Check(!ReferenceEquals(scoped, original) && !ReferenceEquals(scoped.Context, context), "independent settings and context");
                Check(ReferenceEquals(original.Context, context) && context.MaxPawnContextCount == 3, "global context untouched");
                Check(scoped.Context.MaxPawnContextCount == 32 && scoped.Context.ConversationHistoryCount == 40, "configured limits");
                var inner = RimTalkTakeoverContextScope.Enter();
                inner.Dispose();
                inner.Dispose();
                Check(ReferenceEquals(RimTalk.Settings.Get(), scoped), "double disposal preserves outer scope");
                Task.Run(() =>
                {
                    Check(ReferenceEquals(RimTalk.Settings.Get(), original), "other thread sees original");
                    using (RimTalkTakeoverContextScope.Enter())
                        Check(!ReferenceEquals(RimTalk.Settings.Get(), scoped), "other thread owns its copy");
                    Check(ReferenceEquals(RimTalk.Settings.Get(), original), "other thread restored");
                }).GetAwaiter().GetResult();
                Check(ReferenceEquals(RimTalk.Settings.Get(), scoped), "other thread did not alter outer scope");
            }
            Check(ReferenceEquals(RimTalk.Settings.Get(), original), "outer scope restored");
            try
            {
                using (RimTalkTakeoverContextScope.Enter()) throw new InvalidOperationException("fixture");
            }
            catch (InvalidOperationException) { }
            Check(ReferenceEquals(RimTalk.Settings.Get(), original), "exception unwinds scope");
            AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism = false;
            using (RimTalkTakeoverContextScope.Enter())
                Check(ReferenceEquals(RimTalk.Settings.Get(), original), "embed mode untouched");
            using (RimTalkTakeoverContextScope.Enter(true))
                Check(!ReferenceEquals(RimTalk.Settings.Get(), original), "explicit takeover preview while running embed");
            Check(ReferenceEquals(RimTalk.Settings.Get(), original), "explicit preview restored");

            AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism = true;
            using (RimTalkTakeoverContextScope.Enter(false))
                Check(ReferenceEquals(RimTalk.Settings.Get(), original), "explicit embed preview while running takeover");
            AdvancedRimTalkMod.Settings.TakeoverMaxPawnContextCount = 17;
            AdvancedRimTalkMod.Settings.TakeoverConversationHistoryCount = 0;
            context.IncludeEvents = true;
            context.MaxEventsCount = 7;
            using (RimTalkTakeoverContextScope.Enter())
            {
                var configured = RimTalk.Settings.Get().Context;
                Check(configured.MaxPawnContextCount == 17 && configured.ConversationHistoryCount == 0,
                    "nondefault configuration and zero history retained");
                Check(configured.IncludeEvents && configured.MaxEventsCount == 7, "event settings preserved");
            }
            original.Context = null;
            try
            {
                using (RimTalkTakeoverContextScope.Enter())
                    Check(ReferenceEquals(RimTalk.Settings.Get(), original), "missing host context is a no-op");
                Check(RimTalkTakeoverContextScope.CurrentSettings == null, "missing context leaves no stale scope");
            }
            finally
            {
                original.Context = context;
                AdvancedRimTalkMod.Settings = new Settings.AdvancedRimTalkSettings();
                AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism = false;
            }
        }

        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("Takeover scope: " + name);
        }
    }
}

// Host doubles exercise the production scope and postfix, not Harmony installation.
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type, string method) { }
    }
}

namespace AdvancedRimTalk
{
    internal static class AdvancedRimTalkMod
    {
        internal static bool ShouldReplaceRimTalkPromptMechanism;
        internal static Settings.AdvancedRimTalkSettings Settings = new Settings.AdvancedRimTalkSettings();
    }
}

namespace RimTalk
{
    internal sealed class RimTalkSettings
    {
        public Data.ContextSettings Context = new Data.ContextSettings { MaxPawnContextCount = 3 };
    }
    internal static class Settings
    {
        internal static readonly RimTalkSettings Shared = new RimTalkSettings();
        public static RimTalkSettings Get()
        {
            object[] args = { Shared };
            typeof(RimTalkScopedSettingsPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
            return (RimTalkSettings)args[0];
        }
    }
}

namespace RimTalk.Data
{
    internal sealed class ContextSettings
    {
        public int MaxPawnContextCount, ConversationHistoryCount, MaxEventsCount;
        public bool EnableContextOptimization, IncludeRace, IncludeNotableGenes, IncludeIdeology,
            IncludeBackstory, IncludeTraits, IncludeSkills, IncludeHealth, IncludeMood, IncludeThoughts,
            IncludeRelations, IncludeEquipment, IncludePrisonerSlaveStatus, IncludeTime, IncludeDate,
            IncludeSeason, IncludeWeather, IncludeLocationAndTemperature, IncludeTerrain, IncludeBeauty,
            IncludeCleanliness, IncludeSurroundings, IncludeWealth, IncludeEvents, IncludeTopicKeywords;
    }
}
