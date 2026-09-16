using System;
using System.Collections.Generic;
using AdvancedRimTalk.Documentation;
using AdvancedRimTalk.Integration;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class DocumentationPage
    {
        private const float LeftPanelWidth = 255f;
        private const float PanelGap = 8f;
        private const float RowHeight = 24f;
        private static readonly Color PanelBackground =
            new Color(0.05f, 0.05f, 0.05f, 0.55f);
        private static readonly Color SectionColor =
            new Color(1f, 0.85f, 0.55f);

        private readonly AdvancedRimTalkDocumentationCatalog catalog;
        private readonly IrisMenusMarkdownRenderer markdown;
        private Vector2 navigationScrollPosition = Vector2.zero;
        private Vector2 documentScrollPosition = Vector2.zero;
        private DocumentationEntry selectedEntry;
        private bool sourceMode;
        private GUIStyle sourceStyle;
        private string pendingPath;
        private bool pendingBack;
        private bool focusNavigation;
        private readonly Stack<DocumentationEntry> back = new Stack<DocumentationEntry>();
        public IEnumerable<DocumentationEntry> Entries => catalog.Entries;

        public void Focus(string path) { pendingPath = path; }

        public DocumentationPage(string contentRoot)
        {
            catalog = AdvancedRimTalkDocumentationCatalog.Create(contentRoot);
            markdown = IrisMenusMarkdownRenderer.TryCreate(
                delegate
                {
                    return selectedEntry == null
                        ? string.Empty
                        : selectedEntry.Markdown;
                });
            selectedEntry = catalog.Overview;
            if (markdown != null) markdown.OpenLink = OpenLink;
        }

        private void OpenLink(string link)
        {
            if (Uri.TryCreate(link, UriKind.Absolute, out Uri uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                Application.OpenURL(link);
                return;
            }
            string path = AdvancedRimTalkDocumentationCatalog.ResolvePath(selectedEntry.RelativePath, Uri.UnescapeDataString(link));
            if (path != null && catalog.Find(path) != null) Focus(path);
            else Messages.Message("AdvancedRimTalk.Documentation.LinkUnavailable".Translate(link), RimWorld.MessageTypeDefOf.RejectInput);
        }

        public void Draw(Rect inRect)
        {
            if (pendingBack && back.Count > 0)
            {
                selectedEntry = back.Pop();
                focusNavigation = true;
                documentScrollPosition = Vector2.zero;
                GUI.FocusControl(null);
            }
            pendingBack = false;
            if (pendingPath != null)
            {
                var entry = catalog.Find(pendingPath);
                if (entry != null) Select(entry);
                pendingPath = null;
            }
            if (!catalog.IsAvailable)
            {
                Widgets.Label(
                    inRect,
                    "AdvancedRimTalk.Documentation.Unavailable".Translate());
                return;
            }

            if (selectedEntry == null)
            {
                selectedEntry = catalog.Overview;
            }

            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            Color previousColor = GUI.color;
            try
            {
                Rect leftRect = new Rect(
                    inRect.x,
                    inRect.y,
                    Mathf.Min(LeftPanelWidth, Mathf.Max(180f, inRect.width * 0.32f)),
                    inRect.height);
                Rect rightRect = new Rect(
                    leftRect.xMax + PanelGap,
                    inRect.y,
                    Mathf.Max(1f, inRect.width - leftRect.width - PanelGap),
                    inRect.height);

                DrawNavigation(leftRect);
                DrawDocument(rightRect);
            }
            finally
            {
                Text.Font = previousFont;
                Text.Anchor = previousAnchor;
                GUI.color = previousColor;
            }
        }

        private void DrawNavigation(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, PanelBackground);
            float contentWidth = Mathf.Max(1f, rect.width - 18f);
            float contentHeight = 64f + catalog.Categories.Count * 30f;
            foreach (DocumentationCategory category in catalog.Categories)
            {
                contentHeight += Mathf.Max(0, category.Documents.Count - 1)
                    * (RowHeight + 2f);
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                contentWidth,
                Mathf.Max(rect.height, contentHeight));
            Widgets.BeginScrollView(rect, ref navigationScrollPosition, viewRect);
            try
            {
                GUI.color = SectionColor;
                Widgets.Label(
                    new Rect(6f, 4f, viewRect.width - 12f, 22f),
                    "AdvancedRimTalk.Documentation.Index".Translate());
                GUI.color = Color.white;
                float y = 30f;
                y = DrawNavigationEntry(
                    viewRect,
                    y,
                    4f,
                    "AdvancedRimTalk.Documentation.Overview".Translate().ToString(),
                    catalog.Overview);
                y += 6f;

                foreach (DocumentationCategory category in catalog.Categories)
                {
                    DocumentationEntry categoryIndex = category.Documents[0];
                    Rect header = new Rect(
                        4f,
                        y,
                        Mathf.Max(1f, viewRect.width - 8f),
                        RowHeight);
                    if (categoryIndex == selectedEntry)
                    {
                        Widgets.DrawHighlight(header);
                        FocusNavigationRow(header.y);
                    }

                    GUI.color = SectionColor;
                    if (Widgets.ButtonText(
                        header,
                        category.TitleKey.Translate().ToString(),
                        false))
                    {
                        Select(categoryIndex);
                    }

                    GUI.color = Color.white;
                    y += RowHeight + 2f;
                    for (int index = 1; index < category.Documents.Count; index++)
                    {
                        y = DrawNavigationEntry(
                            viewRect,
                            y,
                            16f,
                            category.Documents[index].Title,
                            category.Documents[index]);
                    }

                    y += 4f;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private float DrawNavigationEntry(
            Rect viewRect,
            float y,
            float indent,
            string label,
            DocumentationEntry entry)
        {
            Rect row = new Rect(
                indent,
                y,
                Mathf.Max(1f, viewRect.width - indent - 4f),
                RowHeight);
            if (entry == selectedEntry)
            {
                Widgets.DrawHighlight(row);
                FocusNavigationRow(row.y);
            }

            if (Widgets.ButtonText(row, label, false))
            {
                Select(entry);
            }

            return y + RowHeight + 2f;
        }

        private void DrawDocument(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, PanelBackground);
            if (selectedEntry == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(
                    rect,
                    "AdvancedRimTalk.Documentation.SelectDocument".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            Text.Font = GameFont.Medium;
            float titleWidth = Mathf.Max(1f, rect.width - 24f);
            float titleHeight = Mathf.Max(
                30f,
                Text.CalcHeight(selectedEntry.Title, titleWidth));
            Widgets.Label(
                new Rect(rect.x + 12f, rect.y + 8f, titleWidth, titleHeight),
                selectedEntry.Title);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(
                new Rect(
                    rect.x + 12f,
                    rect.y + 8f + titleHeight,
                    titleWidth,
                    18f),
                selectedEntry.RelativePath);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            float toolbarY = rect.y + 34f + titleHeight;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && back.Count > 0;
            if (Widgets.ButtonText(new Rect(rect.x + 12f, toolbarY, 28f, 24f), "◀"))
            {
                pendingBack = true;
            }
            GUI.enabled = enabled;
            TooltipHandler.TipRegion(new Rect(rect.x + 12f, toolbarY, 28f, 24f), "AdvancedRimTalk.Documentation.Back".Translate());
            Rect sourceToggle = new Rect(rect.x + 48f, toolbarY,
                Mathf.Max(1f, rect.width - 156f), 24f);
            bool previousSourceMode = sourceMode;
            Widgets.CheckboxLabeled(sourceToggle,
                "AdvancedRimTalk.Documentation.Source".Translate().ToString(), ref sourceMode);
            if (sourceMode != previousSourceMode)
            {
                documentScrollPosition = Vector2.zero;
                GUI.FocusControl(null);
            }
            Rect copyButton = new Rect(rect.xMax - 102f, toolbarY, 90f, 24f);
            if (Widgets.ButtonText(copyButton, "AdvancedRimTalk.Documentation.Copy".Translate().ToString()))
            {
                GUIUtility.systemCopyBuffer = selectedEntry.Markdown ?? string.Empty;
            }
            Text.Font = GameFont.Small;

            Rect viewport = new Rect(
                rect.x + 8f,
                rect.y + 64f + titleHeight,
                Mathf.Max(1f, rect.width - 16f),
                Mathf.Max(1f, rect.height - 72f - titleHeight));
            float contentWidth = Mathf.Max(1f, viewport.width - 20f);
            if (sourceStyle == null)
            {
                sourceStyle = new GUIStyle(Text.CurTextAreaReadOnlyStyle) { richText = false };
            }

            bool showSource = sourceMode || markdown == null || markdown.Failed;
            float measuredHeight = showSource ? 0f : markdown.Measure(contentWidth);
            showSource = showSource || markdown.Failed;
            if (showSource)
            {
                measuredHeight = sourceStyle.CalcHeight(
                    new GUIContent(selectedEntry.Markdown ?? string.Empty), contentWidth);
            }
            float contentHeight = Mathf.Max(
                viewport.height,
                measuredHeight + 12f);
            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);

            Widgets.BeginScrollView(viewport, ref documentScrollPosition, viewRect);
            try
            {
                Rect contentRect = new Rect(
                    0f,
                    0f,
                    contentWidth,
                    Mathf.Max(contentHeight, measuredHeight));
                if (showSource)
                {
                    // Ignore edits: this is a selectable snapshot, never a document editor.
                    GUI.TextArea(contentRect, selectedEntry.Markdown ?? string.Empty, sourceStyle);
                }
                else
                {
                    markdown.Draw(contentRect);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void Select(DocumentationEntry entry)
        {
            if (entry == selectedEntry)
            {
                return;
            }

            if (selectedEntry != null) back.Push(selectedEntry);
            selectedEntry = entry;
            focusNavigation = true;
            documentScrollPosition = Vector2.zero;
            GUI.FocusControl(null);
        }

        private void FocusNavigationRow(float y)
        {
            if (!focusNavigation) return;
            navigationScrollPosition.y = Mathf.Max(0f, y - 30f);
            focusNavigation = false;
        }
    }
}
