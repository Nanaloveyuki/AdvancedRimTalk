using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AdvancedRimTalk.Settings;

namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkResponseProcessor
    {
        private static DateTime lastAccepted = DateTime.MinValue;
        internal static string Process(string text, string model, AdvancedRimTalkSettings s)
        {
            if (s == null || !s.EnableResponseProcessing || string.IsNullOrWhiteSpace(text)) return text;
            if (s.IgnoreByModel && !string.IsNullOrWhiteSpace(s.ResponseModelIds) && !AnyMatch(model, s.ResponseModelIds)) return text;
            if (s.IgnoreByInterval && s.IgnoreIntervalSeconds > 0 && (DateTime.UtcNow - lastAccepted).TotalSeconds < s.IgnoreIntervalSeconds) return text;
            if (s.IgnoreByRegex && MatchesPolicy(text, s)) return text;
            string result = text;
            if (s.EnableJsonFormatting) result = TryFormat(result) ?? text;
            lastAccepted = DateTime.UtcNow;
            return result;
        }
        private static bool MatchesPolicy(string text, AdvancedRimTalkSettings s)
        {
            bool white = s.RegexWhitelist && AnyMatch(text, s.ResponseWhitelistRegex);
            bool black = s.RegexBlacklist && AnyMatch(text, s.ResponseBlacklistRegex);
            if (s.RegexWhitelist && !white) return true;
            return black;
        }
        private static bool AnyMatch(string text, string source)
        {
            foreach (string line in (source ?? string.Empty).Split(new[] {'\r','\n'}, StringSplitOptions.RemoveEmptyEntries))
                try { if (Regex.IsMatch(text, line, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50))) return true; } catch { }
            return false;
        }
        private static string TryFormat(string input)
        {
            string t = input.Trim();
            if (t.Length == 0 || (!t.Contains("\"") && !t.Contains("{"))) return null;
            int start = t.IndexOf('{'); int end = t.LastIndexOf('}');
            if (start < 0) return null;
            if (end < start) t += "}";
            else if (end != t.Length - 1) t = t.Substring(start, end - start + 1);
            t = Regex.Replace(t, "([{,])\\s*([A-Za-z_][A-Za-z0-9_]*)\\s*:", "$1\"$2\":");
            t = t.Replace("\u201c", "\"").Replace("\u201d", "\"");
            int balance = 0; foreach (char c in t) { if (c == '{') balance++; else if (c == '}') balance--; }
            while (balance-- > 0) t += "}";
            return t.StartsWith("{") && t.Contains("\"") ? t : null;
        }
    }
}
