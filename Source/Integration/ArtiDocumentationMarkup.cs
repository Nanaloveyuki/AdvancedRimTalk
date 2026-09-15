using System;
using System.Collections.Generic;
using System.Text;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.UI;

namespace AdvancedRimTalk.Integration
{
    internal static class ArtiDocumentationMarkup
    {
        public static string Render(
            string markdown,
            Func<string, string> parseMarkdown,
            ArtiEditorIntelligence intelligence)
        {
            string source = ArtiEditorText.NormalizeLineEndings(markdown);
            string[] lines = source.Split(new[] { '\n' }, StringSplitOptions.None);
            StringBuilder output = new StringBuilder(source.Length + 128);
            StringBuilder normal = new StringBuilder();
            int lineIndex = 0;

            while (lineIndex < lines.Length)
            {
                Fence fence;
                if (!TryGetFence(lines[lineIndex], out fence))
                {
                    if (normal.Length > 0)
                    {
                        normal.Append('\n');
                    }

                    normal.Append(lines[lineIndex]);
                    lineIndex++;
                    continue;
                }

                AppendNormal(output, normal, parseMarkdown, intelligence);
                lineIndex++;

                List<string> codeLines = new List<string>();
                bool closed = false;
                while (lineIndex < lines.Length)
                {
                    if (IsClosingFence(lines[lineIndex], fence))
                    {
                        closed = true;
                        lineIndex++;
                        break;
                    }

                    codeLines.Add(lines[lineIndex]);
                    lineIndex++;
                }

                string code = string.Join("\n", codeLines.ToArray());
                ArtiEditorAnalysis analysis = IsArtiFence(fence.Info)
                    ? intelligence.AnalyzeCode(code)
                    : intelligence.AnalyzeDocument(code);
                if (output.Length > 0 && output[output.Length - 1] != '\n')
                {
                    output.Append('\n');
                }

                output.Append(ArtiSyntaxRendering.ToRichText(code, analysis));
                if (closed && lineIndex < lines.Length)
                {
                    output.Append('\n');
                }
            }

            AppendNormal(output, normal, parseMarkdown, intelligence);
            return output.ToString();
        }

        private static void AppendNormal(
            StringBuilder output,
            StringBuilder normal,
            Func<string, string> parseMarkdown,
            ArtiEditorIntelligence intelligence)
        {
            if (normal.Length == 0)
            {
                return;
            }

            string source = normal.ToString();
            ArtiDocumentParseResult parsed = new ArtiDocumentParser().Parse(source);
            if (parsed == null
                || parsed.CodeBlocks == null
                || parsed.CodeBlocks.Count == 0)
            {
                AppendMarkdown(output, source, parseMarkdown);
                normal.Length = 0;
                return;
            }

            int cursor = 0;
            foreach (ArtiCodeBlock block in parsed.CodeBlocks)
            {
                if (block == null)
                {
                    continue;
                }

                int start = Math.Max(0, Math.Min(source.Length, block.Span.StartOffset));
                int end = Math.Max(start, Math.Min(source.Length, block.Span.EndOffset));
                if (start > cursor)
                {
                    AppendMarkdown(
                        output,
                        source.Substring(cursor, start - cursor),
                        parseMarkdown);
                }

                output.Append(RenderArtiBlock(source, block, intelligence));
                cursor = Math.Max(cursor, end);
            }

            if (cursor < source.Length)
            {
                AppendMarkdown(
                    output,
                    source.Substring(cursor),
                    parseMarkdown);
            }

            normal.Length = 0;
        }

        private static void AppendMarkdown(
            StringBuilder output,
            string source,
            Func<string, string> parseMarkdown)
        {
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            string rendered = parseMarkdown == null ? source : parseMarkdown(source);
            output.Append(rendered ?? source);
        }

        private static string RenderArtiBlock(
            string source,
            ArtiCodeBlock block,
            ArtiEditorIntelligence intelligence)
        {
            StringBuilder rendered = new StringBuilder(block.Span.Length + 64);
            string opening = source.Substring(
                block.OpeningSpan.StartOffset,
                block.OpeningSpan.Length);
            rendered.Append(ArtiSyntaxRendering.ToRichText(
                opening,
                ArtiSyntaxRole.Marker));
            rendered.Append(ArtiSyntaxRendering.ToRichText(
                block.Body,
                intelligence == null ? null : intelligence.AnalyzeCode(block.Body)));
            if (block.ClosingSpan.HasValue)
            {
                ArtiSourceSpan closing = block.ClosingSpan.Value;
                rendered.Append(ArtiSyntaxRendering.ToRichText(
                    source.Substring(closing.StartOffset, closing.Length),
                    ArtiSyntaxRole.Marker));
            }

            return rendered.ToString();
        }

        private static bool IsArtiFence(string info)
        {
            if (string.IsNullOrWhiteSpace(info))
            {
                return false;
            }

            int separator = info.IndexOfAny(new[] { ' ', '\t' });
            string language = separator < 0 ? info : info.Substring(0, separator);
            return string.Equals(language, "arti", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetFence(string line, out Fence fence)
        {
            fence = null;
            line = line ?? string.Empty;
            int cursor = 0;
            while (cursor < line.Length
                && cursor < 4
                && (line[cursor] == ' ' || line[cursor] == '\t'))
            {
                cursor++;
            }

            if (cursor >= line.Length
                || (line[cursor] != '`' && line[cursor] != '~'))
            {
                return false;
            }

            char marker = line[cursor];
            int markerStart = cursor;
            while (cursor < line.Length && line[cursor] == marker)
            {
                cursor++;
            }

            int length = cursor - markerStart;
            if (length < 3)
            {
                return false;
            }

            fence = new Fence(marker, length, line.Substring(cursor).Trim());
            return true;
        }

        private static bool IsClosingFence(string line, Fence opening)
        {
            Fence closing;
            if (!TryGetFence(line, out closing)
                || closing.Marker != opening.Marker
                || closing.Length < opening.Length)
            {
                return false;
            }

            return string.IsNullOrEmpty(closing.Info);
        }

        private sealed class Fence
        {
            public Fence(char marker, int length, string info)
            {
                Marker = marker;
                Length = length;
                Info = info ?? string.Empty;
            }

            public char Marker { get; }
            public int Length { get; }
            public string Info { get; }
        }
    }
}
