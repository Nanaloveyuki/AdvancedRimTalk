using System.Collections.Generic;
using AdvancedRimTalk.Prompt;
using RimTalk.Prompt;
using Verse;

namespace AdvancedRimTalk.Settings
{
    public sealed class AdvancedRimTalkSettings : ModSettings
    {
        public const string DefaultTakeoverArtiPromptDocument =
            "You are writing in-character RimWorld colonist dialogue.\n"
            + "Return JSONL only. Each line must be a JSON object with keys \"name\" and \"text\".\n"
            + "Use participant names exactly as provided. Do not include Markdown, code fences, or commentary.\n\n"
            + "Current RimWorld context:\n"
            + "{{%\n"
            + "core.emit(ctx.pawn_context)\n"
            + "core.emit(\"\\n\\nDialogue type: \")\n"
            + "core.emit(ctx.dialogue_type)\n"
            + "core.emit(\"\\nIntent: \")\n"
            + "core.emit(ctx.intent)\n"
            + "core.emit(\"\\nTopic: \")\n"
            + "core.emit(ctx.conversation_topic)\n"
            + "core.emit(\"\\nStatus: \")\n"
            + "core.emit(ctx.dialogue_status)\n"
            + "core.emit(\"\\n\\n\")\n"
            + "core.emit(json.format)\n"
            + "%}}";

        public bool EnablePlaceholderLayer = true;
        public bool ReplaceRimTalkPromptMechanism = false;
        public string TakeoverArtiPromptDocument = DefaultTakeoverArtiPromptDocument;
        public List<ArtiPromptPart> TakeoverPromptParts = new List<ArtiPromptPart>();
        public int ArtiEditorUndoLimit = 100;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnablePlaceholderLayer, "enablePlaceholderLayer", true);
            Scribe_Values.Look(ref ReplaceRimTalkPromptMechanism, "replaceRimTalkPromptMechanism", false);
            Scribe_Values.Look(
                ref TakeoverArtiPromptDocument,
                "takeoverArtiPromptDocument",
                DefaultTakeoverArtiPromptDocument);
            Scribe_Collections.Look(ref TakeoverPromptParts, "takeoverPromptParts", LookMode.Deep);
            Scribe_Values.Look(ref ArtiEditorUndoLimit, "artiEditorUndoLimit", 100);
            if (TakeoverArtiPromptDocument == null)
            {
                TakeoverArtiPromptDocument = DefaultTakeoverArtiPromptDocument;
            }

            EnsureTakeoverPromptParts();

            if (ArtiEditorUndoLimit < 0)
            {
                ArtiEditorUndoLimit = 0;
            }
            else if (ArtiEditorUndoLimit > 500)
            {
                ArtiEditorUndoLimit = 500;
            }
        }

        public void EnsureTakeoverPromptParts()
        {
            if (TakeoverPromptParts == null || TakeoverPromptParts.Count == 0)
            {
                TakeoverPromptParts = ArtiPromptPart.CreateDefaultParts(TakeoverArtiPromptDocument);
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
                TakeoverPromptParts = ArtiPromptPart.CreateDefaultParts(TakeoverArtiPromptDocument);
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

            return TakeoverArtiPromptDocument ?? DefaultTakeoverArtiPromptDocument;
        }

        public void SetPrimaryTakeoverSystemDocument(string document)
        {
            string normalized = document ?? DefaultTakeoverArtiPromptDocument;
            TakeoverArtiPromptDocument = normalized;
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
