using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AdvancedRimTalk.Documentation;
using AdvancedRimTalk.UI;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal sealed class IrisMenusMarkdownRenderer
    {
        private readonly Func<string> getMarkdown;
        private readonly MethodInfo parseMethod;
        private readonly ArtiEditorIntelligence intelligence = new ArtiEditorIntelligence();
        private string cachedSource;
        private List<DocumentationBlock> blocks;
        private readonly List<DrawItem> layout = new List<DrawItem>();
        private readonly List<Rect> tableRows = new List<Rect>();
        private readonly List<Rect> tableHeaders = new List<Rect>();
        private float layoutWidth = -1f;
        private float layoutHeight;
        private GUIStyle plainStyle;
        public Action<string> OpenLink;

        private sealed class DrawItem
        {
            public Rect Rect;
            public string Text;
            public string Link;
            public bool Rich;
        }
        private bool failureLogged;

        private IrisMenusMarkdownRenderer(
            Func<string> getMarkdown,
            MethodInfo parseMethod)
        {
            this.getMarkdown = getMarkdown;
            this.parseMethod = parseMethod;
        }

        public bool Failed { get; private set; }

        public static IrisMenusMarkdownRenderer TryCreate(Func<string> getMarkdown)
        {
            if (getMarkdown == null)
            {
                throw new ArgumentNullException(nameof(getMarkdown));
            }

            try
            {
                Type markdownType = FindType("IrisMenus.Markdown");
                if (markdownType == null)
                {
                    return null;
                }

                MethodInfo parse = markdownType.GetMethod(
                    "Parse",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);
                if (parse == null)
                {
                    return null;
                }

                return new IrisMenusMarkdownRenderer(getMarkdown, parse);
            }
            catch (Exception exception)
            {
                Log.Warning(
                    "[Advanced RimTalk] Could not connect to IrisMenus Markdown: "
                    + exception);
                return null;
            }
        }

        private static Type FindType(string name)
        {
            Type type = GenTypes.GetTypeInAnyAssembly(name);
            if (type != null)
            {
                return type;
            }

            type = Type.GetType(name + ", IrisMenus", false);
            if (type != null)
            {
                return type;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public float Measure(float width)
        {
            try
            {
                Refresh();
                Layout(Mathf.Max(1f, width));
                return layoutHeight;
            }
            catch (Exception exception)
            {
                LogFailure(exception);
                return 0f;
            }
        }

        public void Draw(Rect rect)
        {
            try
            {
                Refresh();
                Layout(Mathf.Max(1f, rect.width));
                foreach (Rect header in tableHeaders)
                    Widgets.DrawBoxSolid(Offset(header, rect), new Color(0.18f, 0.2f, 0.22f, 0.9f));
                foreach (Rect row in tableRows) Widgets.DrawBox(Offset(row, rect));
                string clicked = null;
                foreach (var item in layout)
                {
                    Rect target = Offset(item.Rect, rect);
                    if (item.Rich) Widgets.Label(target, item.Text);
                    else
                    {
                        Color previous = GUI.color;
                        if (item.Link != null) GUI.color = new Color(0.45f, 0.75f, 1f);
                        GUI.Label(target, item.Text, plainStyle);
                        GUI.color = previous;
                        if (item.Link != null)
                        {
                            TooltipHandler.TipRegion(target, item.Link);
                            if (Widgets.ButtonInvisible(target)) clicked = item.Link;
                        }
                    }
                }
                if (clicked != null) OpenLink?.Invoke(clicked);
            }
            catch (Exception exception)
            {
                LogFailure(exception);
            }
        }

        private void Refresh()
        {
            string source = getMarkdown() ?? string.Empty;
            if (blocks != null
                && string.Equals(cachedSource, source, StringComparison.Ordinal))
            {
                return;
            }

            cachedSource = source;
            blocks = DocumentationBlocks.Parse(source);
            layoutWidth = -1f;
            Failed = false;
        }

        private static Rect Offset(Rect item, Rect parent)
        {
            return new Rect(item.x + parent.x, item.y + parent.y, item.width, item.height);
        }

        private void Layout(float width)
        {
            if (layoutWidth == width) return;
            if (plainStyle == null) plainStyle = new GUIStyle(Text.CurFontStyle) { richText = false, wordWrap = false };
            layout.Clear(); tableRows.Clear(); tableHeaders.Clear();
            float y = 0f;
            foreach (var block in blocks)
            {
                if (block.Cells != null)
                {
                    float cellWidth = width / block.Cells.Length;
                    float height = 24f;
                    for (int i = 0; i < block.Cells.Length; i++)
                        height = Mathf.Max(height, AddText(block.Cells[i] ?? string.Empty,
                            i * cellWidth + 6f, y + 5f, Mathf.Max(1f, cellWidth - 12f), false) + 10f);
                    for (int i = 0; i < block.Cells.Length; i++)
                        tableRows.Add(new Rect(i * cellWidth, y, cellWidth, height));
                    if (block.Header) tableHeaders.Add(new Rect(0f, y, width, height));
                    y += height;
                }
                else y += AddText(block.Text, 0f, y, width, block.Code) + 3f;
            }
            layoutHeight = y;
            layoutWidth = width;
        }

        private float AddText(string source, float left, float top, float width, bool code)
        {
            if (code || !DocumentationBlocks.Link.IsMatch(source))
            {
                string text = ArtiDocumentationMarkup.Render(source, ParseMarkdown, intelligence);
                float height = Mathf.Max(Text.LineHeight, Text.CalcHeight(text, width));
                layout.Add(new DrawItem { Rect = new Rect(left, top, width, height), Text = text, Rich = true });
                return height;
            }
            float x = 0f, y = 0f;
            int cursor = 0;
            foreach (Match link in DocumentationBlocks.Link.Matches(source))
            {
                AddRun(source.Substring(cursor, link.Index - cursor), null, left, top, width, ref x, ref y);
                AddRun(link.Groups[1].Value, link.Groups[2].Value, left, top, width, ref x, ref y);
                cursor = link.Index + link.Length;
            }
            AddRun(source.Substring(cursor), null, left, top, width, ref x, ref y);
            return y + Text.LineHeight;
        }

        private void AddRun(string markdown, string link, float left, float top, float width, ref float x, ref float y)
        {
            string text = Regex.Replace(ParseMarkdown(markdown), @"</?(?:b|i|color(?:=[^>]+)?)>", string.Empty);
            var elements = StringInfo.GetTextElementEnumerator(text);
            string run = string.Empty;
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                if (element == "\n" || x + plainStyle.CalcSize(new GUIContent(run + element)).x > width)
                {
                    AppendRun(run, link, left, top, ref x, y);
                    run = string.Empty;
                    if (x > 0f || element == "\n") { x = 0f; y += Text.LineHeight; }
                    if (element == "\n") continue;
                }
                run += element;
            }
            AppendRun(run, link, left, top, ref x, y);
        }

        private void AppendRun(string text, string link, float left, float top, ref float x, float y)
        {
            if (text.Length == 0) return;
            float width = plainStyle.CalcSize(new GUIContent(text)).x;
            layout.Add(new DrawItem { Rect = new Rect(left + x, top + y, width, Text.LineHeight), Text = text, Link = link });
            x += width;
        }

        private string ParseMarkdown(string source)
        {
            object value = parseMethod.Invoke(null, new object[] { source });
            return value as string ?? Convert.ToString(value) ?? string.Empty;
        }

        private void LogFailure(Exception exception)
        {
            if (failureLogged)
            {
                return;
            }

            failureLogged = true;
            Failed = true;
            Log.Warning(
                "[Advanced RimTalk] IrisMenus Markdown rendering failed: "
                + exception);
        }
    }
}
