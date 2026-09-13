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

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnablePlaceholderLayer, "enablePlaceholderLayer", true);
            Scribe_Values.Look(ref ReplaceRimTalkPromptMechanism, "replaceRimTalkPromptMechanism", false);
            Scribe_Values.Look(
                ref TakeoverArtiPromptDocument,
                "takeoverArtiPromptDocument",
                DefaultTakeoverArtiPromptDocument);
            if (TakeoverArtiPromptDocument == null)
            {
                TakeoverArtiPromptDocument = DefaultTakeoverArtiPromptDocument;
            }
        }
    }
}
