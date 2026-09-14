using System;
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
        }

        public void Draw(Rect inRect)
        {
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

            Rect viewport = new Rect(
                rect.x + 8f,
                rect.y + 34f + titleHeight,
                Mathf.Max(1f, rect.width - 16f),
                Mathf.Max(1f, rect.height - 42f - titleHeight));
            float contentWidth = Mathf.Max(1f, viewport.width - 20f);
            float measuredHeight = markdown == null || markdown.Failed
                ? Text.CalcHeight(selectedEntry.Markdown, contentWidth)
                : markdown.Measure(contentWidth);
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
                if (markdown == null || markdown.Failed)
                {
                    Widgets.Label(contentRect, selectedEntry.Markdown);
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

            selectedEntry = entry;
            documentScrollPosition = Vector2.zero;
        }
    }
}
