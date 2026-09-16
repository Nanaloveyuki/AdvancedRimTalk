using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace AdvancedRimTalk.Integration
{
    internal static class ResponseJsonRepair
    {
        internal static string TryRepair(string input)
        {
            var documents = new List<string>();
            int index = 0;
            while (index < input.Length)
            {
                int start = input.IndexOfAny(new[] { '{', '[' }, index);
                if (start < 0) break;
                index = start;
                string repaired = ReadDocument(input, ref index);
                if (repaired == null || !IsJson(repaired)) return null;
                documents.Add(repaired);
            }
            return documents.Count == 0 ? null : string.Join("\n", documents);
        }

        // Repair tokens outside strings only; the platform JSON reader validates the result.
        private static string ReadDocument(string input, ref int index)
        {
            var output = new StringBuilder();
            var closers = new Stack<char>();
            for (; index < input.Length; index++)
            {
                char c = input[index];
                if (c == '"' || c == '\'' || c == '\u201c')
                {
                    char end = c == '\u201c' ? '\u201d' : c;
                    bool closed = false;
                    output.Append('"');
                    while (++index < input.Length)
                    {
                        c = input[index];
                        if (c == end) { closed = true; break; }
                        if (c == '\\')
                        {
                            if (++index == input.Length) return null;
                            char escaped = input[index];
                            if (end == '\'' && escaped == '\'') output.Append('\'');
                            else output.Append('\\').Append(escaped);
                        }
                        else if (c == '"') output.Append("\\\"");
                        else if (c < 32) output.Append("\\u").Append(((int)c).ToString("x4"));
                        else output.Append(c);
                    }
                    if (!closed) return null;
                    output.Append('"');
                }
                else if (c == '{' || c == '[')
                {
                    closers.Push(c == '{' ? '}' : ']');
                    output.Append(c);
                }
                else if (c == '}' || c == ']')
                {
                    if (closers.Count == 0 || closers.Pop() != c) return null;
                    RemoveTrailingComma(output);
                    output.Append(c);
                    if (closers.Count == 0) { index++; return output.ToString(); }
                }
                else if ((char.IsLetter(c) || c == '_') && Previous(output) is '{' or ',')
                {
                    int end = index + 1;
                    while (end < input.Length && (char.IsLetterOrDigit(input[end]) || input[end] == '_')) end++;
                    int next = end;
                    while (next < input.Length && char.IsWhiteSpace(input[next])) next++;
                    if (next < input.Length && input[next] == ':')
                    {
                        output.Append('"').Append(input, index, end - index).Append('"');
                        index = end - 1;
                    }
                    else output.Append(c);
                }
                else output.Append(c);
            }
            // Missing closing containers are repairable; unterminated strings and values are not.
            while (closers.Count > 0)
            {
                RemoveTrailingComma(output);
                output.Append(closers.Pop());
            }
            return output.ToString();
        }

        private static char Previous(StringBuilder text)
        {
            for (int i = text.Length - 1; i >= 0; i--)
                if (!char.IsWhiteSpace(text[i])) return text[i];
            return '\0';
        }

        private static void RemoveTrailingComma(StringBuilder text)
        {
            int end = text.Length - 1;
            while (end >= 0 && char.IsWhiteSpace(text[end])) end--;
            if (end >= 0 && text[end] == ',') text.Remove(end, 1);
        }

        private static bool IsJson(string text)
        {
            try
            {
                using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(
                    Encoding.UTF8.GetBytes(text), XmlDictionaryReaderQuotas.Max))
                    while (reader.Read()) { }
                return true;
            }
            catch (XmlException) { return false; }
            catch (ArgumentException) { return false; }
        }
    }
}
