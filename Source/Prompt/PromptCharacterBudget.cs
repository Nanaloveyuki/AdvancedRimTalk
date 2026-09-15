using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedRimTalk.Prompt
{
    internal static class PromptCharacterBudget
    {
        internal static string[] Apply(IList<string> texts, int budget)
        {
            if (budget < 0) throw new ArgumentOutOfRangeException(nameof(budget));
            string[] result = new string[texts.Count];
            int[] order = Enumerable.Range(0, texts.Count)
                .OrderBy(index => (texts[index] ?? string.Empty).Length).ToArray();
            for (int i = 0; i < order.Length; i++)
            {
                int index = order[i];
                string text = texts[index] ?? string.Empty;
                int length = Math.Min(text.Length, budget / (order.Length - i));
                if (length > 0 && length < text.Length
                    && char.IsHighSurrogate(text[length - 1]) && char.IsLowSurrogate(text[length])) length--;
                result[index] = text.Substring(0, length);
                budget -= length;
            }
            return result;
        }
    }
}
