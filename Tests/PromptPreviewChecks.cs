using System;
using System.Collections.Generic;
using System.Text.Json;
using AdvancedRimTalk.Prompt;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class PromptPreviewChecks
    {
        public static void Run()
        {
            CheckRoundTripDomain();
            PromptPreview assistant = PromptPreview.FromMessages(new List<(string, string)> { ("AI", "reply") });
            using (JsonDocument json = JsonDocument.Parse(assistant.RawResponseJson))
            {
                if (json.RootElement[0].GetProperty("role").GetString() != "assistant"
                    || !assistant.PlainText.StartsWith("[assistant]", StringComparison.Ordinal))
                    throw new Exception("RimTalk AI role must map to assistant in both preview formats.");
            }
            string content = "quote: \" slash: \\ tab:\t nul:\0 newline:\r\n unicode:\u4e2d\ud83d\ude00";
            PromptPreview preview = PromptPreview.FromMessages(new List<(string, string)>
            {
                ("System", content), ("User", null)
            });
            using (JsonDocument json = JsonDocument.Parse(preview.RawResponseJson))
            {
                if (json.RootElement.GetArrayLength() != 2
                    || json.RootElement[0].GetProperty("role").GetString() != "system"
                    || json.RootElement[0].GetProperty("content").GetString() != content
                    || json.RootElement[1].GetProperty("content").GetString() != string.Empty)
                    throw new Exception("Prompt preview JSON did not round-trip.");
            }
            if (!preview.PlainText.Contains(content))
                throw new Exception("Plain preview changed the message text.");
            PromptPreview empty = PromptPreview.FromMessages<string>(null);
            if (empty.RawResponseJson != "[]" || empty.PlainText != string.Empty)
                throw new Exception("Empty preview must contain no messages.");
        }

        private static void CheckRoundTripDomain()
        {
            string[] atoms = { "a", "\"", "\\", "\t", "\0", "\n", "\u4e2d", "\ud83d\ude00" };
            string[] roles = { "System", "User", "AI" };
            string[] expectedRoles = { "system", "user", "assistant" };
            int cases = 0;
            for (int size = 0, combinations = 1; size <= 3; size++, combinations *= atoms.Length)
            {
                for (int code = 0; code < combinations; code++)
                {
                    string content = string.Empty;
                    for (int i = 0, digits = code; i < size; i++, digits /= atoms.Length)
                        content += atoms[digits % atoms.Length];
                    var messages = new List<(string, string)>();
                    foreach (string role in roles) messages.Add((role, content));
                    PromptPreview result = PromptPreview.FromMessages(messages);
                    using (JsonDocument parsed = JsonDocument.Parse(result.RawResponseJson))
                    {
                        if (parsed.RootElement.GetArrayLength() != roles.Length)
                            throw new Exception("Preview property: message count changed.");
                        for (int i = 0; i < roles.Length; i++)
                        {
                            if (parsed.RootElement[i].GetProperty("content").GetString() != content
                                || parsed.RootElement[i].GetProperty("role").GetString() != expectedRoles[i])
                                throw new Exception("Preview round-trip failed: size=" + size + "; case=" + code
                                    + "; role=" + roles[i] + "; input=" + JsonSerializer.Serialize(content));
                        }
                    }
                    cases++;
                }
            }
            Console.WriteLine("Preview properties passed: " + cases + " exhaustive message arrays.");
        }
    }
}
