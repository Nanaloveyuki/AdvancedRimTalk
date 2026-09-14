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

        public void Draw(Rect inRect)
        {
            AdvancedRimTalkSettings settings = AdvancedRimTalkMod.Settings;
            if (settings == null)
            {
                Widgets.Label(inRect, "Advanced RimTalk settings are not available.");
                return;
            }

            settings.EnsureTakeoverPromptParts();
            List<ArtiPromptPart> parts = settings.TakeoverPromptParts;
            EnsureSelection(parts);

            Rect leftRect = new Rect(inRect.x, inRect.y, LeftPanelWidth, inRect.height);
            Rect rightRect = new Rect(
                leftRect.xMax + PanelGap,
                inRect.y,
                Mathf.Max(1f, inRect.width - LeftPanelWidth - PanelGap),
                inRect.height);

            DrawPartList(leftRect, settings, parts);
            DrawPartEditor(rightRect, parts.FirstOrDefault(part => part.Id == selectedPartId));
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
                        settings.TakeoverArtiPromptDocument = AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument;
                        settings.TakeoverPromptParts = ArtiPromptPart.CreateDefaultParts(
                            AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument);
                        selectedPartId = null;
                        EnsureSelection(settings.TakeoverPromptParts);
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

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(labelX, y, rect.width - 20f, 20f), "AdvancedRimTalk.PromptParts.Help".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 22f;

            Rect editorRect = new Rect(rect.x + 10f, y, rect.width - 20f, rect.yMax - y - 5f);
            float innerWidth = editorRect.width - 20f;
            float contentHeight = Mathf.Ceil(Mathf.Max(editorRect.height, Text.CalcHeight(part.Content, innerWidth) + 25f));
            Rect viewRect = new Rect(0f, 0f, innerWidth, contentHeight);
            Widgets.BeginScrollView(editorRect, ref contentScrollPosition, viewRect);
            part.Content = Widgets.TextArea(new Rect(0f, 0f, innerWidth, contentHeight), part.Content ?? string.Empty);
            Widgets.EndScrollView();
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
            List<ArtiPromptPart> parts = settings.TakeoverPromptParts;
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
