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
                    var program = new ArtiProgram(parsed.Program.Span);
                    var imports = parsed.Program.Statements.OfType<ArtiUseStatement>().ToArray();
                    string importSource = string.Join("\n", imports.Select(import =>
                        source.Substring(import.Span.StartOffset, import.Span.Length)));
                    foreach (ArtiStatement statement in parsed.Program.Statements)
                    {
                        if (statement is ArtiFunctionDeclarationStatement
                            || statement is ArtiVariableDeclarationStatement variable && variable.IsConst && variable.Name != "_")
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
                            program.Statements.Add(definition);
                        }
                        else program.Statements.Add(statement);
                    }
                    var candidate = new ArtiGlobalDefinitions();
                    candidate.Register(definitions.Snapshot());
                    candidate.Register(batch);
                    var localNames = new HashSet<string>(batch.Select(definition => definition.Name), StringComparer.Ordinal);
                    var imported = definitions.Snapshot().Where(definition => !localNames.Contains(definition.Name)).ToArray();
                    // Keep global constants available to function bodies regardless of declaration order.
                    var constants = program.Statements.OfType<ArtiVariableDeclarationStatement>()
                        .Where(statement => statement.IsConst && statement.Name != "_").ToArray();
                    foreach (var constant in constants) program.Statements.Remove(constant);
                    for (int i = constants.Length - 1; i >= 0; i--) program.Statements.Insert(0, constants[i]);
                    var analysis = new ArtiAnalyzer(
                        context.ModuleCatalog,
                        context.SymbolCatalog,
                        imported.Select(definition => definition.Name),
                        externalConstants: imported.Where(definition => definition.Declaration is ArtiVariableDeclarationStatement)
                            .Select(definition => definition.Name),
                        externalFunctions: imported.Where(definition => definition.Declaration is ArtiFunctionDeclarationStatement)
                            .Select(definition => definition.Name)).Analyze(program);
                    if (analysis.HasErrors) return new ArtiExecutionResult(string.Empty, null, analysis.Diagnostics);
                    var result = executor.ExecuteWithGlobals(program, context, imported);
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

        private static ArtiUseStatement LocalImport(ArtiUseStatement import)
            => new ArtiUseStatement(import.Span, import.PackageId, import.Alias, import.IsOptional, false);
    }
}
