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
