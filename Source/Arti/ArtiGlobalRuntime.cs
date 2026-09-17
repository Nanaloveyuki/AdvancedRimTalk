using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedRimTalk.Arti
{
    public sealed class ArtiGlobalRuntime
    {
        private readonly ArtiGlobalDefinitions definitions = new ArtiGlobalDefinitions();
        // Reusing the executor keeps captured callables on the current call's context/output.
        // Execution is serialized by the definitions lock below.
        private readonly ArtiExecutor executor = new ArtiExecutor();

        public ArtiExecutionResult Execute(string source, string owner, ArtiExecutionContext context = null,
            int sourceOffset = 0, int startLine = 1, int startColumn = 1)
        {
            source = source ?? string.Empty;
            context = context ?? new ArtiExecutionContext();
            var parsed = new ArtiParser().Parse(source, sourceOffset, startLine, startColumn);
            if (parsed.HasErrors) return new ArtiExecutionResult(string.Empty, null, parsed.Diagnostics);
            lock (definitions.SyncRoot)
            {
                try
                {
                    if (context.Options.PersistVariables)
                        throw new InvalidOperationException("Global Arti blocks cannot persist temporary variables.");
                    var batch = new List<ArtiGlobalDefinition>();
                    var program = PrepareProgram(parsed.Program);
                    var imports = parsed.Program.Statements.OfType<ArtiUseStatement>().ToArray();
                    string importSource = string.Join("\n", imports.Select(import =>
                        source.Substring(import.Span.StartOffset - sourceOffset, import.Span.Length)));
                    foreach (ArtiStatement statement in parsed.Program.Statements)
                    {
                        if (statement is ArtiFunctionDeclarationStatement
                            || statement is ArtiVariableDeclarationStatement variable && variable.IsConst && variable.Name != "_")
                        {
                            batch.Add(new ArtiGlobalDefinition(owner + ":" + statement.Span.StartOffset,
                                importSource + "\n" + source.Substring(statement.Span.StartOffset - sourceOffset, statement.Span.Length), statement));
                        }
                    }
                    var candidate = new ArtiGlobalDefinitions();
                    candidate.Register(definitions.Snapshot());
                    candidate.Register(batch);
                    var localNames = new HashSet<string>(batch.Select(definition => definition.Name), StringComparer.Ordinal);
                    var imported = definitions.Snapshot().Where(definition => !localNames.Contains(definition.Name)).ToArray();
                    var analysis = new ArtiAnalyzer(
                        context.ModuleCatalog,
                        context.SymbolCatalog,
                        imported.Select(definition => definition.Name),
                        externalConstants: imported.Where(definition => definition.Declaration is ArtiVariableDeclarationStatement)
                            .Select(definition => definition.Name),
                        externalFunctions: imported.Where(definition => definition.Declaration is ArtiFunctionDeclarationStatement)
                            .Select(definition => definition.Name)).Analyze(program);
                    if (analysis.HasErrors) return new ArtiExecutionResult(string.Empty, null, analysis.Diagnostics);
                    var result = executor.ExecuteWithGlobals(program, context, definitions);
                    if (!result.HasErrors)
                    {
                        foreach (ArtiGlobalDefinition definition in batch)
                            definition.Bind(executor.GetDeclaredValue(definition.Name));
                        definitions.Register(batch);
                    }
                    return result;
                }
                catch (Exception exception)
                {
                    return new ArtiExecutionResult(string.Empty, null, new[]
                    {
                        new ArtiDiagnostic(ArtiDiagnosticSeverity.Error, "ART3017", exception.Message, parsed.Program.Span)
                    });
                }
            }
        }

        internal static ArtiProgram PrepareProgram(ArtiProgram source)
        {
            var program = new ArtiProgram(source.Span);
            foreach (ArtiStatement statement in source.Statements.OrderBy(statement =>
                statement is ArtiVariableDeclarationStatement variable && variable.IsConst && variable.Name != "_" ? 0 : 1))
                program.Statements.Add(statement);
            return program;
        }
    }
}
