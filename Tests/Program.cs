using System;
using System.Collections.Generic;
using AdvancedRimTalk.Prompt;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class Program
    {
        private static void Main()
        {
            GlobalRuntimeProperties.Run();
            GlobalDefinitionProperties.Run();
            PreviewSessionPatchChecks.Run();
            MixedTemplateChecks.Run();
            ArtiDocumentRenderChecks.Run();
            PromptSettingsChecks.Run();
            MemoryLayerTransferChecks.Run();
            MemoryPreviewPolicyChecks.Run();
            PromptPreviewSessionChecks.Run();
            TakeoverScopeChecks.Run();
            PromptPreviewChecks.Run();
            PromptCharacterBudgetChecks.Run();
            MemoryApiMethodResolverChecks.Run();
            ArtiChecks.Run();
            ArtiEditorTextChecks.Run();
            ArtiExecutorChecks.Run();
            ArtiPawnInfoChecks.Run();
            CheckMultilineAndMultipleExpressions();
            CheckCommonFunctions();
            CheckNativeScribanAndProtectedSlots();
            CheckInvalidExpressionIsVisible();
            CheckSystemInstructionFormats();
            TakeoverPromptImportConverterChecks.Run();
            Console.WriteLine("Prompt checks passed.");
        }

        private static void CheckMultilineAndMultipleExpressions()
        {
            PromptSnapshot snapshot = new PromptSnapshot();
            SequenceRandom random = new SequenceRandom(new[] { 4, 4 }, new[] { 0.25 });
            string template = "A={{ art.random_int(\n  1,\n  6\n) }} B={{ art.random_float(0, 1) }} C={{ art.random_int 2 3 }}";
            string expanded = PromptTemplateExpander.ExpandToText(template, snapshot, random);

            AssertEqual("A=4 B=0.25 C=4", expanded, "multiline and whitespace calls");
        }

        private static void CheckCommonFunctions()
        {
            PromptSnapshot snapshot = new PromptSnapshot
            {
                PawnName = "Ada",
                Topic = string.Empty
            };
            SequenceRandom random = new SequenceRandom(new[] { 1 }, new double[0]);
            string template = "{{ art.choose(\"calm\", \"urgent\") }}|{{ art.default(art.topic, \"fallback\") }}|{{ art.join(\", \", \"name\", art.pawn_name) }}|{{ art.coalesce(null, \"\", \"ready\") }}";

            string expanded = PromptTemplateExpander.ExpandToText(template, snapshot, random);
            AssertEqual("urgent|fallback|name, Ada|ready", expanded, "common functions");
        }

        private static void CheckNativeScribanAndProtectedSlots()
        {
            PromptSnapshot snapshot = new PromptSnapshot
            {
                Prompt = "literal {{ native_value }}"
            };
            string native = "{{ if something }}native{{ end }}";
            Assert(!PromptTemplateExpander.HasAdvancedExpressions(native), "native templates skip snapshot creation");
            AssertEqual(native, PromptTemplateExpander.ExpandToText(native, snapshot), "native Scriban remains untouched");

            string protectedValue = PromptTemplateExpander.ExpandToText("{{ art.prompt }}", snapshot);
            AssertEqual("literal {{ native_value }}", protectedValue, "inserted braces are restored literally");
        }

        private static void CheckInvalidExpressionIsVisible()
        {
            PromptExpansionResult result = PromptTemplateExpander.Expand("before {{ art.unknown }} after", new PromptSnapshot());
            Assert(result.Errors.Count == 1, "invalid placeholders are recorded");
            string expanded = result.Restore(result.Template);
            Assert(expanded.Contains("AdvancedRimTalk placeholder error"), "invalid placeholders remain visible");
        }

        private static void CheckSystemInstructionFormats()
        {
            SystemInstructionDocument document = new SystemInstructionDocument
            {
                Identity = "A <RimWorld> assistant & guide",
                Rules = "Follow the output contract.",
                OutputContract = "Return one response."
            };

            string xml = SystemInstructionRenderer.Render(document, SystemInstructionFormat.Xml);
            Assert(xml.Contains("<system_instruction>"), "XML root is present");
            Assert(xml.Contains("&lt;RimWorld&gt; assistant &amp; guide"), "XML content is escaped");
            Assert(!xml.Contains("<current_state>"), "empty XML sections are omitted");

            string markdown = SystemInstructionRenderer.Render(document, SystemInstructionFormat.Markdown);
            Assert(markdown.Contains("# System Instruction"), "Markdown title is present");
            Assert(markdown.Contains("## Output Contract"), "Markdown sections are present");
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
                throw new InvalidOperationException("Failed: " + name + ". Expected '" + expected + "', got '" + actual + "'.");
            }
        }

        private sealed class SequenceRandom : IPromptRandom
        {
            private readonly Queue<int> _integers;
            private readonly Queue<double> _doubles;

            public SequenceRandom(IEnumerable<int> integers, IEnumerable<double> doubles)
            {
                _integers = new Queue<int>(integers);
                _doubles = new Queue<double>(doubles);
            }

            public int NextInt(int minInclusive, int maxInclusive)
            {
                if (_integers.Count == 0)
                {
                    throw new InvalidOperationException("No deterministic integer available.");
                }

                return _integers.Dequeue();
            }

            public double NextDouble(double minInclusive, double maxInclusive)
            {
                if (_doubles.Count == 0)
                {
                    throw new InvalidOperationException("No deterministic double available.");
                }

                return _doubles.Dequeue();
            }
        }
    }
}
