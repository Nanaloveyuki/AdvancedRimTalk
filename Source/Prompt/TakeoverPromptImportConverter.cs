using System;
using System.Text.RegularExpressions;

namespace AdvancedRimTalk.Prompt
{
    internal static class TakeoverPromptImportConverter
    {
        private static readonly Regex MoodEffectsBlockRegex = new Regex(
            @"\{\{\s*if\s+settings\.ApplyMoodAndSocialEffects\s*\}\}(.*?)\{\{\s*end\s*\}\}",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public static string ConvertKnownRimTalkScriban(string content)
        {
            string result = content ?? string.Empty;
            result = MoodEffectsBlockRegex.Replace(
                result,
                match => ConvertConditionalEmit(
                    "settings.apply_mood_and_social_effects",
                    match.Groups[1].Value));
            result = ReplaceSimpleToken(result, "chat.history_simplified", "{{% core.emit(chat.history_simplified) %}}");
            result = ReplaceSimpleToken(result, "chat.history", "{{% core.emit(chat.history) %}}");
            result = ReplaceSimpleToken(result, "json.format", "{{% core.emit(json.format) %}}");
            result = ReplaceSimpleToken(result, "context", "{{% core.emit(ctx.pawn_context) %}}");
            result = ReplaceSimpleToken(result, "pawn_context", "{{% core.emit(ctx.pawn_context) %}}");
            result = ReplaceSimpleToken(result, "prompt", "{{% core.emit(ctx.dialogue_prompt) %}}");
            result = ReplaceSimpleToken(result, "lang", "{{% core.emit(lang) %}}");
            return result;
        }

        private static string ReplaceSimpleToken(string input, string token, string replacement)
        {
            return Regex.Replace(
                input,
                @"\{\{\s*" + Regex.Escape(token) + @"\s*\}\}",
                replacement,
                RegexOptions.IgnoreCase);
        }

        private static string ConvertConditionalEmit(string condition, string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return string.Empty;
            }

            return "{{%\n"
                + "if "
                + condition
                + " {\n"
                + "    core.emit("
                + ToArtiString(body)
                + ")\n"
                + "}\n"
                + "%}}";
        }

        private static string ToArtiString(string value)
        {
            value = value ?? string.Empty;
            return "\""
                + value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                + "\"";
        }
    }
}
