using System;

namespace AdvancedRimTalk.Prompt
{
    public sealed class PromptSnapshot
    {
        public int Tick { get; set; } = -1;
        public string PawnName { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string TalkType { get; set; } = string.Empty;
        public string DialogueType { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string RawPrompt { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string PawnContext { get; set; } = string.Empty;
        public string Hour { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;
        public string Weather { get; set; } = string.Empty;
        public string Temperature { get; set; } = string.Empty;
        public string Wealth { get; set; } = string.Empty;
        public bool IsAnnouncement { get; set; }
        public bool IsMonologue { get; set; }
        public string State { get; set; } = string.Empty;

        public bool TryGetValue(string name, out object value)
        {
            string key = (name ?? string.Empty).Trim().ToLowerInvariant();
            switch (key)
            {
                case "tick":
                    value = Tick;
                    return true;
                case "pawn_name":
                case "pawn.name":
                    value = PawnName;
                    return true;
                case "recipient_name":
                case "recipient.name":
                    value = RecipientName;
                    return true;
                case "talk_type":
                    value = TalkType;
                    return true;
                case "dialogue_type":
                    value = DialogueType;
                    return true;
                case "intent":
                    value = Intent;
                    return true;
                case "topic":
                case "conversation_topic":
                    value = Topic;
                    return true;
                case "status":
                    value = Status;
                    return true;
                case "prompt":
                    value = Prompt;
                    return true;
                case "raw_prompt":
                    value = RawPrompt;
                    return true;
                case "context":
                    value = Context;
                    return true;
                case "pawn_context":
                    value = PawnContext;
                    return true;
                case "hour":
                    value = Hour;
                    return true;
                case "date":
                    value = Date;
                    return true;
                case "season":
                    value = Season;
                    return true;
                case "weather":
                    value = Weather;
                    return true;
                case "temperature":
                    value = Temperature;
                    return true;
                case "wealth":
                    value = Wealth;
                    return true;
                case "is_announcement":
                    value = IsAnnouncement;
                    return true;
                case "is_monologue":
                    value = IsMonologue;
                    return true;
                case "state":
                    value = State;
                    return true;
                default:
                    value = null;
                    return false;
            }
        }
    }
}
