using AdvancedRimTalk.Diagnostics;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiLogPage
    {
        private Vector2 scroll;
        public void Draw(Rect rect)
        {
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 100f, 28f), "AdvancedRimTalk.ArtiLog.Clear".Translate())) ArtiLogBuffer.Clear();
            rect.yMin += 34f;
            var entries = ArtiLogBuffer.Snapshot();
            float width = Mathf.Max(1f, rect.width - 20f);
            float height = 0f;
            foreach (var entry in entries) height += Mathf.Max(24f, Text.CalcHeight(entry.Line, width));
            Widgets.BeginScrollView(rect, ref scroll, new Rect(0, 0, width, height));
            Color previous = GUI.color;
            try
            {
                float y = 0f;
                foreach (var entry in entries)
                {
                    GUI.color = entry.Level == "ERROR" ? new Color(1f, .45f, .45f)
                        : entry.Level == "WARN" ? new Color(1f, .8f, .3f) : Color.white;
                    float rowHeight = Mathf.Max(24f, Text.CalcHeight(entry.Line, width));
                    Widgets.Label(new Rect(0, y, width, rowHeight), entry.Line);
                    y += rowHeight;
                }
            }
            finally { GUI.color = previous; Widgets.EndScrollView(); }
        }
    }
}
