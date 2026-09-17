using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedRimTalk.Arti
{
    public sealed class ArtiCodeBlock
    {
        public ArtiCodeBlock(
            string body,
            ArtiSourceSpan span,
            ArtiSourceSpan bodySpan,
            ArtiSourceSpan openingSpan,
            ArtiSourceSpan? closingSpan,
            ArtiParseResult parseResult)
        {
            Body = body ?? string.Empty;
            Span = span;
            BodySpan = bodySpan;
            OpeningSpan = openingSpan;
            ClosingSpan = closingSpan;
            ParseResult = parseResult;
        }

        public string Body { get; }
        public string Source { get { return Body; } }
        public ArtiSourceSpan Span { get; }
        public ArtiSourceSpan BodySpan { get; }
        public ArtiSourceSpan OpeningSpan { get; }
        public ArtiSourceSpan? ClosingSpan { get; }
        public ArtiParseResult ParseResult { get; }
        public bool HasErrors
        {
            get { return !ClosingSpan.HasValue || (ParseResult != null && ParseResult.HasErrors); }
        }
    }

    public sealed class ArtiDocumentParseResult
    {
        public ArtiDocumentParseResult(string source, IList<ArtiCodeBlock> codeBlocks, IList<ArtiDiagnostic> diagnostics)
        {
            Source = source ?? string.Empty;
            CodeBlocks = codeBlocks;
            Diagnostics = diagnostics;
        }

        public string Source { get; }
        public IList<ArtiCodeBlock> CodeBlocks { get; }
        public IList<ArtiDiagnostic> Diagnostics { get; }
        public bool HasErrors
        {
            get
            {
                return Diagnostics.Any(diagnostic => diagnostic.Severity == ArtiDiagnosticSeverity.Error);
            }
        }
    }

    public sealed class ArtiDocumentParser : ArtiDiagnosticReporter
    {
        public const string OpeningMarker = "{{%";
        public const string ClosingMarker = "%}}";

        public ArtiDocumentParseResult Parse(string source)
        {
            source = source ?? string.Empty;
            ClearDiagnostics();
            List<ArtiCodeBlock> blocks = new List<ArtiCodeBlock>();

            int position = 0;
            char fenceCharacter = '\0';
            int fenceLength = 0;
            while (position < source.Length)
            {
                int lineEnd = FindLineEnd(source, position);
                if (fenceCharacter != '\0')
                {
                    if (IsClosingFence(source, position, lineEnd, fenceCharacter, fenceLength))
                    {
                        fenceCharacter = '\0';
                        fenceLength = 0;
                    }

                    position = NextLine(source, lineEnd);
                    continue;
                }

                char openingFenceCharacter;
                int openingFenceLength;
                if (TryGetFence(source, position, lineEnd, out openingFenceCharacter, out openingFenceLength))
                {
                    fenceCharacter = openingFenceCharacter;
                    fenceLength = openingFenceLength;
                    position = NextLine(source, lineEnd);
                    continue;
                }

                int inlineCodeDelimiterLength = 0;
                int cursor = position;
                while (cursor < lineEnd)
                {
                    if (source[cursor] == '`' && !IsEscaped(source, cursor))
                    {
                        int delimiterLength = CountRepeated(source, cursor, '`');
                        if (inlineCodeDelimiterLength == 0)
                        {
                            inlineCodeDelimiterLength = delimiterLength;
                        }
                        else if (inlineCodeDelimiterLength == delimiterLength)
                        {
                            inlineCodeDelimiterLength = 0;
                        }

                        cursor += delimiterLength;
                        continue;
                    }

                    if (inlineCodeDelimiterLength == 0
                        && StartsWith(source, cursor, "{{")
                        && !StartsWith(source, cursor, OpeningMarker))
                    {
                        int nativeEnd = FindNativeScribanEnd(source, cursor + 2);
                        if (nativeEnd >= 0)
                        {
                            cursor = nativeEnd + 2;
                            if (cursor >= lineEnd)
                            {
                                position = cursor;
                                break;
                            }

                            continue;
                        }
                    }

                    if (inlineCodeDelimiterLength == 0 && StartsWith(source, cursor, OpeningMarker))
                    {
                        int bodyStart = cursor + OpeningMarker.Length;
                        int closingStart = FindClosingMarker(source, bodyStart);
                        bool hasClosingMarker = closingStart >= 0;
                        int bodyEnd = hasClosingMarker ? closingStart : source.Length;
                        int blockEnd = hasClosingMarker ? closingStart + ClosingMarker.Length : source.Length;
                        string body = source.Substring(bodyStart, bodyEnd - bodyStart);
                        ArtiSourceSpan bodySpan = CreateSpan(source, bodyStart, body.Length);
                        ArtiSourceSpan openingSpan = CreateSpan(source, cursor, OpeningMarker.Length);
                        ArtiSourceSpan blockSpan = CreateSpan(source, cursor, blockEnd - cursor);
                        ArtiSourceSpan? closingSpan = hasClosingMarker
                            ? new ArtiSourceSpan?(CreateSpan(source, closingStart, ClosingMarker.Length))
                            : null;
                        ArtiSourceLocation bodyLocation = ArtiSourceLocation.FromOffset(source, bodyStart);
                        ArtiParseResult parseResult = new ArtiParser().Parse(
                            body,
                            bodySpan.StartOffset,
                            bodyLocation.Line,
                            bodyLocation.Column);
                        blocks.Add(new ArtiCodeBlock(
                            body,
                            blockSpan,
                            bodySpan,
                            openingSpan,
                            closingSpan,
                            parseResult));

                        if (!hasClosingMarker)
                        {
                            ReportError(2020, blockSpan);
                            position = source.Length;
                            break;
                        }

                        cursor = blockEnd;
                        if (cursor >= lineEnd)
                        {
                            position = cursor;
                            break;
                        }

                        continue;
                    }

                    cursor++;
                }

                if (position == source.Length)
                {
                    break;
                }

                if (position > lineEnd)
                {
                    continue;
                }

                if (position == cursor && cursor >= lineEnd)
                {
                    position = NextLine(source, lineEnd);
                }
                else if (position < lineEnd)
                {
                    position = NextLine(source, lineEnd);
                }
            }

            List<ArtiDiagnostic> diagnostics = new List<ArtiDiagnostic>(ReportedDiagnostics);
            foreach (ArtiCodeBlock block in blocks)
            {
                if (block.ParseResult == null)
                {
                    continue;
                }

                foreach (ArtiDiagnostic diagnostic in block.ParseResult.Diagnostics)
                {
                    diagnostics.Add(diagnostic);
                }
            }

            return new ArtiDocumentParseResult(source, blocks, diagnostics);
        }

        private static int FindClosingMarker(string source, int start)
        {
            bool lineComment = false;
            for (int index = start; index < source.Length; index++)
            {
                char current = source[index];
                if (lineComment)
                {
                    if (current == '\r' || current == '\n')
                    {
                        lineComment = false;
                    }

                    continue;
                }

                if (current == 'f' && index + 1 < source.Length
                    && (source[index + 1] == '"' || source[index + 1] == '\''))
                {
                    index = ArtiLexer.SkipString(source, index) - 1;
                    continue;
                }

                if (current == '/' && index + 1 < source.Length && source[index + 1] == '/')
                {
                    lineComment = true;
                    index++;
                    continue;
                }

                if (current == '"' || current == '\'')
                {
                    index = ArtiLexer.SkipString(source, index) - 1;
                    continue;
                }

                if (StartsWith(source, index, ClosingMarker))
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindNativeScribanEnd(string source, int start)
        {
            char quote = '\0';
            bool escaped = false;
            for (int index = start; index + 1 < source.Length; index++)
            {
                char current = source[index];
                if (quote != '\0')
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == quote)
                    {
                        quote = '\0';
                    }

                    continue;
                }

                if (current == '"' || current == '\'')
                {
                    quote = current;
                    continue;
                }

                if (current == '}' && source[index + 1] == '}')
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool TryGetFence(string source, int lineStart, int lineEnd, out char fenceCharacter, out int fenceLength)
        {
            int cursor = lineStart;
            int indentation = 0;
            while (cursor < lineEnd && indentation < 4 && (source[cursor] == ' ' || source[cursor] == '\t'))
            {
                cursor++;
                indentation++;
            }

            fenceCharacter = cursor < lineEnd && (source[cursor] == '`' || source[cursor] == '~')
                ? source[cursor]
                : '\0';
            fenceLength = 0;
            if (fenceCharacter == '\0')
            {
                return false;
            }

            while (cursor < lineEnd && source[cursor] == fenceCharacter)
            {
                cursor++;
                fenceLength++;
            }

            return fenceLength >= 3;
        }

        private static bool IsClosingFence(string source, int lineStart, int lineEnd, char expectedCharacter, int expectedLength)
        {
            char actualCharacter;
            int actualLength;
            if (!TryGetFence(source, lineStart, lineEnd, out actualCharacter, out actualLength)
                || actualCharacter != expectedCharacter
                || actualLength < expectedLength)
            {
                return false;
            }

            int cursor = lineStart;
            int indentation = 0;
            while (cursor < lineEnd && indentation < 4 && (source[cursor] == ' ' || source[cursor] == '\t'))
            {
                cursor++;
                indentation++;
            }

            cursor += actualLength;
            while (cursor < lineEnd && (source[cursor] == ' ' || source[cursor] == '\t'))
            {
                cursor++;
            }

            return cursor == lineEnd;
        }

        private static bool IsEscaped(string source, int position)
        {
            int slashCount = 0;
            for (int index = position - 1; index >= 0 && source[index] == '\\'; index--)
            {
                slashCount++;
            }

            return (slashCount % 2) != 0;
        }

        private static int CountRepeated(string source, int position, char value)
        {
            int count = 0;
            while (position + count < source.Length && source[position + count] == value)
            {
                count++;
            }

            return count;
        }

        private static bool StartsWith(string source, int position, string value)
        {
            return position >= 0
                && position + value.Length <= source.Length
                && string.CompareOrdinal(source, position, value, 0, value.Length) == 0;
        }

        private static ArtiSourceSpan CreateSpan(string source, int start, int length)
        {
            ArtiSourceLocation location = ArtiSourceLocation.FromOffset(source, start);
            return new ArtiSourceSpan(start, length, location.Line, location.Column);
        }

        private static int FindLineEnd(string source, int start)
        {
            int cursor = start;
            while (cursor < source.Length && source[cursor] != '\r' && source[cursor] != '\n')
            {
                cursor++;
            }

            return cursor;
        }

        private static int NextLine(string source, int lineEnd)
        {
            if (lineEnd >= source.Length)
            {
                return source.Length;
            }

            if (source[lineEnd] == '\r' && lineEnd + 1 < source.Length && source[lineEnd + 1] == '\n')
            {
                return lineEnd + 2;
            }

            return lineEnd + 1;
        }

        private struct ArtiSourceLocation
        {
            public ArtiSourceLocation(int line, int column)
            {
                Line = line;
                Column = column;
            }

            public int Line { get; }
            public int Column { get; }

            public static ArtiSourceLocation FromOffset(string source, int offset)
            {
                int line = 1;
                int column = 1;
                int end = Math.Max(0, Math.Min(offset, source.Length));
                for (int index = 0; index < end; index++)
                {
                    if (source[index] == '\r')
                    {
                        if (index + 1 < end && source[index + 1] == '\n')
                        {
                            index++;
                        }

                        line++;
                        column = 1;
                    }
                    else if (source[index] == '\n')
                    {
                        line++;
                        column = 1;
                    }
                    else
                    {
                        column++;
                    }
                }

                return new ArtiSourceLocation(line, column);
            }
        }
    }
}
