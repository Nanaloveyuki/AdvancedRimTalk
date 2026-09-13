using System;
using System.Globalization;
using AdvancedRimTalk.Prompt;
using RimTalk.Data;
using RimTalk.Prompt;
using RimTalk.Util;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class PromptContextSnapshotFactory
    {
        public static PromptSnapshot From(PromptContext context)
        {
            PromptSnapshot snapshot = new PromptSnapshot();
            TalkRequest request = context == null ? null : context.TalkRequest;

            snapshot.PawnName = GetPawnName(context == null ? null : context.CurrentPawn);
            snapshot.RecipientName = GetPawnName(request == null ? null : request.Recipient);
            snapshot.TalkType = request == null ? string.Empty : request.TalkType.ToString();
            snapshot.DialogueType = context == null ? string.Empty : context.DialogueType ?? string.Empty;
            snapshot.Intent = context == null ? string.Empty : context.Intent ?? string.Empty;
            snapshot.Topic = context == null ? string.Empty : context.ConversationTopic ?? string.Empty;
            snapshot.Status = context == null ? string.Empty : context.DialogueStatus ?? string.Empty;
            snapshot.Prompt = request == null ? (context == null ? string.Empty : context.DialoguePrompt ?? string.Empty) : request.Prompt ?? (context.DialoguePrompt ?? string.Empty);
            snapshot.RawPrompt = request == null ? string.Empty : request.RawPrompt ?? string.Empty;
            snapshot.Context = request == null ? (context == null ? string.Empty : context.PawnContext ?? string.Empty) : request.Context ?? (context.PawnContext ?? string.Empty);
            snapshot.PawnContext = context == null ? string.Empty : context.PawnContext ?? string.Empty;
            snapshot.IsAnnouncement = request != null && request.IsAnnouncement;
            snapshot.IsMonologue = context != null && context.IsMonologue;
            snapshot.State = request == null ? string.Empty : request.Status.ToString();

            try
            {
                TickManager tickManager = Find.TickManager;
                if (tickManager != null)
                {
                    snapshot.Tick = tickManager.TicksGame;
                }
            }
            catch (Exception)
            {
                snapshot.Tick = -1;
            }

            try
            {
                CommonUtil.InGameData gameData = CommonUtil.GetInGameData();
                snapshot.Hour = gameData.Hour12HString ?? string.Empty;
                snapshot.Date = gameData.DateString ?? string.Empty;
                snapshot.Season = gameData.SeasonString ?? string.Empty;
                snapshot.Weather = gameData.WeatherString ?? string.Empty;
            }
            catch (Exception)
            {
                // The game can be between map/world initialization here.
            }

            Map map = context == null ? null : context.Map;
            try
            {
                if (map != null && map.mapTemperature != null)
                {
                    snapshot.Temperature = map.mapTemperature.OutdoorTemp.ToString("0.##", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                snapshot.Temperature = string.Empty;
            }

            try
            {
                if (map != null && map.wealthWatcher != null)
                {
                    snapshot.Wealth = map.wealthWatcher.WealthTotal.ToString("0.##", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                snapshot.Wealth = string.Empty;
            }

            return snapshot;
        }

        private static string GetPawnName(Pawn pawn)
        {
            return pawn == null ? string.Empty : pawn.LabelShort ?? string.Empty;
        }
    }
}
