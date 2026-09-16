using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AdvancedRimTalk.Prompt;
using AdvancedRimTalk.Settings;
using RimTalk.Prompt;
using RimWorld;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class TakeoverPromptPartsPage
    {
        private const float LeftPanelWidth = 220f;
        private const float PanelGap = 8f;
        private const float ButtonSize = 20f;
        private static readonly Color LeftPanelBackground = new Color(0.05f, 0.05f, 0.05f, 0.55f);
        private static readonly Color AddGreen = new Color(0.3f, 0.9f, 0.3f);
        private static readonly Color DeleteRed = new Color(1f, 0.4f, 0.4f);

        private Vector2 partListScrollPosition = Vector2.zero;
        private Vector2 contentScrollPosition = Vector2.zero;
        private string selectedPartId;
        private string selectedPresetId;
        private Vector2 presetScroll;
        private readonly ArtiEditorIntelligence contentIntelligence = new ArtiEditorIntelligence();
        private ArtiEditorAnalysis contentAnalysis;
        private string analyzedContent;
        private GUIStyle contentInputStyle;
        private GUIStyle contentSyntaxStyle;
        private float contentLineAdvance;
        private bool contentStylesInitialized;

        public void Draw(Rect inRect)
        {
            AdvancedRimTalkSettings settings = AdvancedRimTalkMod.Settings;
            if (settings == null)
            {
                Widgets.Label(inRect, "Advanced RimTalk settings are not available.");
                return;
            }

            settings.EnsureTakeoverPromptParts();

            Rect leftRect = new Rect(inRect.x, inRect.y, LeftPanelWidth, inRect.height);
            Rect rightRect = new Rect(
                leftRect.xMax + PanelGap,
                inRect.y,
                Mathf.Max(1f, inRect.width - LeftPanelWidth - PanelGap),
                inRect.height);

            DrawPresets(new Rect(leftRect.x, leftRect.y, leftRect.width, 196f), settings);
            ArtiPromptPreset preset = SelectedPreset(settings);
            List<ArtiPromptPart> parts = preset.Parts;
            EnsureSelection(parts);
            DrawPartList(new Rect(leftRect.x, leftRect.y + 202f, leftRect.width,
                Mathf.Max(1f, leftRect.height - 202f)), settings, parts);
            DrawPartEditor(rightRect, parts.FirstOrDefault(part => part.Id == selectedPartId));
        }

        private ArtiPromptPreset SelectedPreset(AdvancedRimTalkSettings settings)
        {
            return settings.TakeoverPresets.Find(preset => preset.Id == selectedPresetId)
                ?? settings.ActiveTakeoverPreset;
        }

        private void SelectPreset(ArtiPromptPreset preset)
        {
            selectedPresetId = preset.Id;
            selectedPartId = null;
            partListScrollPosition = contentScrollPosition = Vector2.zero;
            GUI.FocusControl(null);
        }

        private void DrawPresets(Rect rect, AdvancedRimTalkSettings settings)
        {
            Widgets.DrawBoxSolid(rect, LeftPanelBackground);
            var selected = SelectedPreset(settings);
            Widgets.Label(new Rect(rect.x + 5f, rect.y, rect.width - 36f, 24f),
                "AdvancedRimTalk.Presets.Title".Translate());
            if (Widgets.ButtonText(new Rect(rect.xMax - 27f, rect.y + 2f, 22f, 22f), "+"))
            {
                var created = new ArtiPromptPreset { Name = "AdvancedRimTalk.Presets.New".Translate(),
                    Parts = ArtiPromptPart.CreateDefaultParts(AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument) };
                settings.AddTakeoverPreset(created);
                SelectPreset(created);
                selected = created;
            }
            Rect list = new Rect(rect.x + 4f, rect.y + 26f, rect.width - 8f, 82f);
            Rect view = new Rect(0f, 0f, list.width - 16f, Mathf.Max(82f, settings.TakeoverPresets.Count * 26f));
            Widgets.BeginScrollView(list, ref presetScroll, view);
            try
            {
                float y = 0f;
                foreach (var preset in settings.TakeoverPresets)
                {
                    Rect row = new Rect(0f, y, view.width, 24f);
                    if (preset == selected) Widgets.DrawHighlight(row);
                    string label = (preset.Id == settings.ActiveTakeoverPresetId ? "▶ " : string.Empty) + preset.Name;
                    if (Widgets.ButtonText(row, label, false)) SelectPreset(preset);
                    y += 26f;
                }
            }
            finally { Widgets.EndScrollView(); }
            selected = SelectedPreset(settings);
            selected.Name = Widgets.TextField(new Rect(rect.x + 5f, rect.y + 112f, rect.width - 10f, 24f), selected.Name);
            float half = (rect.width - 15f) / 2f;
            if (Widgets.ButtonText(new Rect(rect.x + 5f, rect.y + 140f, half, 24f), "AdvancedRimTalk.Presets.Activate".Translate()))
                settings.ActivateTakeoverPreset(selected.Id);
            if (Widgets.ButtonText(new Rect(rect.x + 10f + half, rect.y + 140f, half, 24f), "AdvancedRimTalk.Presets.Copy".Translate()))
            {
                var copy = selected.Copy(selected.Name);
                settings.AddTakeoverPreset(copy);
                SelectPreset(copy);
            }
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && settings.TakeoverPresets.Count > 1;
            if (Widgets.ButtonText(new Rect(rect.x + 5f, rect.y + 168f, rect.width - 10f, 24f), "AdvancedRimTalk.Presets.Delete".Translate()))
            {
                var removed = selected;
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("AdvancedRimTalk.Presets.DeleteConfirm".Translate(removed.Name), () =>
                {
                    if (settings.TakeoverPresets.Count <= 1) return;
                    if (settings.ActiveTakeoverPresetId == removed.Id)
                        settings.ActivateTakeoverPreset(settings.TakeoverPresets.First(p => p.Id != removed.Id).Id);
                    settings.TakeoverPresets.Remove(removed);
                    SelectPreset(settings.ActiveTakeoverPreset);
                }));
            }
            GUI.enabled = enabled;
        }

        private void EnsureSelection(List<ArtiPromptPart> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                selectedPartId = null;
                return;
            }

            if (string.IsNullOrEmpty(selectedPartId) || !parts.Any(part => part.Id == selectedPartId))
            {
                selectedPartId = parts[0].Id;
            }
        }

        private void DrawPartList(Rect rect, AdvancedRimTalkSettings settings, List<ArtiPromptPart> parts)
        {
            Widgets.DrawBoxSolid(rect, LeftPanelBackground);
            float y = rect.y + 5f;
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x + 5f, y, rect.width - 35f, 20f), "AdvancedRimTalk.PromptParts.Parts".Translate());
            GUI.color = AddGreen;
            Rect addRect = new Rect(rect.xMax - ButtonSize - 7f, y, ButtonSize, ButtonSize);
            if (Widgets.ButtonText(addRect, "+"))
            {
                ArtiPromptPart part = new ArtiPromptPart(
                    UniqueName(parts, "AdvancedRimTalk.PromptParts.NewPart".Translate().ToString()),
                    PromptRole.User,
                    string.Empty);
                parts.Add(part);
                selectedPartId = part.Id;
                contentScrollPosition = Vector2.zero;
            }

            TooltipHandler.TipRegion(addRect, "AdvancedRimTalk.PromptParts.AddTooltip".Translate());
            GUI.color = Color.white;
            y += 24f;

            float listBottom = rect.yMax - 92f;
            Rect listRect = new Rect(rect.x + 2f, y, rect.width - 4f, Mathf.Max(40f, listBottom - y));
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, Mathf.Max(listRect.height, parts.Count * 25f));
            Widgets.BeginScrollView(listRect, ref partListScrollPosition, viewRect);
            float rowY = 0f;
            for (int index = 0; index < parts.Count; index++)
            {
                ArtiPromptPart part = parts[index];
                part.Normalize();
                Rect row = new Rect(0f, rowY, viewRect.width, 24f);
                if (part.Id == selectedPartId)
                {
                    Widgets.DrawHighlight(row);
                }

                bool enabled = part.Enabled;
                Widgets.Checkbox(new Vector2(4f, rowY + 4f), ref enabled, 16f);
                part.Enabled = enabled;

                GUI.color = part.Enabled ? Color.white : Color.gray;
                if (Widgets.ButtonText(new Rect(24f, rowY, viewRect.width - 50f, 24f), part.Name, false))
                {
                    selectedPartId = part.Id;
                    contentScrollPosition = Vector2.zero;
                }

                GUI.color = DeleteRed;
                Rect deleteRect = new Rect(viewRect.width - ButtonSize - 2f, rowY + 2f, ButtonSize, ButtonSize);
                if (Widgets.ButtonText(deleteRect, "×"))
                {
                    parts.RemoveAt(index);
                    EnsureSelection(parts);
                    GUI.color = Color.white;
                    Widgets.EndScrollView();
                    return;
                }

                GUI.color = Color.white;
                rowY += 25f;
            }

            Widgets.EndScrollView();

            float buttonWidth = (rect.width - 15f) / 2f;
            float buttonY = rect.yMax - 88f;
            if (Widgets.ButtonText(new Rect(rect.x + 5f, buttonY, buttonWidth, 24f), "AdvancedRimTalk.PromptParts.ImportLocal".Translate()))
            {
                ShowLocalImportMenu(settings);
            }

            if (Widgets.ButtonText(new Rect(rect.x + 10f + buttonWidth, buttonY, buttonWidth, 24f), "AdvancedRimTalk.PromptParts.ImportShared".Translate()))
            {
                ShowSharedImportMenu(settings);
            }

            buttonY += 28f;
            if (Widgets.ButtonText(new Rect(rect.x + 5f, buttonY, rect.width - 10f, 24f), "AdvancedRimTalk.PromptParts.ResetDefaults".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "AdvancedRimTalk.PromptParts.ResetDefaultsConfirm".Translate(),
                    delegate
                    {
                        parts.Clear();
                        parts.AddRange(ArtiPromptPart.CreateDefaultParts(
                            AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument));
                        selectedPartId = null;
                        EnsureSelection(parts);
                    }));
            }

            buttonY += 30f;
            ArtiPromptPart selected = parts.FirstOrDefault(part => part.Id == selectedPartId);
            int selectedIndex = selected == null ? -1 : parts.IndexOf(selected);
            DrawMoveButton(new Rect(rect.x + 5f, buttonY, buttonWidth, 24f), "▲", parts, selectedIndex, -1);
            DrawMoveButton(new Rect(rect.x + 10f + buttonWidth, buttonY, buttonWidth, 24f), "▼", parts, selectedIndex, 1);
        }

        private void DrawMoveButton(Rect rect, string label, List<ArtiPromptPart> parts, int selectedIndex, int direction)
        {
            bool canMove = selectedIndex >= 0
                && selectedIndex + direction >= 0
                && selectedIndex + direction < parts.Count;
            GUI.enabled = canMove;
            if (Widgets.ButtonText(rect, label) && canMove)
            {
                ArtiPromptPart part = parts[selectedIndex];
                parts.RemoveAt(selectedIndex);
                parts.Insert(selectedIndex + direction, part);
            }

            GUI.enabled = true;
        }

        private void DrawPartEditor(Rect rect, ArtiPromptPart part)
        {
            if (part == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "AdvancedRimTalk.PromptParts.SelectPart".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            part.Normalize();
            float y = rect.y + 2f;
            float labelX = rect.x + 10f;
            float inputX = rect.x + 130f;
            float inputWidth = 240f;
            float dropdownWidth = 120f;

            Widgets.Label(new Rect(labelX, y, inputX - labelX - 10f, 24f), "AdvancedRimTalk.PromptParts.Name".Translate());
            part.Name = Widgets.TextField(new Rect(inputX, y, inputWidth, 24f), part.Name);
            bool enabled = part.Enabled;
            Widgets.CheckboxLabeled(new Rect(rect.xMax - 180f, y, 170f, 24f), "AdvancedRimTalk.PromptParts.Enabled".Translate(), ref enabled);
            part.Enabled = enabled;
            y += 28f;

            Widgets.Label(new Rect(labelX, y, inputX - labelX - 10f, 24f), "AdvancedRimTalk.PromptParts.Role".Translate());
            if (Widgets.ButtonText(new Rect(inputX, y, dropdownWidth, 24f), part.Role.ToString()))
            {
                ShowRoleMenu(part);
            }

            Widgets.Label(new Rect(inputX + dropdownWidth + 12f, y, 85f, 24f), "AdvancedRimTalk.PromptParts.CustomRole".Translate());
            part.CustomRole = Widgets.TextField(new Rect(inputX + dropdownWidth + 100f, y, inputWidth, 24f), part.CustomRole ?? string.Empty);
            y += 30f;

            if (Widgets.ButtonText(new Rect(labelX, y, Mathf.Min(240f, rect.width - 20f), 28f),
                "AdvancedRimTalk.ArtiEditor.Title".Translate()))
            {
                Find.WindowStack.Add(new ArtiPartEditorWindow(part));
            }
            y += 34f;

            Rect editorRect = new Rect(rect.x + 10f, y, rect.width - 20f, rect.yMax - y - 5f);
            DrawContentEditor(editorRect, part);
        }

        private void DrawContentEditor(Rect editorRect, ArtiPromptPart part)
        {
            bool previousWordWrap = Text.WordWrap;
            try
            {
                Text.WordWrap = false;
                EnsureContentEditorStyles();

                string content = ArtiEditorText.NormalizeLineEndings(part.Content);
                if (!string.Equals(part.Content, content, StringComparison.Ordinal))
                {
                    part.Content = content;
                }

                RefreshContentAnalysis(content);
                float viewportWidth = Mathf.Max(1f, editorRect.width - 20f);
                float contentWidth = Mathf.Max(
                    viewportWidth,
                    GetMaxContentLineWidth(content)
                        + contentInputStyle.padding.left
                        + contentInputStyle.padding.right
                        + 16f);
                float contentHeight = Mathf.Ceil(Mathf.Max(
                    editorRect.height,
                    ArtiEditorText.GetLineCount(content) * contentLineAdvance
                        + contentInputStyle.padding.top
                        + contentInputStyle.padding.bottom
                        + 8f));
                Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
                Widgets.BeginScrollView(editorRect, ref contentScrollPosition, viewRect);
                try
                {
                    Rect syntaxRect = new Rect(
                        contentInputStyle.padding.left,
                        contentInputStyle.padding.top,
                        Mathf.Max(
                            1f,
                            contentWidth
                                - contentInputStyle.padding.left
                                - contentInputStyle.padding.right),
                        Mathf.Max(
                            1f,
                            contentHeight
                                - contentInputStyle.padding.top
                                - contentInputStyle.padding.bottom));
                    GUI.Label(
                        syntaxRect,
                        ArtiSyntaxRendering.ToRichText(content, contentAnalysis),
                        contentSyntaxStyle);
                    GUI.SetNextControlName("AdvancedRimTalk.PromptParts.Content");
                    string edited = GUI.TextArea(
                        new Rect(0f, 0f, contentWidth, contentHeight),
                        content,
                        contentInputStyle);
                    string normalized = ArtiEditorText.NormalizeLineEndings(edited);
                    if (!string.Equals(content, normalized, StringComparison.Ordinal))
                    {
                        part.Content = normalized;
                        contentAnalysis = null;
                        analyzedContent = null;
                    }
                }
                finally
                {
                    Widgets.EndScrollView();
                }
            }
            finally
            {
                Text.WordWrap = previousWordWrap;
            }
        }

        private void RefreshContentAnalysis(string content)
        {
            if (contentAnalysis != null
                && string.Equals(analyzedContent, content, StringComparison.Ordinal))
            {
                return;
            }

            contentAnalysis = contentIntelligence.AnalyzeDocument(content);
            analyzedContent = content;
        }

        private void EnsureContentEditorStyles()
        {
            if (contentStylesInitialized)
            {
                return;
            }

            contentInputStyle = ArtiSyntaxRendering.CreateInputStyle();
            contentSyntaxStyle = ArtiSyntaxRendering.CreateSyntaxStyle(contentInputStyle);
            contentLineAdvance = Mathf.Max(16f, contentInputStyle.lineHeight);
            contentStylesInitialized = true;
        }

        private float GetMaxContentLineWidth(string content)
        {
            float width = 0f;
            int lineStart = 0;
            while (lineStart <= content.Length)
            {
                int lineEnd = ArtiEditorText.GetLineEnd(content, lineStart);
                if (lineEnd > lineStart)
                {
                    width = Mathf.Max(
                        width,
                        contentSyntaxStyle.CalcSize(new GUIContent(
                            content.Substring(lineStart, lineEnd - lineStart))).x);
                }

                if (lineEnd >= content.Length)
                {
                    break;
                }

                lineStart = lineEnd + 1;
            }

            return width;
        }

        private void ShowRoleMenu(ArtiPromptPart part)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("System", delegate { part.Role = PromptRole.System; }),
                new FloatMenuOption("User", delegate { part.Role = PromptRole.User; }),
                new FloatMenuOption("Assistant", delegate { part.Role = PromptRole.Assistant; })
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowLocalImportMenu(AdvancedRimTalkSettings settings)
        {
            PromptManager manager = PromptManager.Instance;
            List<PromptPreset> presets = manager == null ? new List<PromptPreset>() : manager.Presets ?? new List<PromptPreset>();
            if (presets.Count == 0)
            {
                Messages.Message("AdvancedRimTalk.PromptParts.NoLocalPrompts".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (PromptPreset preset in presets)
            {
                PromptPreset captured = preset;
                options.Add(new FloatMenuOption(
                    captured.Name,
                    delegate { ImportPreset(settings, captured, captured.Name); }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowSharedImportMenu(AdvancedRimTalkSettings settings)
        {
            List<string> files = PresetSerializer.GetAvailablePresetFiles();
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (string file in files)
            {
                string capturedFile = file;
                string fileName = Path.GetFileNameWithoutExtension(file);
                options.Add(new FloatMenuOption(
                    fileName,
                    delegate
                    {
                        PromptPreset preset = PresetSerializer.ImportFromFile(capturedFile);
                        if (preset == null)
                        {
                            Messages.Message("AdvancedRimTalk.PromptParts.ImportFailed".Translate(), MessageTypeDefOf.RejectInput);
                            return;
                        }

                        ImportPreset(settings, preset, fileName);
                    }));
            }

            options.Add(new FloatMenuOption(
                "AdvancedRimTalk.PromptParts.OpenSharedFolder".Translate(),
                delegate
                {
                    string directory = PresetSerializer.GetExportDirectory();
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    });
                }));

            if (files.Count == 0)
            {
                Messages.Message("AdvancedRimTalk.PromptParts.NoSharedPrompts".Translate(), MessageTypeDefOf.NeutralEvent);
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ImportPreset(AdvancedRimTalkSettings settings, PromptPreset preset, string sourceName)
        {
            if (preset == null || preset.Entries == null || preset.Entries.Count == 0)
            {
                Messages.Message("AdvancedRimTalk.PromptParts.ImportFailed".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            settings.EnsureTakeoverPromptParts();
            var importedPreset = new ArtiPromptPreset { Name = sourceName };
            List<ArtiPromptPart> parts = importedPreset.Parts;
            int imported = 0;
            string firstImportedId = null;
            foreach (PromptEntry entry in preset.Entries)
            {
                string baseName = string.IsNullOrWhiteSpace(entry.Name) ? preset.Name : entry.Name;
                ArtiPromptPart part = ArtiPromptPart.FromRimTalkEntry(entry, UniqueName(parts, baseName));
                parts.Add(part);
                if (firstImportedId == null)
                {
                    firstImportedId = part.Id;
                }

                imported++;
            }

            if (firstImportedId != null)
            {
                settings.AddTakeoverPreset(importedPreset);
                SelectPreset(importedPreset);
                selectedPartId = firstImportedId;
                contentScrollPosition = Vector2.zero;
            }

            string message = string.Format(
                "AdvancedRimTalk.PromptParts.Imported".Translate().ToString(),
                imported,
                sourceName);
            Messages.Message(message, MessageTypeDefOf.PositiveEvent);
        }

        private static string UniqueName(List<ArtiPromptPart> parts, string baseName)
        {
            string normalizedBase = string.IsNullOrWhiteSpace(baseName) ? "Prompt Part" : baseName.Trim();
            if (parts == null || !parts.Any(part => string.Equals(part.Name, normalizedBase, StringComparison.Ordinal)))
            {
                return normalizedBase;
            }

            int index = 1;
            while (true)
            {
                string candidate = normalizedBase + " (" + index + ")";
                if (!parts.Any(part => string.Equals(part.Name, candidate, StringComparison.Ordinal)))
                {
                    return candidate;
                }

                index++;
            }
        }
    }
}
