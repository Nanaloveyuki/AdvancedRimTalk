using System.Collections.Generic;
using AdvancedRimTalk.Prompt;
using RimTalk.Prompt;
using Verse;

namespace AdvancedRimTalk.Settings
{
    public sealed class AdvancedRimTalkSettings : ModSettings
    {
        public const string DefaultTakeoverArtiPromptDocument =
            "You are writing an in-character RimWorld roleplay scene, not operating an agent.\n"
            + "Use the supplied facts and preserve player agency.\n\n"
            + "{{%\n"
            + "core.emit(ctx.pawn_context)\n"
            + "%}}";

        public bool EnablePlaceholderLayer = true;
        public bool ReplaceRimTalkPromptMechanism = false;
        public List<ArtiPromptPart> TakeoverPromptParts = new List<ArtiPromptPart>();
        public List<ArtiPromptPreset> TakeoverPresets = new List<ArtiPromptPreset>();
        public string ActiveTakeoverPresetId = string.Empty;
        public int ArtiEditorUndoLimit = 100;
        public int ArtiEditorCompletionLimit = 5;
        public int TakeoverMaxPawnContextCount = 32;
        public int TakeoverConversationHistoryCount = 40;
        public int TakeoverPromptCharacterBudget = 24000;
        public bool EnableResponseProcessing = true;
        public bool EnableJsonFormatting = false;
        public bool IgnoreByRegex = false;
        public bool IgnoreByInterval = false;
        public bool IgnoreByModel = false;
        public float IgnoreIntervalSeconds = 0f;
        public bool RegexWhitelist = false;
        public bool RegexBlacklist = true;
        public string ResponseWhitelistRegex = string.Empty;
        public string ResponseBlacklistRegex = string.Empty;
        public string ResponseModelIds = string.Empty;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnablePlaceholderLayer, "enablePlaceholderLayer", true);
            Scribe_Values.Look(ref ReplaceRimTalkPromptMechanism, "replaceRimTalkPromptMechanism", false);
            Scribe_Collections.Look(ref TakeoverPromptParts, "takeoverPromptParts", LookMode.Deep);
            Scribe_Collections.Look(ref TakeoverPresets, "takeoverPresets", LookMode.Deep);
            Scribe_Values.Look(ref ActiveTakeoverPresetId, "activeTakeoverPresetId", string.Empty);
            Scribe_Values.Look(ref ArtiEditorUndoLimit, "artiEditorUndoLimit", 100);
            Scribe_Values.Look(ref ArtiEditorCompletionLimit, "artiEditorCompletionLimit", 5);
            Scribe_Values.Look(ref TakeoverMaxPawnContextCount, "takeoverMaxPawnContextCount", 32);
            Scribe_Values.Look(ref TakeoverConversationHistoryCount, "takeoverConversationHistoryCount", 40);
            Scribe_Values.Look(ref TakeoverPromptCharacterBudget, "takeoverPromptCharacterBudget", 24000);
            Scribe_Values.Look(ref EnableResponseProcessing, "enableResponseProcessing", true);
            Scribe_Values.Look(ref EnableJsonFormatting, "enableJsonFormatting", false);
            Scribe_Values.Look(ref IgnoreByRegex, "ignoreByRegex", false);
            Scribe_Values.Look(ref IgnoreByInterval, "ignoreByInterval", false);
            Scribe_Values.Look(ref IgnoreByModel, "ignoreByModel", false);
            Scribe_Values.Look(ref IgnoreIntervalSeconds, "ignoreIntervalSeconds", 0f);
            Scribe_Values.Look(ref RegexWhitelist, "regexWhitelist", false);
            Scribe_Values.Look(ref RegexBlacklist, "regexBlacklist", true);
            Scribe_Values.Look(ref ResponseWhitelistRegex, "responseWhitelistRegex", string.Empty);
            Scribe_Values.Look(ref ResponseBlacklistRegex, "responseBlacklistRegex", string.Empty);
            Scribe_Values.Look(ref ResponseModelIds, "responseModelIds", string.Empty);
            EnsureTakeoverPromptParts();

