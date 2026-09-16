using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedRimTalk.Prompt;
using RimTalk.Data;
using RimTalk.Prompt;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class PromptPreviewPage
    {
        private static Game capturedGame;
        private static readonly PromptPreview[] previews = new PromptPreview[2];
        private static readonly string[] captureSources = new string[2];
        private string presetId;
        private PromptPreview generated;
        private string generatedSource;
        private string generatedDiagnostics;
        private bool showCaptured = true;
        private bool initialized;
        private Game generatedGame;
        private GUIStyle outputStyle;
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
            captureSources[mode] = (isTakeover ? AdvancedRimTalkMod.Settings.ActiveTakeoverPreset.Name
                : PromptManager.Instance.GetActivePreset()?.Name) + " · " + DateTime.Now.ToString("HH:mm:ss");
        }

        private static void ClearResults()
        {
            previews[0] = previews[1] = null;
            captureSources[0] = captureSources[1] = null;
        }

        internal void Draw(Rect rect)
        {
            if (!initialized)
            {
                takeover = AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism;
                initialized = true;
            }
            if (!ReferenceEquals(capturedGame, Current.Game))
            {
                ClearResults();
                capturedGame = Current.Game;
                generated = null;
                generatedDiagnostics = null;
            }
            if (!ReferenceEquals(generatedGame, Current.Game))
            {
                generated = null;
                generatedDiagnostics = null;
                generatedGame = Current.Game;
            }
            float half = rect.width / 2f;
            bool previousMode = takeover;
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
            if (previousMode != takeover)
            {
                presetId = null;
                generated = null;
                generatedDiagnostics = null;
            }
            var settings = AdvancedRimTalkMod.Settings;
            var activeTakeover = settings.ActiveTakeoverPreset;
            var activeRimTalk = PromptManager.Instance.GetActivePreset();
            var selectedTakeover = settings.TakeoverPresets.Find(p => p.Id == presetId) ?? activeTakeover;
            var selectedRimTalk = PromptManager.Instance.Presets.FirstOrDefault(p => p.Id == presetId) ?? activeRimTalk;
            string selectedName = takeover ? selectedTakeover.Name : selectedRimTalk?.Name ?? "-";
            string activeName = AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism ? activeTakeover.Name : activeRimTalk?.Name ?? "-";
            string activeMode = (AdvancedRimTalkMod.ShouldReplaceRimTalkPromptMechanism
                ? "AdvancedRimTalk.Preview.Takeover" : "AdvancedRimTalk.Preview.Embed").Translate();
            Widgets.Label(new Rect(rect.x, rect.y + 32f, rect.width, 26f),
                "AdvancedRimTalk.Preview.Active".Translate(activeMode, activeName));
            Widgets.Label(new Rect(rect.x, rect.y + 62f, 130f, 28f), "AdvancedRimTalk.Preview.Preset".Translate());
            if (Widgets.ButtonText(new Rect(rect.x + 134f, rect.y + 62f, Mathf.Max(1f, rect.width - 134f), 28f), selectedName))
            {
                var options = new List<FloatMenuOption>();
                Action<string> select = id =>
                {
                    presetId = id; generated = null; generatedDiagnostics = null;
                    scroll = Vector2.zero; showCaptured = false;
                };
                if (takeover)
                    foreach (var preset in settings.TakeoverPresets)
                    {
                        var item = preset;
                        options.Add(new FloatMenuOption(item.Name, () => select(item.Id)));
                    }
                else
                    foreach (var preset in PromptManager.Instance.Presets)
                    {
                        var item = preset;
                        options.Add(new FloatMenuOption(item.Name, () => select(item.Id)));
                    }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (Widgets.RadioButtonLabeled(new Rect(rect.x, rect.y + 94f, half, 26f),
                "AdvancedRimTalk.Preview.CapturedResult".Translate(), showCaptured)) showCaptured = true;
            if (Widgets.RadioButtonLabeled(new Rect(rect.x + half, rect.y + 94f, half, 26f),
                "AdvancedRimTalk.Preview.ContextResult".Translate(), !showCaptured)) showCaptured = false;
            rect = new Rect(rect.x, rect.y + 94f, rect.width, Mathf.Max(1f, rect.height - 94f));
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
                    showCaptured = false;
                    generated = null;
                    generatedDiagnostics = null;
                    generatedSource = selectedName + " · " + DateTime.Now.ToString("HH:mm:ss");
                    generated = ContextPromptPreview.Build(takeover, request, out generatedDiagnostics, selectedTakeover, selectedRimTalk);
                }
                catch (Exception exception)
                {
                    generated = null;
                    generatedDiagnostics = exception.GetBaseException().Message;
                }
                scroll = Vector2.zero;
            }
            GUI.enabled = previousEnabled;
            float requestLabelWidth = Mathf.Min(160f, rect.width * 0.35f);
            Widgets.Label(new Rect(rect.x, rect.y + 96f, requestLabelWidth, 28f), "AdvancedRimTalk.Preview.Request".Translate());
            string previousRequest = request;
            request = Widgets.TextField(new Rect(rect.x + requestLabelWidth, rect.y + 96f,
                Mathf.Max(1f, rect.width - requestLabelWidth), 28f), request);
            if (previousRequest != request) { generated = null; generatedDiagnostics = null; }

            PromptPreview preview = Current.ProgramState == ProgramState.Playing
                && ReferenceEquals(capturedGame, Current.Game) ? (showCaptured ? previews[takeover ? 1 : 0] : generated) : null;
            string content = preview == null ? "AdvancedRimTalk.Preview.NoCapture".Translate().ToString()
                : json ? preview.RawResponseJson : preview.PlainText;
            string heading = "AdvancedRimTalk.Preview.ResultSource".Translate(
                (showCaptured ? "AdvancedRimTalk.Preview.CapturedResult" : "AdvancedRimTalk.Preview.ContextResult").Translate(),
                preview == null ? "-" : showCaptured ? captureSources[takeover ? 1 : 0] : generatedSource).ToString();
            float headingHeight = Mathf.Max(24f, Text.CalcHeight(heading, rect.width));
            Widgets.Label(new Rect(rect.x, rect.y + 128f, rect.width, headingHeight), heading);
            if (!showCaptured && !string.IsNullOrEmpty(generatedDiagnostics)) content += "\n\n" + generatedDiagnostics;
            Rect viewport = new Rect(rect.x, rect.y + 134f + headingHeight, rect.width, Mathf.Max(1f, rect.height - 134f - headingHeight));
            Widgets.DrawBoxSolid(viewport, new Color(0.055f, 0.06f, 0.065f, 0.98f));
            viewport = viewport.ContractedBy(8f);
            if (outputStyle == null) outputStyle = new GUIStyle(Text.CurTextAreaReadOnlyStyle) { richText = false };
            float width = Mathf.Max(1f, viewport.width - 20f);
            Rect view = new Rect(0f, 0f, width, Mathf.Max(viewport.height, outputStyle.CalcHeight(new GUIContent(content), width) + 12f));
            Widgets.BeginScrollView(viewport, ref scroll, view);
            try { GUI.TextArea(view, content, outputStyle); }
            finally { Widgets.EndScrollView(); }
        }
    }
}
