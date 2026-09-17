using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Integration;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal enum ArtiSyntaxRole
    {
        Plain,
        Marker,
        Comment,
        Keyword,
        String,
        Number,
        Boolean,
        Builtin,
        Identifier,
        Operator,
        Punctuation,
        Invalid
    }

    internal sealed class ArtiHighlightSpan
    {
        public ArtiHighlightSpan(int startOffset, int length, ArtiSyntaxRole role)
        {
            StartOffset = startOffset;
            Length = length;
            Role = role;
        }

        public int StartOffset { get; }
        public int Length { get; }
        public int EndOffset { get { return StartOffset + Length; } }
        public ArtiSyntaxRole Role { get; }
    }

    internal sealed class ArtiCodeRange
    {
        public ArtiCodeRange(int startOffset, int endOffset)
        {
            StartOffset = startOffset;
            EndOffset = Math.Max(startOffset, endOffset);
        }

        public int StartOffset { get; }
        public int EndOffset { get; }

        public bool Contains(int offset)
        {
            return offset >= StartOffset && offset <= EndOffset;
        }
    }

    internal sealed class ArtiCompletionItem
    {
        public ArtiCompletionItem(string label, string detail)
        {
            Label = label ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Label { get; }
        public string Detail { get; }
    }

    internal sealed class ArtiEditorAnalysis
    {
        public ArtiEditorAnalysis(
            string source,
            IList<ArtiToken> tokens,
            IList<ArtiDiagnostic> diagnostics,
            IList<ArtiHighlightSpan> highlights,
            IList<ArtiCodeRange> codeRanges)
        {
            Source = source ?? string.Empty;
            Tokens = tokens ?? new List<ArtiToken>();
            Diagnostics = diagnostics ?? new List<ArtiDiagnostic>();
            Highlights = highlights ?? new List<ArtiHighlightSpan>();
            CodeRanges = codeRanges ?? new List<ArtiCodeRange>();
        }

        public string Source { get; }
        public IList<ArtiToken> Tokens { get; }
        public IList<ArtiDiagnostic> Diagnostics { get; }
        public IList<ArtiHighlightSpan> Highlights { get; }
        public IList<ArtiCodeRange> CodeRanges { get; }

        public int ErrorCount
        {
            get { return CountDiagnostics(ArtiDiagnosticSeverity.Error); }
        }

        public int WarningCount
        {
            get { return CountDiagnostics(ArtiDiagnosticSeverity.Warning); }
        }

        private int CountDiagnostics(ArtiDiagnosticSeverity severity)
        {
            int count = 0;
            foreach (ArtiDiagnostic diagnostic in Diagnostics)
            {
                if (diagnostic != null && diagnostic.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }
    }

    internal sealed class ArtiEditorIntelligence
    {
        private static readonly string[] Keywords =
        {
            "use",
            "optional",
            "as",
            "group",
            "const",
            "let",
            "fn",
            "if",
            "else",
            "for",
            "in",
            "while",
            "return",
            "break",
            "continue",
            "true",
            "false",
            "null"
        };

        private static readonly string[] BuiltinNames =
        {
            "core",
            "range",
            "array",
            "date",
            "html",
            "math",
            "object",
            "regex",
            "string",
            "timespan",
            "ctx",
            "pawn",
            "recipient",
            "pawns",
            "map",
            "settings",
            "game",
            "json",
            "chat",
            "prompt",
            "context",
            "current",
            "world",
            "maps",
            "factions",
            "settlements",
            "sites",
            "caravans",
            "world_objects",
            "mods",
            "defs",
            "thing",
            "things",
            "cell",
            "query",
            "setvar",
            "getvar",
            "exists",
            "len",
            "remove_space",
            "remove_spaces",
            "append",
            "prepend",
            "upper",
            "uppercase",
            "lower",
            "lowercase",
            "capitalize",
            "title",
            "trim",
            "trim_start",
            "trim_end",
            "replace",
            "contains",
            "starts_with",
            "ends_with",
            "substring",
            "split",
            "repeat",
            "is_empty",
            "random",
            "lang",
            "time",
            "hour",
            "day",
            "quadrum",
            "year",
            "season",
            "weather",
            "temperature",
            "wealth",
            "events",
            "Find",
            "GenDate",
            "PawnsFinder"
        };

        private static readonly string[] CommonMemberPaths =
        {
            "core.mod",
            "core.packageid",
            "core.emit",
            "core.append",
            "core.emit_if",
            "core.log",
            "core.try_call",
            "core.time",
            "core.time.now",
            "core.time.utc_now",
            "core.time.unix",
            "core.time.offset_minutes",
            "core.time.zone",
            "core.random",
            "core.random.int",
            "core.random.float",
            "core.random.pick",
            "core.string",
            "core.string.len",
            "core.value",
            "core.value.default",
            "core.value.coalesce",
            "core.text",
            "core.text.join_nonempty",
            "core.text.trim_lines",
            "core.text.indent",
            "core.escape",
            "core.escape.xml_text",
            "core.escape.json",
            "core.escape.markdown",
            "core.diag",
            "core.diag.warn",
            "core.game",
            "core.world",
            "core.maps",
            "core.pawns",
            "core.factions",
            "core.settlements",
            "core.world_objects",
            "core.mods",
            "core.defs",
            "core.query",
            "core.find",
            "ctx.pawn_context",
            "ctx.dialogue_type",
            "ctx.intent",
            "ctx.conversation_topic",
            "ctx.dialogue_status"
        };

        private static readonly ISet<string> BuiltinSet =
            new HashSet<string>(BuiltinNames, StringComparer.Ordinal);

        private IArtiModuleCatalog moduleCatalog;
        private ArtiSymbolCatalog symbolCatalog;
        private bool catalogsInitialized;
        private bool catalogWarningLogged;

        public ArtiEditorAnalysis AnalyzeDocument(
            string source,
            IEnumerable<string> externalGlobals = null,
            bool allowExternalGlobalRedeclare = false,
            IEnumerable<string> externalConstants = null,
            IEnumerable<string> externalFunctions = null)
        {
            source = ArtiEditorText.NormalizeLineEndings(source);
            EnsureCatalogs();

            ArtiDocumentParseResult parsed = new ArtiDocumentParser().Parse(source);
            var visibleGlobals = new HashSet<string>(externalGlobals ?? new string[0], StringComparer.Ordinal);
            var constants = new HashSet<string>(externalConstants ?? new string[0], StringComparer.Ordinal);
            var functions = new HashSet<string>(externalFunctions ?? new string[0], StringComparer.Ordinal);
            List<ArtiToken> tokens = new List<ArtiToken>();
            List<ArtiDiagnostic> diagnostics = new List<ArtiDiagnostic>();
            List<ArtiCodeRange> codeRanges = new List<ArtiCodeRange>();
            if (parsed != null)
            {
                AddDiagnostics(diagnostics, parsed.Diagnostics);
                foreach (ArtiCodeBlock block in parsed.CodeBlocks)
                {
                    if (block == null)
                    {
                        continue;
                    }

                    codeRanges.Add(new ArtiCodeRange(
                        block.BodySpan.StartOffset,
                        block.BodySpan.EndOffset));
                    if (block.ParseResult == null)
                    {
                        continue;
                    }

                    AddTokens(tokens, block.ParseResult.Tokens);
                    try
                    {
                        ArtiAnalysisResult semantic = new ArtiAnalyzer(
                            moduleCatalog,
                            symbolCatalog,
                            visibleGlobals,
                            allowExternalGlobalRedeclare, constants, functions)
                            .Analyze(ArtiGlobalRuntime.PrepareProgram(block.ParseResult.Program));
                        AddDiagnostics(diagnostics, semantic.Diagnostics);
                        if (!block.HasErrors && !semantic.HasErrors)
                            AdvancedRimTalk.Prompt.ArtiPromptAnalysisContext.AddDeclarations(block.ParseResult.Program, visibleGlobals, constants, functions);
                    }
                    catch (Exception exception)
                    {
                        WarnCatalogFailure("analysis", exception);
                    }
                }
            }

            List<ArtiHighlightSpan> highlights = BuildHighlights(source, parsed, tokens, codeRanges);
            diagnostics.Sort(CompareDiagnostics);
            return new ArtiEditorAnalysis(source, tokens, diagnostics, highlights, codeRanges);
        }

        public ArtiEditorAnalysis AnalyzeCode(
            string source,
            IEnumerable<string> externalGlobals = null,
            bool allowExternalGlobalRedeclare = false,
            IEnumerable<string> externalConstants = null,
            IEnumerable<string> externalFunctions = null)
        {
            source = ArtiEditorText.NormalizeLineEndings(source);
            EnsureCatalogs();

            ArtiParseResult parsed = new ArtiParser().Parse(source);
            List<ArtiToken> tokens = new List<ArtiToken>();
            List<ArtiDiagnostic> diagnostics = new List<ArtiDiagnostic>();
            List<ArtiCodeRange> codeRanges = new List<ArtiCodeRange>
            {
                new ArtiCodeRange(0, source.Length)
            };

            if (parsed != null)
            {
                AddTokens(tokens, parsed.Tokens);
                AddDiagnostics(diagnostics, parsed.Diagnostics);
                if (!parsed.HasErrors)
                {
                    try
                    {
                        ArtiAnalysisResult semantic = new ArtiAnalyzer(
                            moduleCatalog,
                            symbolCatalog,
                            externalGlobals,
                            allowExternalGlobalRedeclare, externalConstants, externalFunctions).Analyze(parsed.Program);
                        AddDiagnostics(diagnostics, semantic.Diagnostics);
                    }
                    catch (Exception exception)
                    {
                        WarnCatalogFailure("analysis", exception);
                    }
                }
            }

            List<ArtiHighlightSpan> highlights = BuildHighlights(source, null, tokens, codeRanges);
            diagnostics.Sort(CompareDiagnostics);
            return new ArtiEditorAnalysis(source, tokens, diagnostics, highlights, codeRanges);
        }

        public IList<ArtiCompletionItem> GetCompletionCandidates(
            string source,
            int cursor,
            ArtiEditorAnalysis analysis,
            IEnumerable<string> externalGlobals = null,
            int maximumResults = 5)
        {
            source = ArtiEditorText.NormalizeLineEndings(source);
            cursor = Math.Max(0, Math.Min(cursor, source.Length));
            int prefixStart;
            string prefix = GetCompletionPrefix(source, cursor, out prefixStart);
            string qualifier;
            bool memberAccess = TryGetMemberQualifier(source, prefixStart, out qualifier);
            List<ArtiCompletionItem> result = new List<ArtiCompletionItem>();
            Dictionary<string, string> candidates =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (memberAccess)
            {
                AddCandidate(candidates, "exists", prefix, "builtin");
                string requiredPrefix = qualifier + "." + prefix;
                foreach (string path in CommonMemberPaths)
                {
                    AddMemberCandidate(candidates, path, qualifier, prefix, requiredPrefix, "core");
                }

                EnsureCatalogs();
                if (symbolCatalog != null)
                {
                    foreach (string path in symbolCatalog.KnownPaths)
                    {
                        AddMemberCandidate(candidates, path, qualifier, prefix, requiredPrefix, "RimTalk");
                    }
                }
            }
            else
            {
                foreach (string keyword in Keywords)
                {
                    AddCandidate(candidates, keyword, prefix, "keyword");
                }

                foreach (string builtin in BuiltinNames)
                {
                    AddCandidate(candidates, builtin, prefix, "builtin");
                }

                if (analysis != null)
                {
                    foreach (ArtiToken token in analysis.Tokens)
                    {
                        if (token != null && token.Kind == ArtiTokenKind.Identifier)
                        {
                            AddCandidate(candidates, token.Text, prefix, "local");
                        }
                    }
                }

                EnsureCatalogs();
                if (symbolCatalog != null)
                {
                    foreach (string name in symbolCatalog.GlobalNames)
                    {
                        AddCandidate(candidates, name, prefix, "RimTalk");
                    }
                }

                if (externalGlobals != null)
                {
                    foreach (string name in externalGlobals)
                    {
                        AddCandidate(candidates, name, prefix, "session");
                    }
                }
            }

            List<KeyValuePair<string, string>> ordered =
                new List<KeyValuePair<string, string>>(candidates);
            ordered.Sort(delegate(KeyValuePair<string, string> left, KeyValuePair<string, string> right)
            {
                int comparison = StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key);
                return comparison != 0
                    ? comparison
                    : StringComparer.Ordinal.Compare(left.Key, right.Key);
            });

            int limit = Math.Min(Math.Max(1, Math.Min(9, maximumResults)), ordered.Count);
            for (int index = 0; index < limit; index++)
            {
                result.Add(new ArtiCompletionItem(ordered[index].Key, ordered[index].Value));
            }

            return result;
        }

        public static string GetCompletionPrefix(string source, int cursor, out int start)
        {
            source = source ?? string.Empty;
            int position = Math.Max(0, Math.Min(cursor, source.Length));
            start = position;
            while (start > 0 && ArtiEditorText.IsIdentifierPart(source[start - 1]))
            {
                start--;
            }

            return source.Substring(start, position - start);
        }

        public static bool IsMemberAccessContext(string source, int cursor)
        {
            int start;
            GetCompletionPrefix(source, cursor, out start);
            string ignored;
            return TryGetMemberQualifier(source, start, out ignored);
        }

        public static bool IsBuiltin(string value)
        {
            return !string.IsNullOrEmpty(value) && BuiltinSet.Contains(value);
        }

        private static List<ArtiHighlightSpan> BuildHighlights(
            string source,
            ArtiDocumentParseResult parsed,
            IList<ArtiToken> tokens,
            IList<ArtiCodeRange> codeRanges)
        {
            List<ArtiHighlightSpan> highlights = new List<ArtiHighlightSpan>();
            if (parsed != null)
            {
                foreach (ArtiCodeBlock block in parsed.CodeBlocks)
                {
                    if (block == null)
                    {
                        continue;
                    }

                    AddHighlight(highlights, block.OpeningSpan, ArtiSyntaxRole.Marker);
                    if (block.ClosingSpan.HasValue)
                    {
                        AddHighlight(highlights, block.ClosingSpan.Value, ArtiSyntaxRole.Marker);
                    }
                }
            }

            foreach (ArtiCodeRange range in codeRanges)
            {
                AddCommentHighlights(source, range, tokens, highlights);
            }

            foreach (ArtiToken token in tokens)
            {
                if (token == null || token.Kind == ArtiTokenKind.EndOfFile || token.Span.Length <= 0)
                {
                    continue;
                }

                AddHighlight(highlights, token.Span, GetTokenRole(token));
            }

            highlights.Sort(delegate(ArtiHighlightSpan left, ArtiHighlightSpan right)
            {
                int comparison = left.StartOffset.CompareTo(right.StartOffset);
                return comparison != 0
                    ? comparison
                    : left.Length.CompareTo(right.Length);
            });
            return highlights;
        }

        private static ArtiSyntaxRole GetTokenRole(ArtiToken token)
        {
            switch (token.Kind)
            {
                case ArtiTokenKind.True:
                case ArtiTokenKind.False:
                case ArtiTokenKind.Null:
                    return ArtiSyntaxRole.Boolean;
                case ArtiTokenKind.String:
                case ArtiTokenKind.InterpolatedStringStart:
                case ArtiTokenKind.InterpolatedStringEnd:
                    return ArtiSyntaxRole.String;
                case ArtiTokenKind.InterpolationStart:
                case ArtiTokenKind.InterpolationEnd:
                    return ArtiSyntaxRole.Marker;
                case ArtiTokenKind.Integer:
                case ArtiTokenKind.Float:
                    return ArtiSyntaxRole.Number;
                case ArtiTokenKind.Use:
                case ArtiTokenKind.Optional:
                case ArtiTokenKind.As:
                case ArtiTokenKind.Group:
                case ArtiTokenKind.Const:
                case ArtiTokenKind.Let:
                case ArtiTokenKind.Fn:
                case ArtiTokenKind.If:
                case ArtiTokenKind.Else:
                case ArtiTokenKind.For:
                case ArtiTokenKind.In:
                case ArtiTokenKind.While:
                case ArtiTokenKind.Return:
                case ArtiTokenKind.Break:
                case ArtiTokenKind.Continue:
                    return ArtiSyntaxRole.Keyword;
                case ArtiTokenKind.Identifier:
                    return IsBuiltin(token.Text)
                        ? ArtiSyntaxRole.Builtin
                        : ArtiSyntaxRole.Identifier;
                case ArtiTokenKind.LeftBrace:
                case ArtiTokenKind.RightBrace:
                case ArtiTokenKind.LeftParen:
                case ArtiTokenKind.RightParen:
                case ArtiTokenKind.LeftBracket:
                case ArtiTokenKind.RightBracket:
                case ArtiTokenKind.Comma:
                case ArtiTokenKind.Colon:
                case ArtiTokenKind.Dot:
                case ArtiTokenKind.Semicolon:
                    return ArtiSyntaxRole.Punctuation;
                case ArtiTokenKind.Plus:
                case ArtiTokenKind.Minus:
                case ArtiTokenKind.Star:
                case ArtiTokenKind.Slash:
                case ArtiTokenKind.Percent:
                case ArtiTokenKind.Bang:
                case ArtiTokenKind.Equal:
                case ArtiTokenKind.PlusEqual:
                case ArtiTokenKind.MinusEqual:
                case ArtiTokenKind.StarEqual:
                case ArtiTokenKind.SlashEqual:
                case ArtiTokenKind.PercentEqual:
                case ArtiTokenKind.EqualEqual:
                case ArtiTokenKind.BangEqual:
                case ArtiTokenKind.Less:
                case ArtiTokenKind.LessEqual:
                case ArtiTokenKind.Greater:
                case ArtiTokenKind.GreaterEqual:
                case ArtiTokenKind.AndAnd:
                case ArtiTokenKind.OrOr:
                case ArtiTokenKind.Pipe:
                case ArtiTokenKind.QuestionQuestion:
                    return ArtiSyntaxRole.Operator;
                case ArtiTokenKind.Unknown:
                    return ArtiSyntaxRole.Invalid;
                default:
                    return ArtiSyntaxRole.Plain;
            }
        }

        private static void AddCommentHighlights(
            string source,
            ArtiCodeRange range,
            IList<ArtiToken> tokens,
            IList<ArtiHighlightSpan> highlights)
        {
            int start = Math.Max(0, Math.Min(range.StartOffset, source.Length));
            int end = Math.Max(start, Math.Min(range.EndOffset, source.Length));
            int index = start;
            int tokenIndex = 0;
            while (index < end)
            {
                char current = source[index];
                while (tokenIndex < tokens.Count && tokens[tokenIndex].Span.EndOffset <= index) tokenIndex++;
                if (tokenIndex < tokens.Count && tokens[tokenIndex].Span.StartOffset <= index
                    && (tokens[tokenIndex].Kind == ArtiTokenKind.String
                        || tokens[tokenIndex].Kind == ArtiTokenKind.InterpolatedStringStart
                        || tokens[tokenIndex].Kind == ArtiTokenKind.InterpolatedStringEnd))
                {
                    index = tokens[tokenIndex].Span.EndOffset;
                    continue;
                }

                if (current == '/' && index + 1 < end && source[index + 1] == '/')
                {
                    int commentEnd = index + 2;
                    while (commentEnd < end && source[commentEnd] != '\n')
                    {
                        commentEnd++;
                    }

                    AddHighlight(
                        highlights,
                        new ArtiSourceSpan(index, commentEnd - index, 1, 1),
                        ArtiSyntaxRole.Comment);
                    index = commentEnd;
                    continue;
                }

                if (current == '/' && index + 1 < end && source[index + 1] == '*')
                {
                    int commentEnd = index + 2;
                    bool closed = false;
                    while (commentEnd + 1 < end
                        && !(source[commentEnd] == '*' && source[commentEnd + 1] == '/'))
                    {
                        commentEnd++;
                    }

                    if (commentEnd + 1 < end)
                    {
                        commentEnd += 2;
                        closed = true;
                    }

                    if (!closed)
                    {
                        commentEnd = end;
                    }

                    AddHighlight(
                        highlights,
                        new ArtiSourceSpan(index, commentEnd - index, 1, 1),
                        ArtiSyntaxRole.Comment);
                    index = commentEnd;
                    continue;
                }

                index++;
            }
        }

        private static void AddMemberCandidate(
            IDictionary<string, string> candidates,
            string path,
            string qualifier,
            string prefix,
            string requiredPrefix,
            string detail)
        {
            if (string.IsNullOrEmpty(path)
                || !path.StartsWith(qualifier + ".", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string remainder = path.Substring(qualifier.Length + 1);
            int dot = remainder.IndexOf('.');
            string member = dot >= 0 ? remainder.Substring(0, dot) : remainder;
            if (member.Length == 0
                || !path.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AddCandidate(candidates, member, prefix, detail);
        }

        private static void AddCandidate(
            IDictionary<string, string> candidates,
            string value,
            string prefix,
            string detail)
        {
            if (string.IsNullOrEmpty(value)
                || (!string.IsNullOrEmpty(prefix)
                    && !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                || string.Equals(value, prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!candidates.ContainsKey(value))
            {
                candidates.Add(value, detail);
            }
        }

        private static bool TryGetMemberQualifier(
            string source,
            int prefixStart,
            out string qualifier)
        {
            qualifier = string.Empty;
            int dot = prefixStart - 1;
            if (dot < 0 || source[dot] != '.')
            {
                return false;
            }

            int start = dot;
            while (start > 0)
            {
                char value = source[start - 1];
                if (ArtiEditorText.IsIdentifierPart(value) || value == '.')
                {
                    start--;
                    continue;
                }

                break;
            }

            qualifier = source.Substring(start, dot - start);
            return qualifier.Length > 0 && qualifier[0] != '.';
        }

        private static void AddHighlight(
            IList<ArtiHighlightSpan> highlights,
            ArtiSourceSpan span,
            ArtiSyntaxRole role)
        {
            if (span.Length > 0)
            {
                highlights.Add(new ArtiHighlightSpan(span.StartOffset, span.Length, role));
            }
        }

        private static void AddTokens(IList<ArtiToken> target, IList<ArtiToken> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (ArtiToken token in source)
            {
                if (token != null)
                {
                    target.Add(token);
                }
            }
        }

        private static void AddDiagnostics(
            IList<ArtiDiagnostic> target,
            IList<ArtiDiagnostic> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (ArtiDiagnostic diagnostic in source)
            {
                if (diagnostic != null)
                {
                    target.Add(diagnostic);
                }
            }
        }

        private static int CompareDiagnostics(ArtiDiagnostic left, ArtiDiagnostic right)
        {
            if (left == null)
            {
                return right == null ? 0 : 1;
            }

            if (right == null)
            {
                return -1;
            }

            int comparison = left.Span.StartOffset.CompareTo(right.Span.StartOffset);
            if (comparison != 0)
            {
                return comparison;
            }

            return SeverityRank(left.Severity).CompareTo(SeverityRank(right.Severity));
        }

        private static int SeverityRank(ArtiDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case ArtiDiagnosticSeverity.Error:
                    return 0;
                case ArtiDiagnosticSeverity.Warning:
                    return 1;
                default:
                    return 2;
            }
        }

        private void EnsureCatalogs()
        {
            if (catalogsInitialized)
            {
                return;
            }

            catalogsInitialized = true;
            try
            {
                moduleCatalog = RimTalkArtiCatalog.CreateModuleCatalog();
                symbolCatalog = RimTalkArtiCatalog.CreateSymbolCatalog();
            }
            catch (Exception exception)
            {
                moduleCatalog = null;
                symbolCatalog = new ArtiSymbolCatalog();
                WarnCatalogFailure("catalog", exception);
            }
        }

        private void WarnCatalogFailure(string operation, Exception exception)
        {
            if (catalogWarningLogged)
            {
                return;
            }

            catalogWarningLogged = true;
            Log.Warning(
                "Advanced RimTalk Arti editor " + operation + " failed: " + exception.Message);
        }
    }
}
