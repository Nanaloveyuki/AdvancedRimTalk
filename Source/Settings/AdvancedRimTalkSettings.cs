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
        public int ArtiEditorUndoLimit = 100;
        public int ArtiEditorCompletionLimit = 5;
        public int TakeoverMaxPawnContextCount = 32;
        public int TakeoverConversationHistoryCount = 40;
        public int TakeoverPromptCharacterBudget = 24000;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnablePlaceholderLayer, "enablePlaceholderLayer", true);
            Scribe_Values.Look(ref ReplaceRimTalkPromptMechanism, "replaceRimTalkPromptMechanism", false);
            Scribe_Collections.Look(ref TakeoverPromptParts, "takeoverPromptParts", LookMode.Deep);
            Scribe_Values.Look(ref ArtiEditorUndoLimit, "artiEditorUndoLimit", 100);
            Scribe_Values.Look(ref ArtiEditorCompletionLimit, "artiEditorCompletionLimit", 5);
            Scribe_Values.Look(ref TakeoverMaxPawnContextCount, "takeoverMaxPawnContextCount", 32);
            Scribe_Values.Look(ref TakeoverConversationHistoryCount, "takeoverConversationHistoryCount", 40);
            Scribe_Values.Look(ref TakeoverPromptCharacterBudget, "takeoverPromptCharacterBudget", 24000);
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
