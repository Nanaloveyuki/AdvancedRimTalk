using System;
using AdvancedRimTalk.Prompt;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class TakeoverPromptImportConverterChecks
    {
        public static void Run()
        {
            ConvertsKnownRimTalkScribanTokens();
            ConvertsMoodEffectsCondition();
        }

        private static void ConvertsKnownRimTalkScribanTokens()
        {
            string converted = TakeoverPromptImportConverter.ConvertKnownRimTalkScriban(
                "{{context}}\n{{ chat.history_simplified }}\n{{chat.history}}\n{{prompt}}\n{{json.format}}\n{{lang}}");

            AssertContains(converted, "{{% core.emit(ctx.pawn_context) %}}", "context token");
            AssertContains(converted, "{{% core.emit(chat.history_simplified) %}}", "simplified history token");
            AssertContains(converted, "{{% core.emit(chat.history) %}}", "history token");
            AssertContains(converted, "{{% core.emit(ctx.dialogue_prompt) %}}", "prompt token");
            AssertContains(converted, "{{% core.emit(json.format) %}}", "json token");
            AssertContains(converted, "{{% core.emit(lang) %}}", "lang token");
        }

        private static void ConvertsMoodEffectsCondition()
        {
            string converted = TakeoverPromptImportConverter.ConvertKnownRimTalkScriban(
                "A\n{{ if settings.ApplyMoodAndSocialEffects }}\nOptional \"act\"\n{{ end }}\nB");

            AssertContains(converted, "if settings.apply_mood_and_social_effects", "condition");
            AssertContains(converted, "core.emit(\"\\nOptional \\\"act\\\"\\n\")", "escaped body");
            Assert(!converted.Contains("{{ if settings.ApplyMoodAndSocialEffects }}"), "native if removed");
            Assert(!converted.Contains("{{ end }}"), "native end removed");
        }

        private static void AssertContains(string value, string expected, string name)
        {
            if (!value.Contains(expected))
            {
                throw new InvalidOperationException("Failed: " + name + ". Missing '" + expected + "' in '" + value + "'.");
            }
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Failed: " + name);
            }
        }
    }
}
