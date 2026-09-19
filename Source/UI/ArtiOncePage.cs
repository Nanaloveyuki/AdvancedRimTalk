using System;
using System.Collections.Generic;
using System.Globalization;
using AdvancedRimTalk.Arti;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiOncePage
    {
        private Vector2 scroll;
        private bool currentWorldOnly = true;

        public void Draw(Rect rect)
        {
            IArtiOnceStore store = ArtiOnceStores.Current;
            if (store == null)
            {
                Widgets.Label(rect, "AdvancedRimTalk.Settings.Unavailable".Translate());
                return;
            }

            float y = rect.y;
            Rect helpRect = new Rect(rect.x, y, rect.width, 44f);
            Widgets.Label(helpRect, "AdvancedRimTalk.Once.Help".Translate());
            y += 48f;

            Widgets.CheckboxLabeled(
                new Rect(rect.x, y, Mathf.Max(1f, rect.width - 120f), 28f),
                "AdvancedRimTalk.Once.CurrentWorldOnly".Translate(),
                ref currentWorldOnly);
            if (Widgets.ButtonText(new Rect(rect.xMax - 110f, y, 110f, 28f), "AdvancedRimTalk.Once.Clear".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "AdvancedRimTalk.Once.ClearConfirm".Translate(),
                    delegate { store.Clear(); }));
            }

            y += 34f;
            Rect listRect = new Rect(rect.x, y, rect.width, Mathf.Max(1f, rect.yMax - y));
            List<ArtiOnceRecord> records = VisibleRecords(store.Snapshot(), currentWorldOnly);
            if (records.Count == 0)
            {
                Widgets.Label(listRect, "AdvancedRimTalk.Once.Empty".Translate());
                return;
            }

            float width = Mathf.Max(1f, listRect.width - 20f);
            float height = 0f;
            string[] lines = new string[records.Count];
            for (int index = 0; index < records.Count; index++)
            {
                lines[index] = Format(records[index]);
                height += Mathf.Max(52f, Text.CalcHeight(lines[index], width - 88f) + 12f);
            }

            Widgets.BeginScrollView(listRect, ref scroll, new Rect(0f, 0f, width, height));
            float rowY = 0f;
            for (int index = 0; index < records.Count; index++)
            {
                ArtiOnceRecord record = records[index];
                float rowHeight = Mathf.Max(52f, Text.CalcHeight(lines[index], width - 88f) + 12f);
                Rect row = new Rect(0f, rowY, width, rowHeight);
                if (index % 2 == 0)
                {
                    Widgets.DrawHighlight(row);
                }

                Widgets.Label(new Rect(row.x + 6f, row.y + 4f, row.width - 92f, row.height - 8f), lines[index]);
                if (Widgets.ButtonText(new Rect(row.xMax - 80f, row.y + 8f, 74f, 28f), "AdvancedRimTalk.Once.Delete".Translate()))
                {
                    ArtiOnceRecord toRemove = record;
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "AdvancedRimTalk.Once.DeleteConfirm".Translate(toRemove.Id),
                        delegate { store.Remove(toRemove.ScopeKey, toRemove.Id); }));
                }

                rowY += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private static List<ArtiOnceRecord> VisibleRecords(IList<ArtiOnceRecord> records, bool currentWorldOnly)
        {
            string scope = ArtiOnceStores.CurrentScopeKey();
            List<ArtiOnceRecord> visible = new List<ArtiOnceRecord>();
            for (int index = 0; index < records.Count; index++)
            {
                ArtiOnceRecord record = records[index];
                if (record == null)
                {
                    continue;
                }

                if (currentWorldOnly && !string.Equals(record.ScopeKey, scope, StringComparison.Ordinal))
                {
                    continue;
                }

                visible.Add(record);
            }

            visible.Sort(CompareNewestFirst);
            return visible;
        }

        private static int CompareNewestFirst(ArtiOnceRecord left, ArtiOnceRecord right)
        {
            return string.CompareOrdinal(right.ExecutedAtUtc, left.ExecutedAtUtc);
        }

        private static string Format(ArtiOnceRecord record)
        {
            return "AdvancedRimTalk.Once.Entry".Translate(
                record.Id,
                FormatTime(record.ExecutedAtUtc),
                DisplayScope(record),
                string.IsNullOrWhiteSpace(record.Action) ? "-" : record.Action,
                string.IsNullOrWhiteSpace(record.Note) ? "-" : record.Note,
                DisplayOwner(record.Owner)).ToString();
        }

        private static string DisplayScope(ArtiOnceRecord record)
        {
            if (string.Equals(record.ScopeKey, "global", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(record.ScopeLabel))
            {
                return "AdvancedRimTalk.Once.Global".Translate().ToString();
            }

            return record.ScopeLabel;
        }

        private static string DisplayOwner(string owner)
        {
            if (string.Equals(owner, "repl", StringComparison.OrdinalIgnoreCase))
            {
                return "AdvancedRimTalk.Once.OwnerRepl".Translate().ToString();
            }

            if (string.IsNullOrWhiteSpace(owner) || string.Equals(owner, "prompt-document", StringComparison.Ordinal))
            {
                return "AdvancedRimTalk.Once.OwnerPrompt".Translate().ToString();
            }

            return owner;
        }

        private static string FormatTime(string iso)
        {
            DateTimeOffset value;
            if (DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value))
            {
                return value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            return string.IsNullOrEmpty(iso) ? "-" : iso;
        }
    }

    internal sealed class ArtiOnceWindow : Window
    {
        private readonly ArtiOncePage page = new ArtiOncePage();

        public ArtiOnceWindow()
        {
            doCloseX = true;
            draggable = true;
            resizeable = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(760f, 560f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 40f, 28f), "AdvancedRimTalk.Once.Title".Translate());
            page.Draw(new Rect(inRect.x, inRect.y + 32f, inRect.width, Mathf.Max(1f, inRect.height - 32f)));
        }
    }
}
