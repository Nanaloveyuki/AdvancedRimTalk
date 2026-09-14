using System;

namespace AdvancedRimTalk.UI
{
    internal static class ArtiEditorText
    {
        public const string IndentUnit = "    ";

        public static string NormalizeLineEndings(string value)
        {
            return (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        }

        public static bool TryFindSingleInsertion(
            string before,
            string after,
            out int index,
            out char inserted)
        {
            before = before ?? string.Empty;
            after = after ?? string.Empty;
            index = -1;
            inserted = '\0';
            if (after.Length != before.Length + 1)
            {
                return false;
            }

            int prefix = 0;
            while (prefix < before.Length && before[prefix] == after[prefix])
            {
                prefix++;
            }

            int suffix = 0;
            while (suffix < before.Length - prefix
                && before[before.Length - 1 - suffix] == after[after.Length - 1 - suffix])
            {
                suffix++;
            }

            if (prefix + suffix != before.Length)
            {
                return false;
            }

            index = prefix;
            inserted = after[index];
            return true;
        }

        public static bool TryFindSingleDeletion(
            string before,
            string after,
            out int index,
            out char deleted)
        {
            before = before ?? string.Empty;
            after = after ?? string.Empty;
            index = -1;
            deleted = '\0';
            if (before.Length != after.Length + 1)
            {
                return false;
            }

            int prefix = 0;
            while (prefix < after.Length && before[prefix] == after[prefix])
            {
                prefix++;
            }

            int suffix = 0;
            while (suffix < after.Length - prefix
                && before[before.Length - 1 - suffix] == after[after.Length - 1 - suffix])
            {
                suffix++;
            }

            if (prefix + suffix != after.Length)
            {
                return false;
            }

            index = prefix;
            deleted = before[index];
            return true;
        }

        public static int GetLineStart(string source, int position)
        {
            source = source ?? string.Empty;
            int cursor = Math.Max(0, Math.Min(position, source.Length));
            while (cursor > 0 && source[cursor - 1] != '\n')
            {
                cursor--;
            }

            return cursor;
        }

        public static int GetLineEnd(string source, int position)
        {
            source = source ?? string.Empty;
            int cursor = Math.Max(0, Math.Min(position, source.Length));
            while (cursor < source.Length && source[cursor] != '\n')
            {
                cursor++;
            }

            return cursor;
        }

        public static int GetLineIndex(string source, int position)
        {
            source = source ?? string.Empty;
            int end = Math.Max(0, Math.Min(position, source.Length));
            int line = 0;
            for (int index = 0; index < end; index++)
            {
                if (source[index] == '\n')
                {
                    line++;
                }
            }

            return line;
        }

        public static int GetLineCount(string source)
        {
            source = source ?? string.Empty;
            int count = 1;
            for (int index = 0; index < source.Length; index++)
            {
                if (source[index] == '\n')
                {
                    count++;
                }
            }

            return count;
        }

        public static string GetLeadingWhitespace(string value)
        {
            value = value ?? string.Empty;
            int length = 0;
            while (length < value.Length && (value[length] == ' ' || value[length] == '\t'))
            {
                length++;
            }

            return value.Substring(0, length);
        }

        public static string ApplyAutoIndentAfterNewline(string source, int newlineIndex)
        {
            source = source ?? string.Empty;
            int newline = Math.Max(0, Math.Min(newlineIndex, source.Length - 1));
            int caret = newline + 1;
            int previousStart = GetLineStart(source, newline);
            string previousLine = source.Substring(previousStart, newline - previousStart);
            string indent = GetLeadingWhitespace(previousLine);
            string previousCode = previousLine.TrimEnd();
            if (previousCode.EndsWith("{", StringComparison.Ordinal))
            {
                indent += IndentUnit;
            }

            int currentEnd = GetLineEnd(source, caret);
            string currentRemainder = source.Substring(caret, currentEnd - caret);
            char first = FirstNonWhitespace(currentRemainder);
            if (first == '}'
                && previousCode.EndsWith("{", StringComparison.Ordinal))
            {
                return source.Insert(caret, indent);
            }

            if (first == '}' || first == ']' || first == ')')
            {
                indent = RemoveOneIndent(indent);
            }

            return source.Insert(caret, indent);
        }

        public static bool IsInsideStringOrComment(string source, int position)
        {
            return IsInsideStringOrComment(source, position, 0);
        }

        public static bool IsInsideStringOrComment(string source, int position, int scanStart)
        {
            source = source ?? string.Empty;
            int end = Math.Max(0, Math.Min(position, source.Length));
            int start = Math.Max(0, Math.Min(scanStart, end));
            char quote = '\0';
            bool escaped = false;
            bool lineComment = false;
            bool blockComment = false;
            for (int index = start; index < end; index++)
            {
                char current = source[index];
                if (lineComment)
                {
                    if (current == '\n')
                    {
                        lineComment = false;
                    }

                    continue;
                }

                if (blockComment)
                {
                    if (current == '*' && index + 1 < end && source[index + 1] == '/')
                    {
                        blockComment = false;
                        index++;
                    }

                    continue;
                }

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

                if (current == '/' && index + 1 < end && source[index + 1] == '/')
                {
                    lineComment = true;
                    index++;
                }
                else if (current == '/' && index + 1 < end && source[index + 1] == '*')
                {
                    blockComment = true;
                    index++;
                }
                else if (current == '"' || current == '\'')
                {
                    quote = current;
                }
            }

            return quote != '\0' || lineComment || blockComment;
        }

        public static bool IsIdentifierPart(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        public static char MatchingClose(char value)
        {
            switch (value)
            {
                case '(':
                    return ')';
                case '[':
                    return ']';
                case '{':
                    return '}';
                case '"':
                    return '"';
                case '\'':
                    return '\'';
                default:
                    return '\0';
            }
        }

        public static bool IsOpeningPair(char value)
        {
            return value == '('
                || value == '['
                || value == '{'
                || value == '"'
                || value == '\'';
        }

        public static bool IsClosingPair(char value)
        {
            return value == ')'
                || value == ']'
                || value == '}'
                || value == '"'
                || value == '\'';
        }

        public static bool IsMatchingPair(char opening, char closing)
        {
            return MatchingClose(opening) == closing;
        }

        public static string RemoveOneIndent(string value)
        {
            value = value ?? string.Empty;
            if (value.EndsWith(IndentUnit, StringComparison.Ordinal))
            {
                return value.Substring(0, value.Length - IndentUnit.Length);
            }

            if (value.EndsWith("\t", StringComparison.Ordinal))
            {
                return value.Substring(0, value.Length - 1);
            }

            return value.Length == 0 ? value : value.Substring(0, value.Length - 1);
        }

        private static char FirstNonWhitespace(string value)
        {
            value = value ?? string.Empty;
            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] != ' ' && value[index] != '\t')
                {
                    return value[index];
                }
            }

            return '\0';
        }
    }
}