            if (ArtiEditorUndoLimit < 0)
            {
                ArtiEditorUndoLimit = 0;
            }
            else if (ArtiEditorUndoLimit > 500)
            {
                ArtiEditorUndoLimit = 500;
            }
            ArtiEditorCompletionLimit = System.Math.Max(1, System.Math.Min(9, ArtiEditorCompletionLimit));
            TakeoverMaxPawnContextCount = System.Math.Max(1, System.Math.Min(256, TakeoverMaxPawnContextCount));
            TakeoverConversationHistoryCount = System.Math.Max(0, System.Math.Min(500, TakeoverConversationHistoryCount));
            TakeoverPromptCharacterBudget = System.Math.Max(4000, System.Math.Min(200000, TakeoverPromptCharacterBudget));
        }

        public void EnsureTakeoverPromptParts()
        {
            if (TakeoverPresets == null) TakeoverPresets = new List<ArtiPromptPreset>();
            TakeoverPresets.RemoveAll(preset => preset == null);
            foreach (var preset in TakeoverPresets) preset.Normalize();
            if (TakeoverPresets.Count > 0)
            {
                var active = TakeoverPresets.Find(preset => preset.Id == ActiveTakeoverPresetId);
                if (active == null)
                {
                    active = TakeoverPresets[0];
                    ActiveTakeoverPresetId = active.Id;
                    TakeoverPromptParts = active.Parts;
                }
                else
                {
                    active.Parts = TakeoverPromptParts ?? active.Parts;
                }
            }
            if (TakeoverPromptParts == null || TakeoverPromptParts.Count == 0)
            {
                TakeoverPromptParts = ArtiPromptPart.CreateDefaultParts(DefaultTakeoverArtiPromptDocument);
            }

            foreach (ArtiPromptPart part in TakeoverPromptParts)
            {
                if (part != null)
                {
                    part.Normalize();
                }
            }

            TakeoverPromptParts.RemoveAll(part => part == null);
            if (TakeoverPromptParts.Count == 0)
            {
                TakeoverPromptParts = ArtiPromptPart.CreateDefaultParts(DefaultTakeoverArtiPromptDocument);
            }
            if (TakeoverPresets.Count == 0)
            {
                var initial = new ArtiPromptPreset { Parts = TakeoverPromptParts };
                TakeoverPresets.Add(initial);
                ActiveTakeoverPresetId = initial.Id;
            }
            TakeoverPresets.Find(preset => preset.Id == ActiveTakeoverPresetId).Parts = TakeoverPromptParts;
        }

        public ArtiPromptPreset ActiveTakeoverPreset
        {
            get
            {
                EnsureTakeoverPromptParts();
                return TakeoverPresets.Find(preset => preset.Id == ActiveTakeoverPresetId);
            }
        }

        public void ActivateTakeoverPreset(string id)
        {
            EnsureTakeoverPromptParts();
            var preset = TakeoverPresets.Find(item => item.Id == id);
            if (preset == null) return;
            ActiveTakeoverPresetId = preset.Id;
            TakeoverPromptParts = preset.Parts;
        }

        public void AddTakeoverPreset(ArtiPromptPreset preset)
        {
            EnsureTakeoverPromptParts();
            preset.Normalize();
            string name = preset.Name;
            int suffix = 2;
            while (TakeoverPresets.Exists(item => item.Name == preset.Name))
                preset.Name = name + " (" + suffix++ + ")";
            TakeoverPresets.Add(preset);
        }

        public string GetPrimaryTakeoverSystemDocument()
        {
            EnsureTakeoverPromptParts();
            foreach (ArtiPromptPart part in TakeoverPromptParts)
            {
                if (part.Role == PromptRole.System)
                {
                    return part.Content ?? string.Empty;
                }
            }

            return string.Empty;
        }

        public void SetPrimaryTakeoverSystemDocument(string document)
        {
            string normalized = document ?? DefaultTakeoverArtiPromptDocument;
            EnsureTakeoverPromptParts();
            foreach (ArtiPromptPart part in TakeoverPromptParts)
            {
                if (part.Role == PromptRole.System)
                {
                    part.Content = normalized;
                    return;
                }
            }

            TakeoverPromptParts.Insert(0, new ArtiPromptPart("System Document", PromptRole.System, normalized));
        }
    }
}
