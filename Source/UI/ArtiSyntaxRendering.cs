using System;
using System.Collections.Generic;
using System.Text;
using AdvancedRimTalk.Arti;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal static class ArtiSyntaxRendering
    {
        internal const int ExtraVisibleLines = 16;
        private const float MinExtraHorizontalPixels = 64f;
        private static readonly Rect UnboundedVisibleRect = new Rect(0f, 0f, 1e7f, 1e7f);

        private static readonly Color MarkerColor = new Color(0.95f, 0.68f, 0.3f);
        private static readonly Color CommentColor = new Color(0.42f, 0.55f, 0.47f);
        private static readonly Color KeywordColor = new Color(0.83f, 0.62f, 0.96f);
        private static readonly Color StringColor = new Color(0.9f, 0.73f, 0.43f);
        private static readonly Color NumberColor = new Color(0.48f, 0.78f, 0.91f);
        private static readonly Color BooleanColor = new Color(0.82f, 0.58f, 0.94f);
        private static readonly Color BuiltinColor = new Color(0.38f, 0.77f, 0.84f);
        private static readonly Color IdentifierColor = new Color(0.84f, 0.87f, 0.92f);
        private static readonly Color OperatorColor = new Color(0.78f, 0.81f, 0.86f);
        private static readonly Color PunctuationColor = new Color(0.62f, 0.69f, 0.76f);
        private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f);

        public static GUIStyle CreateInputStyle()
        {
            GUIStyle style = new GUIStyle(Text.CurTextAreaStyle);
            style.alignment = TextAnchor.UpperLeft;
            style.wordWrap = false;
            style.richText = false;
            RemoveBackgrounds(style);
            SetTextColors(style, new Color(1f, 1f, 1f, 0.01f));
            return style;
        }

        public static GUIStyle CreateSyntaxStyle(GUIStyle inputStyle)
        {
            GUIStyle style = new GUIStyle(inputStyle);
            style.padding = new RectOffset(0, 0, 0, 0);
            style.contentOffset = Vector2.zero;
            style.richText = false;
            SetTextColors(style, Color.white);
            return style;
        }

        public static void DrawSyntax(Rect rect, string source, ArtiEditorAnalysis analysis,
            GUIStyle style, float lineAdvance)
        {
            DrawSyntax(rect, source, analysis, style, lineAdvance, UnboundedVisibleRect);
        }

        public static void DrawSyntax(Rect rect, string source, ArtiEditorAnalysis analysis,
            GUIStyle style, float lineAdvance, Rect visibleRect)
        {
            source = ArtiEditorText.NormalizeLineEndings(source);
            if (style == null || lineAdvance <= 0f || source.Length == 0)
            {
                return;
            }

            GetVisibleYRange(visibleRect, lineAdvance, out float yMin, out float yMax);
            GetVisibleXRange(visibleRect, out float xMin, out float xMax);

            IList<ArtiHighlightSpan> highlights = analysis == null ? null : analysis.Highlights;
            int highlightCount = highlights == null ? 0 : highlights.Count;
            int highlightIndex = 0;
            int lineStart = 0;
            float y = rect.y;
            while (lineStart < source.Length && y + lineAdvance <= yMin)
            {
                int skippedEnd = ArtiEditorText.GetLineEnd(source, lineStart);
                highlightIndex = AdvanceHighlightIndex(
                    highlights, highlightIndex, highlightCount, skippedEnd);
                lineStart = skippedEnd + 1;
                y += lineAdvance;
            }

            while (lineStart < source.Length && y < yMax)
            {
                int lineEnd = ArtiEditorText.GetLineEnd(source, lineStart);
                highlightIndex = DrawSyntaxLine(
                    rect.x,
                    y,
                    source,
                    lineStart,
                    lineEnd,
                    highlights,
                    highlightIndex,
                    highlightCount,
                    style,
                    lineAdvance,
                    xMin,
                    xMax);
                if (lineEnd >= source.Length)
                {
                    break;
                }

                lineStart = lineEnd + 1;
                y += lineAdvance;
            }
        }

        internal static void GetVisibleLineRange(
            float originY,
            float lineAdvance,
            int lineCount,
            Rect visibleRect,
            out int firstLine,
            out int lastExclusive)
        {
            if (lineAdvance <= 0f || lineCount <= 0)
            {
                firstLine = 0;
                lastExclusive = 0;
                return;
            }

            GetVisibleYRange(visibleRect, lineAdvance, out float yMin, out float yMax);
            int first = (int)Math.Floor((yMin - originY) / lineAdvance);
            int last = (int)Math.Ceiling((yMax - originY) / lineAdvance);
            if (first < 0)
            {
                first = 0;
            }

            if (last > lineCount)
            {
                last = lineCount;
            }

            if (first > last)
            {
                first = last;
            }

            firstLine = first;
            lastExclusive = last;
        }

        private static int DrawSyntaxLine(
            float x,
            float y,
            string source,
            int lineStart,
            int lineEnd,
            IList<ArtiHighlightSpan> highlights,
            int highlightIndex,
            int highlightCount,
            GUIStyle style,
            float lineAdvance,
            float xMin,
            float xMax)
        {
            int position = lineStart;
            int index = highlightIndex;
            if (highlights != null)
            {
                for (; index < highlightCount; index++)
                {
                    ArtiHighlightSpan highlight = highlights[index];
                    if (highlight == null || highlight.EndOffset <= position)
                    {
                        continue;
                    }

                    if (highlight.StartOffset >= lineEnd)
                    {
                        break;
                    }

                    int start = Math.Max(position, highlight.StartOffset);
                    int end = Math.Min(lineEnd, highlight.EndOffset);
                    if (end <= start)
                    {
                        continue;
                    }

                    x += DrawRun(x, y, source, position, start, IdentifierColor, style, lineAdvance, xMin, xMax);
                    x += DrawRun(x, y, source, start, end, GetSyntaxColor(highlight.Role), style, lineAdvance, xMin, xMax);
                    position = end;
                }
            }

            DrawRun(x, y, source, position, lineEnd, IdentifierColor, style, lineAdvance, xMin, xMax);
            return AdvanceHighlightIndex(highlights, highlightIndex, highlightCount, lineEnd);
        }

        private static float DrawRun(float x, float y, string source, int start, int end,
            Color color, GUIStyle style, float lineAdvance, float xMin, float xMax)
        {
            if (end <= start)
            {
                return 0f;
            }

            string value = source.Substring(start, end - start);
            float width = style.CalcSize(new GUIContent(value)).x;
            if (x + width < xMin || x > xMax)
            {
                return width;
            }

            Color previous = GUI.color;
            try
            {
                GUI.color = color;
                GUI.Label(new Rect(x, y, Mathf.Max(1f, width + 2f), lineAdvance), value, style);
            }
            finally
            {
                GUI.color = previous;
            }

            return width;
        }

        private static void GetVisibleYRange(Rect visibleRect, float lineAdvance, out float yMin, out float yMax)
        {
            float extra = lineAdvance * ExtraVisibleLines;
            if (visibleRect.height > extra)
            {
                extra = visibleRect.height;
            }

            yMin = visibleRect.y - extra;
            yMax = visibleRect.y + visibleRect.height + extra;
        }

        private static void GetVisibleXRange(Rect visibleRect, out float xMin, out float xMax)
        {
            float extra = visibleRect.width;
            if (extra < MinExtraHorizontalPixels)
            {
                extra = MinExtraHorizontalPixels;
            }

            xMin = visibleRect.x - extra;
            xMax = visibleRect.x + visibleRect.width + extra;
        }

        private static int AdvanceHighlightIndex(
            IList<ArtiHighlightSpan> highlights,
            int highlightIndex,
            int highlightCount,
            int lineEnd)
        {
            int next = highlightIndex;
            while (next < highlightCount)
            {
                ArtiHighlightSpan span = highlights[next];
                if (span != null && span.EndOffset > lineEnd)
                {
                    break;
                }

                next++;
            }

            return next;
        }

        public static string ToRichText(string source, ArtiEditorAnalysis analysis)
        {
            source = ArtiEditorText.NormalizeLineEndings(source);
            if (source.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(source.Length + 64);
            int cursor = 0;
            if (analysis != null && analysis.Highlights != null)
            {
                foreach (ArtiHighlightSpan highlight in analysis.Highlights)
                {
                    if (highlight == null)
                    {
                        continue;
                    }

                    int start = Math.Max(0, Math.Min(source.Length, highlight.StartOffset));
                    int end = Math.Max(start, Math.Min(source.Length, highlight.EndOffset));
                    if (end <= cursor)
                    {
                        continue;
                    }

                    if (start > cursor)
                    {
                        AppendColored(
                            builder,
                            source.Substring(cursor, start - cursor),
                            IdentifierColor);
                    }

                    int visibleStart = Math.Max(cursor, start);
                    AppendColored(
                        builder,
                        source.Substring(visibleStart, end - visibleStart),
                        GetSyntaxColor(highlight.Role));
                    cursor = end;
                }
            }

            if (cursor < source.Length)
            {
                AppendColored(
                    builder,
                    source.Substring(cursor),
                    IdentifierColor);
            }

            return builder.ToString();
        }

        public static string ToRichText(string source, ArtiSyntaxRole role)
        {
            source = ArtiEditorText.NormalizeLineEndings(source);
            if (source.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(source.Length + 32);
            AppendColored(builder, source, GetSyntaxColor(role));
            return builder.ToString();
        }

        public static Color GetSyntaxColor(ArtiSyntaxRole role)
        {
            switch (role)
            {
                case ArtiSyntaxRole.Marker:
                    return MarkerColor;
                case ArtiSyntaxRole.Comment:
                    return CommentColor;
                case ArtiSyntaxRole.Keyword:
                    return KeywordColor;
                case ArtiSyntaxRole.String:
                    return StringColor;
                case ArtiSyntaxRole.Number:
                    return NumberColor;
                case ArtiSyntaxRole.Boolean:
                    return BooleanColor;
                case ArtiSyntaxRole.Builtin:
                    return BuiltinColor;
                case ArtiSyntaxRole.Operator:
                    return OperatorColor;
                case ArtiSyntaxRole.Punctuation:
                    return PunctuationColor;
                case ArtiSyntaxRole.Invalid:
                    return InvalidColor;
                default:
                    return IdentifierColor;
            }
        }

        private static void AppendColored(StringBuilder builder, string value, Color color)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(color));
            builder.Append('>');
            // IMGUI does not support noparse. Break literal tag openings with a
            // color boundary so source such as <b> stays visible without extra characters.
            string boundary = "</color><color=#" + ColorUtility.ToHtmlStringRGB(color) + ">";
            builder.Append(value.Replace("<", "<" + boundary));
            builder.Append("</color>");
        }

        private static void RemoveBackgrounds(GUIStyle style)
        {
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.onNormal.background = null;
            style.onHover.background = null;
            style.onActive.background = null;
            style.onFocused.background = null;
        }

        private static void SetTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }
    }
}
