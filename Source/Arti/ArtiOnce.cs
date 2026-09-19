using System;
using System.Collections.Generic;
using Verse;

namespace AdvancedRimTalk.Arti
{
    public sealed class ArtiOnceRecord : IExposable
    {
        public string Id = string.Empty;
        public string ScopeKey = string.Empty;
        public string ScopeLabel = string.Empty;
        public string ExecutedAtUtc = string.Empty;
        public string Action = string.Empty;
        public string Note = string.Empty;
        public string Owner = string.Empty;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id", string.Empty);
            Scribe_Values.Look(ref ScopeKey, "scopeKey", string.Empty);
            Scribe_Values.Look(ref ScopeLabel, "scopeLabel", string.Empty);
            Scribe_Values.Look(ref ExecutedAtUtc, "executedAtUtc", string.Empty);
            Scribe_Values.Look(ref Action, "action", string.Empty);
            Scribe_Values.Look(ref Note, "note", string.Empty);
            Scribe_Values.Look(ref Owner, "owner", string.Empty);
            Id = Id ?? string.Empty;
            ScopeKey = ScopeKey ?? string.Empty;
            ScopeLabel = ScopeLabel ?? string.Empty;
            ExecutedAtUtc = ExecutedAtUtc ?? string.Empty;
            Action = Action ?? string.Empty;
            Note = Note ?? string.Empty;
            Owner = Owner ?? string.Empty;
        }
    }

    public interface IArtiOnceStore
    {
        bool Contains(string scopeKey, string id);

        void Add(ArtiOnceRecord record);

        IList<ArtiOnceRecord> Snapshot();

        bool Remove(string scopeKey, string id);

        void Clear();
    }

    public sealed class MemoryArtiOnceStore : IArtiOnceStore
    {
        private readonly List<ArtiOnceRecord> records = new List<ArtiOnceRecord>();

        public bool Contains(string scopeKey, string id)
        {
            return IndexOf(scopeKey, id) >= 0;
        }

        public void Add(ArtiOnceRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.Id) || Contains(record.ScopeKey, record.Id))
            {
                return;
            }

            records.Add(record);
        }

        public IList<ArtiOnceRecord> Snapshot()
        {
            return new List<ArtiOnceRecord>(records);
        }

        public bool Remove(string scopeKey, string id)
        {
            int index = IndexOf(scopeKey, id);
            if (index < 0)
            {
                return false;
            }

            records.RemoveAt(index);
            return true;
        }

        public void Clear()
        {
            records.Clear();
        }

        private int IndexOf(string scopeKey, string id)
        {
            for (int index = 0; index < records.Count; index++)
            {
                ArtiOnceRecord record = records[index];
                if (record != null
                    && string.Equals(record.ScopeKey, scopeKey ?? string.Empty, StringComparison.Ordinal)
                    && string.Equals(record.Id, id, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
    }

    public static class ArtiOnceStores
    {
        public static IArtiOnceStore Current { get; set; }

        public static Func<string> ScopeKey { get; set; }

        public static Func<string> ScopeLabel { get; set; }

        public static string CurrentScopeKey()
        {
            return ScopeKey == null ? string.Empty : ScopeKey() ?? string.Empty;
        }

        public static string CurrentScopeLabel()
        {
            return ScopeLabel == null ? string.Empty : ScopeLabel() ?? string.Empty;
        }
    }
}
