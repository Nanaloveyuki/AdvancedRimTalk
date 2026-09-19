using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;
using Verse;

namespace AdvancedRimTalk.Settings
{
    internal sealed class ArtiOnceSettingsStore : IArtiOnceStore
    {
        public bool Contains(string scopeKey, string id)
        {
            return IndexOf(scopeKey, id) >= 0;
        }

        public void Add(ArtiOnceRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.Id) || Contains(record.ScopeKey, record.Id))
            {
                return;
            }

            Records().Add(record);
            Persist();
        }

        public IList<ArtiOnceRecord> Snapshot()
        {
            return new List<ArtiOnceRecord>(Records());
        }

        public bool Remove(string scopeKey, string id)
        {
            int index = IndexOf(scopeKey, id);
            if (index < 0)
            {
                return false;
            }

            Records().RemoveAt(index);
            Persist();
            return true;
        }

        public void Clear()
        {
            List<ArtiOnceRecord> records = Records();
            if (records.Count == 0)
            {
                return;
            }

            records.Clear();
            Persist();
        }

        private static int IndexOf(string scopeKey, string id)
        {
            List<ArtiOnceRecord> records = Records();
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

        private static List<ArtiOnceRecord> Records()
        {
            AdvancedRimTalkSettings settings = AdvancedRimTalkMod.Settings;
            if (settings.OnceRecords == null)
            {
                settings.OnceRecords = new List<ArtiOnceRecord>();
            }

            return settings.OnceRecords;
        }

        private static void Persist()
        {
            AdvancedRimTalkSettings settings = AdvancedRimTalkMod.Settings;
            if (settings != null)
            {
                settings.Write();
            }
        }
    }
}
