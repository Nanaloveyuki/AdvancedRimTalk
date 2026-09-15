using System;
using System.Collections.Generic;
using AdvancedRimTalk.Prompt;
using RimTalk.Data;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class PromptPreviewPage
    {
        private static Game capturedGame;
        private static readonly PromptPreview[] previews = new PromptPreview[2];
        private static readonly string[] diagnostics = new string[2];
        private static readonly bool[] contextual = new bool[2];
        private bool takeover;
        private bool json;
        private Vector2 scroll;
        private bool experimentalContext;
        private string request = string.Empty;

        internal static void Capture(IList<ValueTuple<Role, string>> messages, bool isTakeover)
        {
            if (Current.Game == null) return;
            if (!ReferenceEquals(capturedGame, Current.Game))
            {
                capturedGame = Current.Game;
                ClearResults();
            }
            int mode = isTakeover ? 1 : 0;
            previews[mode] = PromptPreview.FromMessages(messages);
            diagnostics[mode] = string.Empty;
            contextual[mode] = false;
        }

        private static void ClearResults()
        {
            previews[0] = previews[1] = null;
            diagnostics[0] = diagnostics[1] = string.Empty;
            contextual[0] = contextual[1] = false;
        }

        internal void Draw(Rect rect)
        {
            if (!ReferenceEquals(capturedGame, Current.Game))
            {
                ClearResults();
                capturedGame = Current.Game;
            }
            float half = rect.width / 2f;
            if (Widgets.RadioButtonLabeled(new Rect(rect.x, rect.y, half, 28f),
                "AdvancedRimTalk.Settings.EmbedArtiIntoRimTalkPrompt".Translate(), !takeover))
            {
                takeover = false;
                scroll = Vector2.zero;
            }
            if (Widgets.RadioButtonLabeled(new Rect(rect.x + half, rect.y, half, 28f),
                "AdvancedRimTalk.Settings.TakeoverRimTalkPrompt".Translate(), takeover))
            {
                takeover = true;
                scroll = Vector2.zero;
            }
            if (Widgets.RadioButtonLabeled(new Rect(rect.x, rect.y + 32f, half, 28f),
                "AdvancedRimTalk.Preview.Text".Translate(), !json)) json = false;
            if (Widgets.RadioButtonLabeled(new Rect(rect.x + half, rect.y + 32f, half, 28f),
                "AdvancedRimTalk.Preview.Json".Translate(), json)) json = true;

            Widgets.CheckboxLabeled(new Rect(rect.x, rect.y + 64f, half, 28f),
                "AdvancedRimTalk.Preview.Experimental".Translate(), ref experimentalContext);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && experimentalContext && Current.ProgramState == ProgramState.Playing;
            if (Widgets.ButtonText(new Rect(rect.x + half, rect.y + 64f, half, 28f), "AdvancedRimTalk.Preview.Generate".Translate()))
            {
                try
                {
                    capturedGame = Current.Game;
                    previews[takeover ? 1 : 0] = ContextPromptPreview.Build(takeover, request, out diagnostics[takeover ? 1 : 0]);
                    contextual[takeover ? 1 : 0] = true;
                }
                catch (Exception exception)
                {
                    previews[takeover ? 1 : 0] = null;
                    diagnostics[takeover ? 1 : 0] = exception.GetBaseException().Message;
                }
                scroll = Vector2.zero;
            }
            GUI.enabled = previousEnabled;
            float requestLabelWidth = Mathf.Min(160f, rect.width * 0.35f);
            Widgets.Label(new Rect(rect.x, rect.y + 96f, requestLabelWidth, 28f), "AdvancedRimTalk.Preview.Request".Translate());
            request = Widgets.TextField(new Rect(rect.x + requestLabelWidth, rect.y + 96f,
                Mathf.Max(1f, rect.width - requestLabelWidth), 28f), request);

            PromptPreview preview = Current.ProgramState == ProgramState.Playing
                && ReferenceEquals(capturedGame, Current.Game) ? previews[takeover ? 1 : 0] : null;
            string content = preview == null ? "AdvancedRimTalk.Preview.NoCapture".Translate().ToString()
                : json ? preview.RawResponseJson : preview.PlainText;
            string previewDiagnostics = diagnostics[takeover ? 1 : 0];
            if (preview != null) Widgets.Label(new Rect(rect.x, rect.y + 128f, rect.width, 24f),
                (contextual[takeover ? 1 : 0] ? "AdvancedRimTalk.Preview.ContextResult" : "AdvancedRimTalk.Preview.CapturedResult").Translate());
            float diagnosticHeight = string.IsNullOrEmpty(previewDiagnostics) ? 0f : 80f;
            if (diagnosticHeight > 0f)
                Widgets.TextArea(new Rect(rect.x, rect.yMax - diagnosticHeight, rect.width, diagnosticHeight), previewDiagnostics, true);
            Rect viewport = new Rect(rect.x, rect.y + 154f, rect.width, Mathf.Max(1f, rect.height - 154f - diagnosticHeight));
            float width = Mathf.Max(1f, viewport.width - 20f);
            Rect view = new Rect(0f, 0f, width, Mathf.Max(viewport.height, Text.CalcHeight(content, width) + 12f));
            Widgets.BeginScrollView(viewport, ref scroll, view);
            try { Widgets.TextArea(view, content, true); }
            finally { Widgets.EndScrollView(); }
        }
    }
}
