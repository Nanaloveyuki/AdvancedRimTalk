using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AdvancedRimTalk.Documentation
{
    internal sealed class DocumentationBlock
    {
        public string Text;
        public string[] Cells;
        public bool Header;
        public bool Code;
    }

    internal static class DocumentationBlocks
    {
        internal static readonly Regex Link = new Regex(@"(?<!!)\[([^\]\n]+)\]\(([^\s)]+)\)", RegexOptions.Compiled);

        internal static List<DocumentationBlock> Parse(string source)
        {
            string[] lines = (source ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var blocks = new List<DocumentationBlock>();
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
                {
                    char marker = trimmed[0];
                    int length = 0;
                    while (length < trimmed.Length && trimmed[length] == marker) length++;
                    var code = new StringBuilder(lines[i]);
                    while (++i < lines.Length)
                    {
                        code.Append('\n').Append(lines[i]);
                        string closing = lines[i].Trim();
                        if (closing.Length >= length && closing.Trim(marker).Length == 0) break;
                    }
                    blocks.Add(new DocumentationBlock { Text = code.ToString(), Code = true });
                }
                else if (i + 1 < lines.Length && IsSeparator(lines[i + 1], out int columns)
                    && Cells(lines[i]).Length == columns)
                {
                    blocks.Add(new DocumentationBlock { Cells = Cells(lines[i]), Header = true });
                    i++;
                    while (i + 1 < lines.Length && lines[i + 1].Contains("|") && !string.IsNullOrWhiteSpace(lines[i + 1]))
                    {
                        string[] cells = Cells(lines[++i]);
                        Array.Resize(ref cells, columns);
                        blocks.Add(new DocumentationBlock { Cells = cells });
                    }
                }
                else blocks.Add(new DocumentationBlock { Text = lines[i] });
            }
            return blocks;
        }

        private static bool IsSeparator(string line, out int columns)
        {
            var cells = Cells(line);
            columns = cells.Length;
            if (!line.Contains("|") || columns == 0) return false;
            foreach (string cell in cells)
                if (!Regex.IsMatch(cell.Trim(), @"^:?-{3,}:?$")) return false;
            return true;
        }

        internal static string[] Cells(string line)
        {
            string text = line.Trim();
            var cells = new List<string>();
            var cell = new StringBuilder();
            int codeTicks = 0;
            for (int i = text.StartsWith("|") ? 1 : 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\\' && i + 1 < text.Length && text[i + 1] == '|')
                { cell.Append('|'); i++; continue; }
                if (c == '`')
                {
                    int start = i;
                    while (i + 1 < text.Length && text[i + 1] == '`') i++;
                    int count = i - start + 1;
                    if (codeTicks == 0) codeTicks = count;
                    else if (codeTicks == count) codeTicks = 0;
                    cell.Append('`', count);
                }
                else if (c == '|' && codeTicks == 0)
                { cells.Add(cell.ToString().Trim()); cell.Clear(); }
                else cell.Append(c);
            }
            if (cell.Length > 0 || !text.EndsWith("|")) cells.Add(cell.ToString().Trim());
            return cells.ToArray();
        }
    }
}
