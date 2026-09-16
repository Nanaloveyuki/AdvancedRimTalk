using System;
using System.Collections.Generic;
using System.Globalization;
using Verse;

namespace AdvancedRimTalk.Diagnostics
{
    internal static class ArtiLogBuffer
    {
        private static readonly object Sync = new object();
        private static readonly List<Entry> Entries = new List<Entry>();
        public static void Add(string level, string message)
        {
            string line = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)
                + " | " + level + " | " + (message ?? string.Empty);
            lock (Sync) { Entries.Add(new Entry(line, level)); if (Entries.Count > 500) Entries.RemoveAt(0); }
            if (level == "ERROR") Log.Error("[Advanced RimTalk] " + line);
            else if (level == "WARN") Log.Warning("[Advanced RimTalk] " + line);
            else Log.Message("[Advanced RimTalk] " + line);
        }
        public static Entry[] Snapshot() { lock (Sync) { return Entries.ToArray(); } }
        public static void Clear() { lock (Sync) Entries.Clear(); }
        internal sealed class Entry { public readonly string Line; public readonly string Level; public Entry(string line, string level) { Line = line; Level = level; } }
    }
}
