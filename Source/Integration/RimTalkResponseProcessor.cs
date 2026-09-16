using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AdvancedRimTalk.Settings;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkResponseProcessor
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Regex[]> Rules = new Dictionary<string, Regex[]>();
        private static readonly HashSet<string> WarnedRules = new HashSet<string>();
        private static long? lastAccepted;
        internal static string Process(string text, string model, AdvancedRimTalkSettings s)
        {
            if (s == null || !s.EnableResponseProcessing || string.IsNullOrWhiteSpace(text)) return text;
            lock (Gate)
            {
                if (s.IgnoreByModel && !string.IsNullOrWhiteSpace(s.ResponseModelIds) && !AnyMatch(model, s.ResponseModelIds)) return text;
                if (s.IgnoreByInterval && s.IgnoreIntervalSeconds > 0 && lastAccepted.HasValue
                    && (Stopwatch.GetTimestamp() - lastAccepted.Value) / (double)Stopwatch.Frequency < s.IgnoreIntervalSeconds) return text;
                if (s.IgnoreByRegex && MatchesPolicy(text, s)) return text;
                string result = s.EnableJsonFormatting ? ResponseJsonRepair.TryRepair(text) ?? text : text;
                lastAccepted = Stopwatch.GetTimestamp();
                return result;
            }
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
            source = source ?? string.Empty;
            if (!Rules.TryGetValue(source, out Regex[] rules))
            {
                // Bound cache growth while users edit patterns; reuse the three current policy sets.
                if (Rules.Count >= 3)
                {
                    Rules.Clear();
                    WarnedRules.Clear();
                }
                var parsed = new List<Regex>();
                foreach (string line in source.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try { parsed.Add(new Regex(line, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50))); }
                    catch (ArgumentException)
                    {
                        if (WarnedRules.Add(source)) Log.Warning("Advanced RimTalk: invalid response regex skipped.");
                    }
                }
                rules = parsed.ToArray();
                Rules[source] = rules;
            }
            var elapsed = Stopwatch.StartNew();
            foreach (Regex rule in rules)
            {
                try { if (rule.IsMatch(text ?? string.Empty)) return true; }
                catch (RegexMatchTimeoutException)
                {
                    if (WarnedRules.Add(source)) Log.Warning("Advanced RimTalk: response regex timed out.");
                }
                if (elapsed.ElapsedMilliseconds >= 50)
                {
                    if (WarnedRules.Add(source)) Log.Warning("Advanced RimTalk: response regex time budget reached; remaining rules skipped.");
                    break;
                }
            }
            return false;
        }
    }
}
