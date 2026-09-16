using System;
using AdvancedRimTalk.UI;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiEditorTextChecks
    {
        public static void Run()
        {
            FindsSingleEdits();
            AppliesIndentation();
            DetectsProtectedText();
            InvalidatesCompletion();
        }

        private static void FindsSingleEdits()
        {
            int index;
            char value;
            Assert(
                ArtiEditorText.TryFindSingleInsertion("ab", "a(b", out index, out value)
                    && index == 1
                    && value == '(',
                "single insertion is located");
            foreach (string before in new[] { "a\n\nb", "{{%\n\n\n%}}", "aaaa", "    " })
            {
                for (int caret = 0; caret <= before.Length; caret++)
                {
                    foreach (char added in new[] { '\n', ' ', 'a', ')' })
                    {
                        string after = before.Insert(caret, added.ToString());
                        Assert(ArtiEditorText.TryFindSingleInsertion(before, after, out index, out value, caret)
                            && index == caret && value == added, "single insertion preserves actual caret");
                    }
                }
            }
            Assert(
                ArtiEditorText.TryFindSingleDeletion("a(b", "ab", out index, out value)
                    && index == 1
                    && value == '(',
                "single deletion is located");
        }

        private static void AppliesIndentation()
        {
            string source = "let value = {\n}";
            string indented = ArtiEditorText.ApplyAutoIndentAfterNewline(
                source,
                source.IndexOf('\n'));
            AssertEqual("let value = {\n    }", indented, "block indentation is inserted");
        }

        private static void InvalidatesCompletion()
        {
            Assert(ArtiEditorText.IsCompletionCurrent("true", 3, 3, 0, "tru"), "unchanged completion stays open");
            Assert(!ArtiEditorText.IsCompletionCurrent("true", 4, 4, 0, "tru"), "manual typing invalidates old prefix");
            Assert(!ArtiEditorText.IsCompletionCurrent("true", 2, 2, 0, "tru"), "caret movement invalidates popup");
            Assert(!ArtiEditorText.IsCompletionCurrent("true", 3, 0, 0, "tru"), "selection invalidates popup");
            Assert(!ArtiEditorText.IsCompletionCurrent("try", 3, 3, 0, "tru"), "replacement invalidates popup");
        }

        private static void DetectsProtectedText()
        {
            Assert(
                ArtiEditorText.IsInsideStringOrComment(
                    "let value = \"text",
                    16,
                    0),
                "unclosed strings are protected");
            Assert(
                !ArtiEditorText.IsInsideStringOrComment(
                    "prompt \"text\"\nlet value = ",
                    27,
                    15),
                "document text does not leak into a code block scan");
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Failed: " + name);
            }
        }

        private static void AssertEqual(string expected, string actual, string name)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Failed: " + name + ". Expected '" + expected + "', got '" + actual + "'.");
            }
        }
    }
}
