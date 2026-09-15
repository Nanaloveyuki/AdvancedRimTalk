using System;
using System.Linq;
using AdvancedRimTalk.Prompt;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class PromptCharacterBudgetChecks
    {
        internal static void Run()
        {
            CheckBoundedDomain();
            string[] input = { new string('a', 1000), "request" };
            string[] bounded = PromptCharacterBudget.Apply(input, 40);
            if (bounded[1] != "request" || bounded.Sum(text => text.Length) != 40 || input[0].Length != 1000)
                throw new Exception("Budget must preserve short request parts without mutating input.");
            for (int budget = 0; budget < 100; budget++)
            {
                bounded = PromptCharacterBudget.Apply(new[] { "a\ud83d\ude00b", "tail", new string('x', 100) }, budget);
                if (bounded.Sum(text => text.Length) > budget
                    || bounded.Any(text => text.Length > 0 && char.IsHighSurrogate(text[text.Length - 1])))
                    throw new Exception("Budget exceeded or surrogate pair split.");
            }
            if (PromptCharacterBudget.Apply(new string[0], 0).Length != 0)
                throw new Exception("Empty input should stay empty.");
            bounded = PromptCharacterBudget.Apply(new[] { "one", "two" }, 100);
            if (bounded[0] != "one" || bounded[1] != "two")
                throw new Exception("Content within budget changed.");
        }

        private static void CheckBoundedDomain()
        {
            string[] alphabet = { null, string.Empty, "a", "abc", "\ud83d\ude00", "a\ud83d\ude00b" };
            int cases = 0;
            // Exhaustive, ordered enumeration gives a stable smallest failing case in this finite domain.
            for (int count = 0, combinations = 1; count <= 3; count++, combinations *= alphabet.Length)
            {
                for (int code = 0; code < combinations; code++)
                {
                    string[] input = new string[count];
                    for (int i = 0, digits = code; i < count; i++, digits /= alphabet.Length)
                        input[i] = alphabet[digits % alphabet.Length];
                    string[] original = (string[])input.Clone();
                    for (int budget = 0; budget <= 16; budget++)
                    {
                        string[] result = PromptCharacterBudget.Apply(input, budget);
                        string failure = null;
                        if (result.Length != count) failure = "part count";
                        else if (!input.SequenceEqual(original)) failure = "input mutation";
                        else if (result.Sum(text => text.Length) > budget) failure = "budget exceeded";
                        else
                        {
                            for (int i = 0; i < count; i++)
                            {
                                string text = input[i] ?? string.Empty;
                                if (!text.StartsWith(result[i], StringComparison.Ordinal)) failure = "not a source prefix";
                                if (result[i].Length > 0 && char.IsHighSurrogate(result[i][result[i].Length - 1]))
                                    failure = "split surrogate";
                            }
                            int available = input.Sum(text => text?.Length ?? 0);
                            if (available <= budget && !result.SequenceEqual(input.Select(text => text ?? string.Empty)))
                                failure = "unnecessary truncation";
                            if (input.All(text => text == null || text.All(character => character < 128))
                                && result.Sum(text => text.Length) != Math.Min(available, budget))
                                failure = "unused ASCII capacity";
                        }
                        if (failure != null)
                            throw new Exception("Budget property failed: " + failure + "; count=" + count
                                + "; case=" + code + "; budget=" + budget
                                + "; input=" + System.Text.Json.JsonSerializer.Serialize(input));
                        cases++;
                    }
                }
            }
            Console.WriteLine("Budget properties passed: " + cases + " exhaustive cases.");
        }
    }
}
