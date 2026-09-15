using System;
using System.Collections.Generic;
using RimTalk.Data;
using RimTalk.Prompt;
using Verse;

namespace AdvancedRimTalk.Prompt
{
    public sealed class ArtiPromptPart : IExposable
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Prompt Part";
        public bool Enabled = true;
        public PromptRole Role = PromptRole.System;
        public string CustomRole = string.Empty;
        public string Content = string.Empty;

        public ArtiPromptPart()
        {
        }

        public ArtiPromptPart(string name, PromptRole role, string content, bool enabled = true)
        {
            Id = Guid.NewGuid().ToString("N");
            Name = name ?? "Prompt Part";
            Role = role;
            Content = content ?? string.Empty;
            Enabled = enabled;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id", Guid.NewGuid().ToString("N"));
            Scribe_Values.Look(ref Name, "name", "Prompt Part");
            Scribe_Values.Look(ref Enabled, "enabled", true);
            Scribe_Values.Look(ref Role, "role", PromptRole.System);
            Scribe_Values.Look(ref CustomRole, "customRole", string.Empty);
            Scribe_Values.Look(ref Content, "content", string.Empty);

            Normalize();
        }

        public void Normalize()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = "Prompt Part";
            }

            if (CustomRole == null)
            {
                CustomRole = string.Empty;
            }

            if (Content == null)
            {
                Content = string.Empty;
            }
        }

        public RimTalk.Data.Role ToMessageRole()
        {
            if (!string.IsNullOrWhiteSpace(CustomRole))
            {
                return RimTalk.Data.Role.User;
            }

            return Role == PromptRole.Assistant ? RimTalk.Data.Role.AI : (RimTalk.Data.Role)Role;
        }

        public string ApplyCustomRolePrefix(string content)
        {
            if (string.IsNullOrWhiteSpace(CustomRole))
            {
                return content ?? string.Empty;
            }

            return "[role: " + CustomRole.Trim() + "]\n" + (content ?? string.Empty);
        }

        public static ArtiPromptPart FromRimTalkEntry(PromptEntry entry, string name)
        {
            ArtiPromptPart part = new ArtiPromptPart(
                name,
                entry == null ? PromptRole.System : entry.Role,
                entry == null
                    ? string.Empty
                    : entry.Content,
                entry == null || entry.Enabled);
            if (entry != null)
            {
                part.CustomRole = entry.CustomRole ?? string.Empty;
            }

            return part;
        }

        public static List<ArtiPromptPart> CreateDefaultParts(string legacySystemDocument)
        {
            string document = legacySystemDocument ?? AdvancedRimTalk.Settings.AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument;
            if (!string.Equals(
                    document.Trim(),
                    AdvancedRimTalk.Settings.AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument.Trim(),
                    StringComparison.Ordinal))
            {
                return new List<ArtiPromptPart>
                {
                    new ArtiPromptPart("System Document", PromptRole.System, document),
                    new ArtiPromptPart("Dialogue Prompt", PromptRole.User, DefaultDialoguePromptDocument)
                };
            }

            return new List<ArtiPromptPart>
            {
                new ArtiPromptPart("Base Instruction", PromptRole.System, DefaultBaseInstructionDocument),
                new ArtiPromptPart("JSON Format", PromptRole.System, DefaultJsonFormatDocument),
                new ArtiPromptPart("Context", PromptRole.System, DefaultContextDocument),
                new ArtiPromptPart("Dialogue State", PromptRole.System, DefaultDialogueStateDocument),
                new ArtiPromptPart("Chat History", PromptRole.User, DefaultChatHistoryDocument),
                new ArtiPromptPart("Dialogue Prompt", PromptRole.User, DefaultDialoguePromptDocument)
            };
        }

        public const string DefaultBaseInstructionDocument =
            "You are writing in-character RimWorld colonist dialogue.\n"
            + "Use participant names exactly as provided. Do not include Markdown, code fences, or commentary.";

        public const string DefaultJsonFormatDocument =
            "Return JSONL only. Each line must be a JSON object with keys \"name\" and \"text\".";

        public const string DefaultContextDocument =
            "Current RimWorld context:\n"
            + "{{%\n"
            + "core.emit(ctx.pawn_context)\n"
            + "%}}";

        public const string DefaultDialogueStateDocument =
            "{{%\n"
            + "core.emit(\"Dialogue type: \")\n"
            + "core.emit(ctx.dialogue_type)\n"
            + "core.emit(\"\\nIntent: \")\n"
            + "core.emit(ctx.intent)\n"
            + "core.emit(\"\\nTopic: \")\n"
            + "core.emit(ctx.conversation_topic)\n"
            + "core.emit(\"\\nStatus: \")\n"
            + "core.emit(ctx.dialogue_status)\n"
            + "%}}";

        public const string DefaultChatHistoryDocument =
            "{{%\n"
            + "core.emit(chat.history)\n"
            + "%}}";

        public const string DefaultDialoguePromptDocument =
            "{{%\n"
            + "let prompt = ctx.dialogue_prompt.trim()\n"
            + "if !prompt.is_empty() {\n"
            + "    core.emit(prompt)\n"
            + "} else if !ctx.conversation_topic.trim().is_empty() {\n"
            + "    core.emit(ctx.conversation_topic)\n"
            + "} else if !ctx.intent.trim().is_empty() {\n"
            + "    core.emit(ctx.intent)\n"
            + "} else {\n"
            + "    core.emit(\"Generate the next dialogue for the current RimWorld situation.\")\n"
            + "}\n"
            + "%}}";
    }
}
