using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedRimTalk.Arti
{
    public sealed class ArtiGlobalRuntime
    {
        private readonly ArtiGlobalDefinitions definitions = new ArtiGlobalDefinitions();

        public ArtiExecutionResult Execute(string source, string owner, ArtiExecutionContext context = null)
        {
            source = source ?? string.Empty;
            context = context ?? new ArtiExecutionContext();
            var parsed = new ArtiParser().Parse(source);
            if (parsed.HasErrors) return new ArtiExecutionResult(string.Empty, null, parsed.Diagnostics);
            lock (definitions.SyncRoot)
            {
                try
                {
                    if (context.Options.PersistVariables)
                        throw new InvalidOperationException("Global Arti blocks cannot persist temporary variables.");
                    var batch = new List<ArtiGlobalDefinition>();
                    var local = new ArtiBlockStatement(parsed.Program.Span);
                    var program = new ArtiProgram(parsed.Program.Span);
                    var imports = parsed.Program.Statements.OfType<ArtiUseStatement>().ToArray();
                    string importSource = string.Join("\n", imports.Select(import =>
                        source.Substring(import.Span.StartOffset, import.Span.Length)));
                    foreach (ArtiStatement statement in parsed.Program.Statements)
                    {
                        if (statement is ArtiFunctionDeclarationStatement
                            || statement is ArtiVariableDeclarationStatement variable && variable.IsConst)
                        {
                            ArtiStatement definition = statement;
                            if (statement is ArtiFunctionDeclarationStatement function)
                            {
                                var body = new ArtiBlockStatement(function.Body.Span);
                                foreach (var import in imports) body.Statements.Add(LocalImport(import));
                                foreach (var item in function.Body.Statements) body.Statements.Add(item);
                                var copy = new ArtiFunctionDeclarationStatement(function.Span, function.Name, body);
                                foreach (string parameter in function.Parameters) copy.Parameters.Add(parameter);
                                definition = copy;
                            }
                            batch.Add(new ArtiGlobalDefinition(owner + ":" + statement.Span.StartOffset,
                                importSource + "\n" + source.Substring(statement.Span.StartOffset, statement.Span.Length), definition));
                        }
                        else if (statement is ArtiUseStatement import) local.Statements.Add(LocalImport(import));
                        else local.Statements.Add(statement);
                    }
                    var candidate = new ArtiGlobalDefinitions();
                    candidate.Register(definitions.Snapshot());
                    candidate.Register(batch);
                    foreach (ArtiGlobalDefinition definition in candidate.Snapshot())
                        program.Statements.Add(definition.Declaration);
                    program.Statements.Add(local);
                    var analysis = new ArtiAnalyzer(
                        context.ModuleCatalog,
                        context.SymbolCatalog,
                        candidate.Snapshot().Select(definition => definition.Name),
                        true).Analyze(program);
                    if (analysis.HasErrors) return new ArtiExecutionResult(string.Empty, null, analysis.Diagnostics);
                    var result = new ArtiExecutor().Execute(program, context);
                    if (!result.HasErrors) definitions.Register(batch);
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

        private static ArtiUseStatement LocalImport(ArtiUseStatement import)
            => new ArtiUseStatement(import.Span, import.PackageId, import.Alias, import.IsOptional, false);
    }
}
