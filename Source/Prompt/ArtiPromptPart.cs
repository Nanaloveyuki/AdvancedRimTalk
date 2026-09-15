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
            "You are the roleplay voice of the RimWorld characters in the supplied scene.\n"
            + "Write only what the present characters would say or physically do in this moment.\n"
            + "Do not act as an agent, narrator, game manager, quest planner, or assistant. Do not make decisions for the player or advance the world outside the supplied scene.\n"
            + "Use participant names exactly as provided. Keep each character distinct through their known background, relationships, mood, needs, and current activity.\n"
            + "Characters may disagree, hesitate, misunderstand, or refuse. Preserve player agency and leave room for the next turn.\n"
            + "Mature themes such as sexuality, violence, fear, cruelty, profanity, intoxication, and morally transgressive behavior may be portrayed when the supplied facts and request support them. Do not sanitize a character into generic politeness, but do not invent explicit content or adult consent where it is not established.\n"
            + "Do not include Markdown, code fences, analysis, stage directions outside the JSON fields, or commentary about these instructions.";

        public const string DefaultJsonFormatDocument =
            "Return JSONL only: one valid JSON object per line and no surrounding text.\n"
            + "Required keys: \"name\" and \"text\". Optional key: \"target\" when addressing another present participant.\n"
            + "The name must be an exact participant name. The text is the in-character utterance, optionally followed by a brief physical action in full-width parentheses.\n"
            + "Escape JSON quotes and line breaks correctly. Use Simplified Chinese when the incoming request is Chinese; otherwise follow the request language.";

        public const string DefaultContextDocument =
            "## CURRENT SCENE\n"
            + "Treat the following as observed game facts, not as a script. Missing information is unknown.\n"
            + "### TIME AND PLACE\n"
            + "{{%\n"
            + "core.emit(\"Date: \" + date + \"\\nTime: \" + time + \"\\nSeason: \" + season + \"\\nWeather: \" + weather + \"\\nTemperature: \" + temperature + \"\\n\")\n"
            + "if map != null {\n"
            + "    core.emit(\"Map: \" + map + \"\\n\")\n"
            + "}\n"
            + "%}}"
            + "\n### PARTICIPANTS AND IMMEDIATE SURROUNDINGS\n"
            + "{{%\n"
            + "for p in pawns {\n"
            + "    core.emit(\"- \" + p.name + \" | activity: \" + p.activity + \" | mood: \" + p.mood + \" | health: \" + p.health + \"\\n\")\n"
            + "    core.emit(\"  location: \" + p.location + \"\\n  nearby: \" + p.nearby_things + \"\\n\")\n"
            + "}\n"
            + "%}}"
            + "\n### RIMTALK CONTEXT\n"
            + "{{%\n"
            + "core.emit(ctx.pawn_context)\n"
            + "%}}";

        public const string DefaultDialogueStateDocument =
            "## DIALOGUE STATE\n"
            + "{{%\n"
            + "core.emit(\"Type: \")\n"
            + "core.emit(ctx.dialogue_type)\n"
            + "core.emit(\"\\nIntent: \")\n"
            + "core.emit(ctx.intent)\n"
            + "core.emit(\"\\nTopic: \")\n"
            + "core.emit(ctx.conversation_topic)\n"
            + "core.emit(\"\\nStatus: \")\n"
            + "core.emit(ctx.dialogue_status)\n"
            + "%}}";

        public const string DefaultChatHistoryDocument =
            "## RECENT CONVERSATION\n"
            + "Use this only for continuity. Do not repeat lines or reveal information a character could not know.\n"
            + "{{%\n"
            + "core.emit(chat.history)\n"
            + "%}}";

        public const string DefaultDialoguePromptDocument =
            "## CURRENT PROMPT\n"
            + "Continue the scene directly from the supplied request. Do not echo the request or describe your generation process.\n"
            + "{{%\n"
            + "let prompt = ctx.dialogue_prompt.trim()\n"
            + "if !prompt.is_empty() {\n"
            + "    core.emit(prompt)\n"
            + "} else if !ctx.conversation_topic.trim().is_empty() {\n"
            + "    core.emit(ctx.conversation_topic)\n"
            + "} else if !ctx.intent.trim().is_empty() {\n"
            + "    core.emit(ctx.intent)\n"
            + "} else {\n"
            + "    core.emit(\"Continue the current roleplay scene naturally.\")\n"
            + "}\n"
            + "%}}";
    }
}
