using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedRimTalk.UI;
using UnityEngine;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiSyntaxRenderingChecks
    {
        internal static void Run()
        {
            var style = ArtiSyntaxRendering.CreateSyntaxStyle(ArtiSyntaxRendering.CreateInputStyle());
            if (style.richText) throw new Exception("Prompt syntax must be drawn as literal text.");
            foreach (string source in new[] { "plain prompt", "<b>literal</b> & <noparse>", "{{%\ncore.emit(\"<color=red>x</color>\")\n%}}", "a\n\nb", "", "中文 < 3" })
            {
                foreach (bool highlighted in new[] { false, true })
                {
                    GUI.Runs.Clear();
                    var analysis = highlighted ? new ArtiEditorAnalysis() : null;
                    if (analysis != null)
                    {
                        for (int i = 0; i < source.Length; i += 3)
                            analysis.Highlights.Add(new ArtiHighlightSpan(i, Math.Min(2, source.Length - i), ArtiSyntaxRole.String));
                    }
                    ArtiSyntaxRendering.DrawSyntax(new Rect(0, 0, 800, 600), source, analysis, style, 20);
                    if (string.Concat(GUI.Runs.Select(run => run.Text)) != source.Replace("\n", ""))
                        throw new Exception("Syntax drawing inserted, dropped, or escaped prompt characters.");
                    foreach (var run in GUI.Runs)
                    {
                        int line = (int)(run.Rect.y / 20);
                        string text = source.Split('\n')[line];
                        if (text.Substring((int)run.Rect.x, run.Text.Length) != run.Text)
                            throw new Exception("Syntax drawing is misaligned with the source.");
                    }
                }
            }
        }
    }
}

// Host doubles record draw calls; real Unity font metrics and IMGUI need in-game validation.
namespace UnityEngine
{
    internal struct Color
    {
        public Color(float r, float g, float b, float a = 1) { }
        public static Color white => new Color(1, 1, 1);
    }
    internal enum TextAnchor { UpperLeft }
    internal sealed class RectOffset { public RectOffset(int left, int right, int top, int bottom) { } }
    internal sealed class GUIStyleState { public object background; public Color textColor; }
    internal sealed class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle source) { richText = source.richText; }
        public bool richText, wordWrap;
        public TextAnchor alignment;
        public RectOffset padding;
        public Vector2 contentOffset;
        public GUIStyleState normal = new GUIStyleState(), hover = new GUIStyleState(), active = new GUIStyleState(),
            focused = new GUIStyleState(), onNormal = new GUIStyleState(), onHover = new GUIStyleState(),
            onActive = new GUIStyleState(), onFocused = new GUIStyleState();
        public Vector2 CalcSize(GUIContent content) => new Vector2(content.Text.Length, 20);
    }
    internal sealed class GUIContent
    {
        public readonly string Text;
        public GUIContent(string text) { Text = text; }
    }
    internal static class GUI
    {
        public static Color color;
        public static readonly List<(Rect Rect, string Text)> Runs = new List<(Rect, string)>();
        public static void Label(Rect rect, string text, GUIStyle style)
        {
            if (style.richText) throw new Exception("Literal prompt rendered using rich text.");
            Runs.Add((rect, text));
        }
    }
    internal static class ColorUtility { public static string ToHtmlStringRGB(Color color) => "FFFFFF"; }
}
namespace Verse
{
    internal static class Text { public static GUIStyle CurTextAreaStyle = new GUIStyle(); }
}
namespace AdvancedRimTalk.UI
{
    internal enum ArtiSyntaxRole { Plain, Marker, Comment, Keyword, String, Number, Boolean, Builtin, Identifier, Operator, Punctuation, Invalid }
    internal sealed class ArtiHighlightSpan
    {
        public int StartOffset { get; }
        public int EndOffset { get; }
        public ArtiSyntaxRole Role { get; }
        public ArtiHighlightSpan(int start, int length, ArtiSyntaxRole role)
        { StartOffset = start; EndOffset = start + length; Role = role; }
    }
    internal sealed class ArtiEditorAnalysis
    {
        public List<ArtiHighlightSpan> Highlights = new List<ArtiHighlightSpan>();
    }
}
