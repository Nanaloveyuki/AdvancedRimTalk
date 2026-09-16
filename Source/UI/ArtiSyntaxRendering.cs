using System;
using System.Text;
using AdvancedRimTalk.Arti;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal static class ArtiSyntaxRendering
    {
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
            source = ArtiEditorText.NormalizeLineEndings(source);
            int lineStart = 0;
            float y = rect.y;
            while (lineStart < source.Length)
            {
                int lineEnd = ArtiEditorText.GetLineEnd(source, lineStart);
                int position = lineStart;
                if (analysis != null && analysis.Highlights != null)
                {
                    foreach (ArtiHighlightSpan highlight in analysis.Highlights)
                    {
                        if (highlight == null || highlight.EndOffset <= position) continue;
                        if (highlight.StartOffset >= lineEnd) break;
                        int start = Math.Max(position, highlight.StartOffset);
                        int end = Math.Min(lineEnd, highlight.EndOffset);
                        if (end <= start) continue;
                        DrawRun(rect.x, y, source, lineStart, position, start, IdentifierColor, style, lineAdvance);
                        DrawRun(rect.x, y, source, lineStart, start, end, GetSyntaxColor(highlight.Role), style, lineAdvance);
                        position = end;
                    }
                }
                DrawRun(rect.x, y, source, lineStart, position, lineEnd, IdentifierColor, style, lineAdvance);
                lineStart = lineEnd + 1;
                y += lineAdvance;
            }
        }

        private static void DrawRun(float x, float y, string source, int lineStart, int start, int end,
            Color color, GUIStyle style, float lineAdvance)
        {
            if (end <= start) return;
            string value = source.Substring(start, end - start);
            float offset = style.CalcSize(new GUIContent(source.Substring(lineStart, start - lineStart))).x;
            float width = style.CalcSize(new GUIContent(value)).x;
            Color previous = GUI.color;
            try
            {
                GUI.color = color;
                GUI.Label(new Rect(x + offset, y, Mathf.Max(1f, width + 2f), lineAdvance), value, style);
            }
            finally
            {
                GUI.color = previous;
            }
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
            builder.Append("<noparse>");
            builder.Append(value);
            builder.Append("</noparse>");
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
