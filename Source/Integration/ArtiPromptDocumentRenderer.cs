using System;
using System.Collections.Generic;
using System.Text;
using AdvancedRimTalk.Arti;
using RimTalk.Prompt;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal sealed class ArtiPromptRenderResult
    {
        public ArtiPromptRenderResult(string text, IList<ArtiDiagnostic> diagnostics)
        {
            Text = text ?? string.Empty;
            Diagnostics = diagnostics ?? new List<ArtiDiagnostic>();
        }

        public string Text { get; }
        public IList<ArtiDiagnostic> Diagnostics { get; }
        public bool HasErrors
        {
            get
            {
                foreach (ArtiDiagnostic diagnostic in Diagnostics)
                {
                    if (diagnostic.Severity == ArtiDiagnosticSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    internal static class ArtiPromptDocumentRenderer
    {
        private static Game runtimeGame;
        private static ArtiGlobalRuntime globalRuntime;

        private static ArtiGlobalRuntime GetGlobalRuntime()
        {
            if (!ReferenceEquals(runtimeGame, Current.Game))
            {
                runtimeGame = Current.Game;
                globalRuntime = new ArtiGlobalRuntime();
            }
            return globalRuntime ?? (globalRuntime = new ArtiGlobalRuntime());
        }
        public static bool HasArtiBlocks(string source)
        {
            return !string.IsNullOrEmpty(source)
                && source.IndexOf(ArtiDocumentParser.OpeningMarker, StringComparison.Ordinal) >= 0;
        }

        public static ArtiPromptRenderResult Render(string source, PromptContext context)
        {
            source = source ?? string.Empty;
            ArtiDocumentParseResult document = new ArtiDocumentParser().Parse(source);
            List<ArtiDiagnostic> diagnostics = new List<ArtiDiagnostic>(document.Diagnostics);
            bool preview = context != null && context.IsPreview;
            if (document.CodeBlocks.Count == 0)
            {
                return new ArtiPromptRenderResult(preview && document.Diagnostics.Count > 0 ? string.Empty : source, diagnostics);
            }

            ArtiSymbolCatalog symbols = RimTalkArtiCatalog.CreateSymbolCatalog();
            IArtiModuleCatalog modules = RimTalkArtiCatalog.CreateModuleCatalog();
            ArtiExecutionContext executionContext = CreateExecutionContext(context, modules, symbols);
            StringBuilder output = new StringBuilder();
            int cursor = 0;
            foreach (ArtiCodeBlock block in document.CodeBlocks)
            {
                int start = Math.Max(cursor, Math.Min(source.Length, block.Span.StartOffset));
                int end = Math.Max(start, Math.Min(source.Length, block.Span.EndOffset));
                if (start > cursor)
                {
                    output.Append(source.Substring(cursor, start - cursor));
                }

                if (block.HasErrors)
                {
                    if (!preview) output.Append(CreateErrorMarker(FirstDiagnosticForBlock(document.Diagnostics, block), block.Span));
                }
                else
                {
                    string blockSource = block.Body ?? string.Empty;
                    ArtiExecutionResult execution = GetGlobalRuntime().Execute(
                        blockSource,
                        "prompt-document",
                        executionContext);
                    AddDiagnostics(diagnostics, execution.Diagnostics);
                    bool executionFailed = false;
                    foreach (ArtiDiagnostic diagnostic in execution.Diagnostics)
                        executionFailed |= diagnostic.Severity == ArtiDiagnosticSeverity.Error;
                    if (!preview || !executionFailed) output.Append(execution.Output);
                }

                cursor = end;
            }

            if (cursor < source.Length)
            {
                output.Append(source.Substring(cursor));
            }

            return new ArtiPromptRenderResult(output.ToString(), diagnostics);
        }

        private static ArtiExecutionContext CreateExecutionContext(
            PromptContext context,
            IArtiModuleCatalog modules,
            IArtiSymbolCatalog symbols)
        {
            PromptContext promptContext = context ?? new PromptContext();
            ArtiExecutionContext executionContext = new ArtiExecutionContext(
                new RimTalkArtiRuntimeValueProvider(promptContext),
                modules,
                symbols);
            executionContext.WarningSink = delegate(string message)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("Advanced RimTalk Arti: " + message);
                }
            };
            Dictionary<string, object> previewVariables = promptContext.IsPreview
                ? PromptPreviewSession.Variables ?? new Dictionary<string, object>(StringComparer.Ordinal) : null;
            executionContext.GetVariable = delegate(string key)
            {
                if (previewVariables != null)
                {
                    object value;
                    return previewVariables.TryGetValue(key, out value) ? value : string.Empty;
                }
                return ScribanParser.GetSessionVar(key);
            };
            executionContext.SetVariable = delegate(string key, string value)
            {
                if (previewVariables != null) previewVariables[key] = value;
                else ScribanParser.SetSessionVar(key, value);
            };
            return executionContext;
        }

        private static ArtiDiagnostic FirstDiagnosticForBlock(
            IList<ArtiDiagnostic> diagnostics,
            ArtiCodeBlock block)
        {
            if (diagnostics != null)
            {
                foreach (ArtiDiagnostic diagnostic in diagnostics)
                {
                    if (diagnostic.Span.StartOffset >= block.Span.StartOffset
                        && diagnostic.Span.StartOffset <= block.Span.EndOffset
                        && diagnostic.Severity == ArtiDiagnosticSeverity.Error)
                    {
                        return diagnostic;
                    }
                }
            }

            return null;
        }

        private static ArtiDiagnostic FirstError(IList<ArtiDiagnostic> diagnostics)
        {
            if (diagnostics != null)
            {
                foreach (ArtiDiagnostic diagnostic in diagnostics)
                {
                    if (diagnostic.Severity == ArtiDiagnosticSeverity.Error)
                    {
                        return diagnostic;
                    }
                }
            }

            return null;
        }

        private static string CreateErrorMarker(ArtiDiagnostic diagnostic, ArtiSourceSpan fallbackSpan)
        {
            if (diagnostic == null)
            {
                return "[Advanced RimTalk Arti error at "
                    + fallbackSpan.Line
                    + ":"
                    + fallbackSpan.Column
                    + "]";
            }

            return "[Advanced RimTalk Arti error "
                + diagnostic.Code
                + " at "
                + diagnostic.Span.Line
                + ":"
                + diagnostic.Span.Column
                + ": "
                + diagnostic.Message
                + "]";
        }

        private static void AddDiagnostics(
            IList<ArtiDiagnostic> target,
            IList<ArtiDiagnostic> diagnostics)
        {
            if (diagnostics == null)
            {
                return;
            }

            foreach (ArtiDiagnostic diagnostic in diagnostics)
            {
                target.Add(diagnostic);
            }
        }
    }
}
