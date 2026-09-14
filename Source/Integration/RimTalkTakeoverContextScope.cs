using System;
using RimTalk;
using RimTalk.Data;

namespace AdvancedRimTalk.Integration
{
    internal sealed class RimTalkTakeoverContextScope : IDisposable
    {
        [ThreadStatic]
        private static int depth;

        [ThreadStatic]
        private static ContextSettings savedContext;

        private bool active;

        private RimTalkTakeoverContextScope()
        {
        }

        public static RimTalkTakeoverContextScope Enter()
        {
            RimTalkTakeoverContextScope scope = new RimTalkTakeoverContextScope();
            if (!AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism)
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
                savedContext = settings.Context;
                settings.Context = CreateTakeoverContextSettings(savedContext);
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

            depth--;
            if (depth > 0)
            {
                return;
            }

            RimTalkSettings settings = RimTalk.Settings.Get();
            if (settings != null && savedContext != null)
            {
                settings.Context = savedContext;
            }

            savedContext = null;
            depth = 0;
            active = false;
        }

        private static ContextSettings CreateTakeoverContextSettings(ContextSettings source)
        {
            ContextSettings fallback = source ?? new ContextSettings();
            return new ContextSettings
            {
                EnableContextOptimization = false,
                MaxPawnContextCount = 9999,
                ConversationHistoryCount = 9999,
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
}
