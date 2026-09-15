using System;
using System.Reflection;
using HarmonyLib;
using RimTalk;
using RimTalk.Data;

namespace AdvancedRimTalk.Integration
{
    internal sealed class RimTalkTakeoverContextScope : IDisposable
    {
        [ThreadStatic]
        private static int depth;

        [ThreadStatic]
        private static RimTalkSettings scopedSettings;

        private static readonly MethodInfo CloneMethod = typeof(object).GetMethod(
            "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static RimTalkSettings CurrentSettings => scopedSettings;

        private bool active;

        private RimTalkTakeoverContextScope()
        {
        }

        public static RimTalkTakeoverContextScope Enter(bool? takeover = null)
        {
            RimTalkTakeoverContextScope scope = new RimTalkTakeoverContextScope();
            if (!(takeover ?? AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism))
            {
                return scope;
            }

            RimTalkSettings settings = RimTalk.Settings.Get();
            if (settings == null || settings.Context == null)
            {
                return scope;
            }

            if (depth == 0)
            {
                RimTalkSettings copy = (RimTalkSettings)CloneMethod.Invoke(settings, null);
                copy.Context = CreateTakeoverContextSettings(settings.Context);
                scopedSettings = copy;
            }

            depth++;
            scope.active = true;
            return scope;
        }

        public void Dispose()
        {
            if (!active)
            {
                return;
            }

            active = false;
            depth--;
            if (depth > 0)
            {
                return;
            }

            scopedSettings = null;
            depth = 0;
        }

        private static ContextSettings CreateTakeoverContextSettings(ContextSettings source)
        {
            ContextSettings fallback = source ?? new ContextSettings();
            return new ContextSettings
            {
                EnableContextOptimization = false,
                MaxPawnContextCount = AdvancedRimTalkMod.Settings == null ? 32 : AdvancedRimTalkMod.Settings.TakeoverMaxPawnContextCount,
                ConversationHistoryCount = AdvancedRimTalkMod.Settings == null ? 40 : AdvancedRimTalkMod.Settings.TakeoverConversationHistoryCount,
                IncludeRace = true,
                IncludeNotableGenes = true,
                IncludeIdeology = true,
                IncludeBackstory = true,
                IncludeTraits = true,
                IncludeSkills = true,
                IncludeHealth = true,
                IncludeMood = true,
                IncludeThoughts = true,
                IncludeRelations = true,
                IncludeEquipment = true,
                IncludePrisonerSlaveStatus = true,
                IncludeTime = true,
                IncludeDate = true,
                IncludeSeason = true,
                IncludeWeather = true,
                IncludeLocationAndTemperature = true,
                IncludeTerrain = true,
                IncludeBeauty = true,
                IncludeCleanliness = true,
                IncludeSurroundings = true,
                IncludeWealth = true,
                IncludeEvents = fallback.IncludeEvents,
                MaxEventsCount = fallback.MaxEventsCount,
                IncludeTopicKeywords = true
            };
        }
    }

    [HarmonyPatch(typeof(RimTalk.Settings), nameof(RimTalk.Settings.Get))]
    internal static class RimTalkScopedSettingsPatch
    {
        private static void Postfix(ref RimTalkSettings __result)
        {
            if (RimTalkTakeoverContextScope.CurrentSettings != null)
                __result = RimTalkTakeoverContextScope.CurrentSettings;
        }
    }
}
