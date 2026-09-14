using System;
using System.Linq;
using AdvancedRimTalk.Arti;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiChecks
    {
        public static void Run()
        {
            ParseDesignedSyntax();
            AnalyzeScopeAndControlFlow();
            AnalyzeModuleAvailability();
            AnalyzeRootOnlyGroupUse();
            AnalyzeAssignmentTargets();
            AnalyzeMutableLet();
            AnalyzeModuleAliases();
            ParsePromptDocumentCodeBlocks();
            AnalyzeRimTalkSymbols();
            CheckDiagnosticCatalog();
            PreserveSourceDiagnostics();
        }

        private static void ParseDesignedSyntax()
        {
            string source = @"
group use core
use optional cj.rimtalk.expandmemory as em

fn number_plus_one(n) {
    n += 1
    return n
}

let result = number_plus_one(1)
if em.active
{
    core.emit(""memory available"", newline: true)
}
else
{
    core.emit(""no memory"")
}

for item in [""a"", ""b""] {
    core.emit(item)
}

core.log(result)

while false {
    break
}
";

            ArtiParseResult parsed = new ArtiParser().Parse(source);
            Assert(!parsed.HasErrors, "designed syntax parses without errors: " + string.Join(" | ", parsed.Diagnostics.Select(diagnostic => diagnostic.ToString())));
            Assert(parsed.Program.Statements.OfType<ArtiFunctionDeclarationStatement>().Single().Parameters.Single() == "n", "function parameters are captured");
            Assert(parsed.Program.Statements.OfType<ArtiIfStatement>().Single().Branches.Count == 1, "if branch is captured");
            Assert(parsed.Program.Statements.OfType<ArtiForStatement>().Single().VariableName == "item", "for variable is captured");
            Assert(parsed.Program.Statements.OfType<ArtiWhileStatement>().Any(), "while statement is captured");
            Assert(parsed.Program.Statements.OfType<ArtiUseStatement>().Count() == 2, "group and optional imports are captured");
        }

        private static void AnalyzeScopeAndControlFlow()
        {
            string source = @"
return 1
break
let value = 1
let value = 2
const fixed_value = 3
fixed_value = 4
unknown = 5
";
            ArtiParseResult parsed = new ArtiParser().Parse(source);
            ArtiAnalysisResult analysis = new ArtiAnalyzer().Analyze(parsed.Program);

            Assert(analysis.HasErrors, "invalid scope and control flow are rejected");
            Assert(analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3004"), "top-level return is rejected");
            Assert(analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3005"), "top-level break is rejected");
            Assert(analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3001"), "duplicate declaration is rejected");
            Assert(analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3008"), "constant assignment is rejected");
            Assert(analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3007"), "assignment to unknown name is rejected");
        }

        private static void PreserveSourceDiagnostics()
        {
            string source = "fn bad(value) {\n    let value = 1\n";
            ArtiParseResult parsed = new ArtiParser().Parse(source);
            Assert(parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "ART2008"), "missing brace has a parser diagnostic");
            ArtiDiagnostic diagnosticWithLocation = parsed.Diagnostics.First(diagnostic => diagnostic.Code == "ART2008");
            Assert(diagnosticWithLocation.Span.Line >= 2, "diagnostics preserve source line");

            ArtiParseResult warning = new ArtiParser().Parse("const use core\n");
            Assert(warning.Diagnostics.Any(diagnostic => diagnostic.Code == "ART2003"), "draft const use is marked as a warning");
        }

        private static void AnalyzeModuleAvailability()
        {
            ArtiParseResult parsed = new ArtiParser().Parse("use missing.package as missing\nuse optional optional.package as optional\n");
            ArtiAnalysisResult analysis = new ArtiAnalyzer(new TestModuleCatalog()).Analyze(parsed.Program);

            Assert(analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3011"), "missing required module is rejected");
            Assert(!analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3011" && diagnostic.Message.Contains("optional.package")), "missing optional module is allowed");

            ArtiParseResult reservedAlias = new ArtiParser().Parse("use another.package as core\n");
            ArtiAnalysisResult reservedAnalysis = new ArtiAnalyzer().Analyze(reservedAlias.Program);
            Assert(reservedAnalysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3013"), "module aliases cannot shadow builtins");
        }

        private static void AnalyzeRootOnlyGroupUse()
        {
            ArtiParseResult root = new ArtiParser().Parse("group use core\n");
            ArtiAnalysisResult rootAnalysis = new ArtiAnalyzer().Analyze(root.Program);
            Assert(!rootAnalysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3002"), "top-level group use is accepted");

            ArtiParseResult nested = new ArtiParser().Parse("if true {\n    group use core\n}\n");
            ArtiAnalysisResult nestedAnalysis = new ArtiAnalyzer().Analyze(nested.Program);
            Assert(nestedAnalysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3002"), "nested group use is rejected");
        }

        private static void AnalyzeAssignmentTargets()
        {
            ArtiParseResult parsed = new ArtiParser().Parse("let value = 1\nvalue.member = 2\ncore.emit(\"x\") = 3\n");
            ArtiAnalysisResult analysis = new ArtiAnalyzer().Analyze(parsed.Program);

            Assert(analysis.Diagnostics.Count(diagnostic => diagnostic.Code == "ART3014") == 2, "only simple declared names can be assigned");
        }

        private static void AnalyzeMutableLet()
        {
            ArtiParseResult parsed = new ArtiParser().Parse("let value = 1\nvalue = 2\n");
            ArtiAnalysisResult analysis = new ArtiAnalyzer().Analyze(parsed.Program);

            Assert(
                !analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3008"),
                "let bindings are mutable");
        }

        private static void AnalyzeModuleAliases()
        {
            ArtiParseResult reserved = new ArtiParser().Parse("use another.package as range\n");
            ArtiAnalysisResult reservedAnalysis = new ArtiAnalyzer().Analyze(reserved.Program);
            Assert(reservedAnalysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3013"), "range is reserved as a module alias");

            ArtiParseResult invalidDefaultAlias = new ArtiParser().Parse("use \"foo.invalid-name\"\n");
            ArtiAnalysisResult invalidDefaultAliasAnalysis = new ArtiAnalyzer().Analyze(invalidDefaultAlias.Program);
            Assert(invalidDefaultAliasAnalysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3015"), "string package ids with invalid default aliases require as");
        }

        private static void ParsePromptDocumentCodeBlocks()
        {
            string document = "native {{ pawn.name }}\n"
                + "{{ \"{{% not an Arti block %}}\" }}\n"
                + "{{ if true\n"
                + "{{% not an Arti block either %}}{{ end }}\n"
                + "```arti\n"
                + "{{% let ignored = 1 %}}\n"
                + "```\n"
                + "`{{% let also_ignored = 1 %}}`\n"
                + "``{{% let double_tick_ignored = 1 %}}``\n"
                + "before {{%\n"
                + "    let text = \"%}}\"\n"
                + "    // %}} in a comment is not the end\n"
                + "    core.emit(text)\n"
                + "%}} after {{% let second = 2 %}}";

            ArtiDocumentParseResult result = new ArtiDocumentParser().Parse(document);
            Assert(result.CodeBlocks.Count == 2, "only real Arti document blocks are extracted");
            Assert(!result.HasErrors, "document code blocks parse without errors");
            Assert(result.CodeBlocks[0].Body.Contains("core.emit(text)"), "code block body is preserved");
            Assert(result.CodeBlocks[0].ClosingSpan.HasValue, "closed code block keeps closing span");
            Assert(result.CodeBlocks[0].BodySpan.StartOffset == result.CodeBlocks[0].OpeningSpan.EndOffset, "body span points into the original document");
            Assert(result.CodeBlocks[0].ParseResult.Tokens.Any(token => token.Span.StartOffset == document.IndexOf("let text", StringComparison.Ordinal)), "token spans are rebased to the document");
            Assert(result.CodeBlocks[1].ParseResult.Program.Span.StartOffset == result.CodeBlocks[1].BodySpan.StartOffset, "program span is rebased to the document");

            ArtiDocumentParseResult unclosed = new ArtiDocumentParser().Parse("prefix\n{{%\nlet value = 1");
            Assert(unclosed.CodeBlocks.Count == 1, "unclosed code blocks still produce a parse result");
            Assert(unclosed.Diagnostics.Any(diagnostic => diagnostic.Code == "ART2020"), "unclosed code blocks are diagnosed");
        }

        private static void AnalyzeRimTalkSymbols()
        {
            ArtiSymbolCatalog symbols = new ArtiSymbolCatalog(new[] { "knowledge", "GetRole" });
            symbols.AddPath("pawn.memory");
            ArtiParseResult parsed = new ArtiParser().Parse(
                "let memories = pawn.memory\n"
                + "let weather_text = map.weather\n"
                + "let knowledge_text = knowledge\n"
                + "let role = GetRole(pawn)\n"
                + "let current_time = time\n"
                + "let current_hour = hour\n"
                + "let current_weather = weather\n"
                + "let stored = getvar(\"key\")\n"
                + "let missing = unregistered_memory\n");
            ArtiAnalysisResult analysis = new ArtiAnalyzer(symbolCatalog: symbols).Analyze(parsed.Program);

            Assert(symbols.ContainsPath("knowledge"), "context variables are recorded as global paths");
            Assert(symbols.ContainsPath("pawn.memory"), "pawn variables can be represented as member paths");
            Assert(!symbols.ContainsGlobal("memory"), "pawn variables are not incorrectly promoted to globals");
            Assert(analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3009" && diagnostic.Message.Contains("unregistered_memory")), "unregistered dynamic variables are rejected");
            Assert(!analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3009" && diagnostic.Message.Contains("knowledge")), "registered context variables are accepted");
            Assert(!analysis.Diagnostics.Any(diagnostic => diagnostic.Code == "ART3009" && diagnostic.Message.Contains("GetRole")), "RimTalk utility roots are accepted");
        }

        private static void CheckDiagnosticCatalog()
        {
            ArtiDiagnostic diagnostic = ArtiDiagnosticCatalog.Create(
                ArtiDiagnosticSeverity.Error,
                3002,
                ArtiSourceSpan.Empty);

            Assert(diagnostic.Code == "ART3002", "diagnostic codes are formatted consistently");
            Assert(diagnostic.MessageZhCn.Contains("Prompt"), "Chinese diagnostic text is available");
            Assert(diagnostic.MessageEn.Contains("Prompt group"), "English diagnostic text is available");
            Assert(diagnostic.GetMessage(ArtiDiagnosticLanguage.English) == diagnostic.MessageEn, "diagnostic language selection is explicit");
        }

        private sealed class TestModuleCatalog : IArtiModuleCatalog
        {
            public bool TryGetModule(string packageId, out ArtiModuleInfo module)
            {
                module = null;
                return false;
            }
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Failed: " + name);
            }
        }
    }
}
