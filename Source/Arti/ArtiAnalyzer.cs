using System;
using System.Collections.Generic;

namespace AdvancedRimTalk.Arti
{
    public interface IArtiModuleCatalog
    {
        bool TryGetModule(string packageId, out ArtiModuleInfo module);
    }

    public sealed class ArtiModuleInfo
    {
        public ArtiModuleInfo(string packageId, bool active, bool apiAvailable)
            : this(packageId, true, active, apiAvailable, string.Empty)
        {
        }

        public ArtiModuleInfo(string packageId, bool installed, bool active, bool apiAvailable, string version)
        {
            PackageId = packageId ?? string.Empty;
            Installed = installed;
            Active = active;
            ApiAvailable = apiAvailable;
            Version = version ?? string.Empty;
        }

        public string PackageId { get; }
        public bool Installed { get; }
        public bool Active { get; }
        public bool ApiAvailable { get; }
        public string Version { get; }
    }

    public sealed class ArtiAnalysisResult
    {
        public ArtiAnalysisResult(IList<ArtiDiagnostic> diagnostics)
        {
            Diagnostics = diagnostics;
        }

        public IList<ArtiDiagnostic> Diagnostics { get; }
        public bool HasErrors { get { return HasError(Diagnostics); } }

        private static bool HasError(IEnumerable<ArtiDiagnostic> diagnostics)
        {
            foreach (ArtiDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Severity == ArtiDiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class ArtiAnalyzer : ArtiDiagnosticReporter
    {
        private static readonly ISet<string> BuiltinNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "core",
            "try_call",
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

        private static readonly ISet<string> ReservedModuleAliases = new HashSet<string>(StringComparer.Ordinal)
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

        private readonly IArtiModuleCatalog _moduleCatalog;
        private readonly IArtiSymbolCatalog _symbolCatalog;
        private readonly IEnumerable<string> _externalGlobals;
        private readonly bool _allowExternalGlobalRedeclare;

        public ArtiAnalyzer(
            IArtiModuleCatalog moduleCatalog = null,
            IArtiSymbolCatalog symbolCatalog = null,
            IEnumerable<string> externalGlobals = null,
            bool allowExternalGlobalRedeclare = false)
        {
            _moduleCatalog = moduleCatalog;
            _symbolCatalog = symbolCatalog;
            _externalGlobals = externalGlobals;
            _allowExternalGlobalRedeclare = allowExternalGlobalRedeclare;
        }

        public ArtiAnalysisResult Analyze(ArtiProgram program)
        {
            ClearDiagnostics();
            if (program == null)
            {
                ReportError(3000);
                return new ArtiAnalysisResult(SnapshotDiagnostics());
            }

            Scope root = new Scope(null);
            if (_externalGlobals != null)
            {
                foreach (string name in _externalGlobals)
                {
                    root.TryDeclare(name, BindingKind.External);
                }
            }

            AnalyzeStatements(program.Statements, root, 0, 0, true);
            return new ArtiAnalysisResult(SnapshotDiagnostics());
        }

        private void AnalyzeStatements(IList<ArtiStatement> statements, Scope scope, int functionDepth, int loopDepth, bool isRoot)
        {
            PredeclareFunctions(statements, scope);
            foreach (ArtiStatement statement in statements)
            {
                AnalyzeStatement(statement, scope, functionDepth, loopDepth, isRoot);
            }
        }

        private void PredeclareFunctions(IList<ArtiStatement> statements, Scope scope)
        {
            foreach (ArtiStatement statement in statements)
            {
                ArtiFunctionDeclarationStatement function = statement as ArtiFunctionDeclarationStatement;
                if (function == null)
                {
                    continue;
                }

                if (!scope.TryDeclare(
                    function.Name,
                    BindingKind.Function,
                    _allowExternalGlobalRedeclare))
                {
                    ReportError(3001, function.Span, function.Name);
                }
            }
        }

        private void AnalyzeStatement(ArtiStatement statement, Scope scope, int functionDepth, int loopDepth, bool isRoot)
        {
            ArtiUseStatement use = statement as ArtiUseStatement;
            if (use != null)
            {
                AnalyzeModuleUse(use);
                if (use.IsGroup && !isRoot)
                {
                    ReportError(3002, use.Span);
                }

                if (!IsValidIdentifierName(use.Alias))
                {
                    ReportError(3015, use.Span, use.Alias);
                    return;
                }

                bool isBuiltinCore = string.Equals(use.PackageId, "core", StringComparison.Ordinal) && string.Equals(use.Alias, "core", StringComparison.Ordinal);
                if (!isBuiltinCore && ReservedModuleAliases.Contains(use.Alias))
                {
                    ReportError(3013, use.Span, use.Alias);
                }
                else if (!isBuiltinCore)
                {
                    if (!scope.TryDeclare(
                        use.Alias,
                        BindingKind.Module,
                        _allowExternalGlobalRedeclare))
                    {
                        ReportError(3001, use.Span, use.Alias);
                    }
                }

                return;
            }

            ArtiVariableDeclarationStatement variable = statement as ArtiVariableDeclarationStatement;
            if (variable != null)
            {
                if (variable.IsConst && !ArtiConstantExpression.IsStatic(variable.Value, name =>
                {
                    Binding binding;
                    return scope.TryResolve(name, out binding) && binding.Kind == BindingKind.Constant;
                })) ReportError(3016, variable.Span);
                if (variable.Name != "_" && !scope.TryDeclare(
                    variable.Name,
                    variable.IsConst ? BindingKind.Constant : BindingKind.Variable,
                    _allowExternalGlobalRedeclare))
                {
                    ReportError(3001, variable.Span, variable.Name);
                }

                AnalyzeExpression(variable.Value, scope);
                return;
            }

            if (statement is ArtiUnpackDeclarationStatement unpack)
            {
                AnalyzeExpression(unpack.Value, scope);
                foreach (ArtiExpression target in unpack.Targets.Items)
                {
                    var binding = target as ArtiNameExpression;
                    if (binding == null) ReportError(3014, target.Span);
                    else if (binding.Name != "_" && !scope.TryDeclare(binding.Name, BindingKind.Variable, _allowExternalGlobalRedeclare))
                        ReportError(3001, binding.Span, binding.Name);
                }
                return;
            }

            ArtiFunctionDeclarationStatement function = statement as ArtiFunctionDeclarationStatement;
            if (function != null)
            {
                Scope functionScope = new Scope(scope);
                foreach (string parameter in function.Parameters)
                {
                    if (parameter != "_" && !functionScope.TryDeclare(parameter, BindingKind.Parameter))
                    {
                        ReportError(3003, function.Span, parameter);
                    }
                }

                AnalyzeStatements(function.Body.Statements, functionScope, functionDepth + 1, 0, false);
                return;
            }

            ArtiAssignmentStatement assignment = statement as ArtiAssignmentStatement;
            if (assignment != null)
            {
                AnalyzeAssignment(assignment, scope);
                return;
            }

            ArtiIfStatement conditional = statement as ArtiIfStatement;
            if (conditional != null)
            {
                foreach (ArtiIfBranch branch in conditional.Branches)
                {
                    AnalyzeExpression(branch.Condition, scope);
                    AnalyzeStatements(branch.Body.Statements, new Scope(scope, true), functionDepth, loopDepth, false);
                }

                if (conditional.ElseBody != null)
                {
                    AnalyzeStatements(conditional.ElseBody.Statements, new Scope(scope, true), functionDepth, loopDepth, false);
                }

                return;
            }

            ArtiForStatement loop = statement as ArtiForStatement;
            if (loop != null)
            {
                AnalyzeExpression(loop.Source, scope);
                Scope loopScope = new Scope(scope, true);
                if (loop.VariableName != "_" && !loopScope.TryDeclare(loop.VariableName, BindingKind.Variable))
                {
                    ReportError(3001, loop.Span, loop.VariableName);
                }

                AnalyzeStatements(loop.Body.Statements, loopScope, functionDepth, loopDepth + 1, false);
                return;
            }

            ArtiWhileStatement whileLoop = statement as ArtiWhileStatement;
            if (whileLoop != null)
            {
                AnalyzeExpression(whileLoop.Condition, scope);
                AnalyzeStatements(whileLoop.Body.Statements, new Scope(scope, true), functionDepth, loopDepth + 1, false);
                return;
            }

            ArtiReturnStatement returnStatement = statement as ArtiReturnStatement;
            if (returnStatement != null)
            {
                if (functionDepth == 0)
                {
                    ReportError(3004, returnStatement.Span);
                }

                AnalyzeExpression(returnStatement.Value, scope);
                return;
            }

            if (statement is ArtiBreakStatement)
            {
                if (loopDepth == 0)
                {
                    ReportError(3005, statement.Span);
                }

                return;
            }

            if (statement is ArtiContinueStatement)
            {
                if (loopDepth == 0)
                {
                    ReportError(3006, statement.Span);
                }

                return;
            }

            ArtiBlockStatement block = statement as ArtiBlockStatement;
            if (block != null)
            {
                AnalyzeStatements(block.Statements, new Scope(scope, true), functionDepth, loopDepth, false);
                return;
            }

            ArtiExpressionStatement expression = statement as ArtiExpressionStatement;
            if (expression != null)
            {
                AnalyzeExpression(expression.Expression, scope);
            }
        }

        private void AnalyzeAssignment(ArtiAssignmentStatement assignment, Scope scope)
        {
            if (assignment.Target is ArtiArrayExpression targets)
            {
                if (assignment.Operator != ArtiTokenKind.Equal) ReportError(3014, assignment.Span);
                foreach (ArtiExpression target in targets.Items)
                {
                    if (target is ArtiArrayExpression) ReportError(3014, target.Span);
                    else AnalyzeAssignment(new ArtiAssignmentStatement(target.Span, target, assignment.Operator, null), scope);
                }
                AnalyzeExpression(assignment.Value, scope);
                return;
            }
            ArtiNameExpression name = assignment.Target as ArtiNameExpression;
            if (name != null)
            {
                Binding binding;
                if (name.Name == "_")
                {
                    if (assignment.Operator != ArtiTokenKind.Equal) ReportError(3014, name.Span);
                }
                else if (!scope.TryResolve(name.Name, out binding))
                {
                    ReportError(3007, name.Span, name.Name);
                }
                else if (binding.Kind == BindingKind.Constant || binding.Kind == BindingKind.Function || binding.Kind == BindingKind.Module)
                {
                    ReportError(3008, name.Span, name.Name);
                }
            }
            else
            {
                if (!(assignment.Target is ArtiErrorExpression))
                {
                    ReportError(3014, assignment.Target.Span);
                }

                AnalyzeExpression(assignment.Target, scope);
            }

            AnalyzeExpression(assignment.Value, scope);
        }

        private void AnalyzeModuleUse(ArtiUseStatement use)
        {
            if (string.IsNullOrEmpty(use.PackageId))
            {
                ReportError(3010, use.Span);
                return;
            }

            if (string.Equals(use.PackageId, "core", StringComparison.Ordinal))
            {
                return;
            }

            if (_moduleCatalog == null)
            {
                return;
            }

            ArtiModuleInfo module;
            if (!_moduleCatalog.TryGetModule(use.PackageId, out module))
            {
                if (!use.IsOptional)
                {
                    ReportError(3011, use.Span, use.PackageId);
                }

                return;
            }

            if (!use.IsOptional && (!module.Active || !module.ApiAvailable))
            {
                ReportError(3012, use.Span, use.PackageId);
            }
        }

        private void AnalyzeExpression(ArtiExpression expression, Scope scope)
        {
            if (expression == null || expression is ArtiErrorExpression)
            {
                return;
            }

            ArtiNameExpression name = expression as ArtiNameExpression;
            if (expression is ArtiInterpolatedStringExpression interpolated)
            {
                foreach (ArtiExpression part in interpolated.Parts) AnalyzeExpression(part, scope);
                return;
            }
            if (name != null)
            {
                Binding ignored;
                if (!scope.TryResolve(name.Name, out ignored)
                    && !BuiltinNames.Contains(name.Name)
                    && (_symbolCatalog == null || !_symbolCatalog.ContainsGlobal(name.Name)))
                {
                    ReportError(3009, name.Span, name.Name);
                }

                return;
            }

            ArtiMemberExpression member = expression as ArtiMemberExpression;
            if (member != null)
            {
                AnalyzeExpression(member.Target, scope);
                return;
            }

            ArtiIndexExpression index = expression as ArtiIndexExpression;
            if (index != null)
            {
                AnalyzeExpression(index.Target, scope);
                AnalyzeExpression(index.Index, scope);
                return;
            }

            ArtiCallExpression call = expression as ArtiCallExpression;
            if (call != null)
            {
                if (call.Target is ArtiNameExpression check && check.Name == "exists"
                    && !scope.TryResolve(check.Name, out Binding overridden))
                {
                    foreach (ArtiArgument argument in call.Arguments)
                        AnalyzeExistenceTarget(argument.Value, scope);
                    return;
                }
                if (call.Target is ArtiMemberExpression probe && probe.Member == "exists" && call.Arguments.Count == 0)
                {
                    AnalyzeExistenceTarget(probe.Target, scope);
                    return;
                }
                AnalyzeExpression(call.Target, scope);
                foreach (ArtiArgument argument in call.Arguments)
                {
                    AnalyzeExpression(argument.Value, scope);
                }

                return;
            }

            ArtiArrayExpression array = expression as ArtiArrayExpression;
            if (array != null)
            {
                foreach (ArtiExpression item in array.Items)
                {
                    AnalyzeExpression(item, scope);
                }

                return;
            }

            ArtiObjectExpression obj = expression as ArtiObjectExpression;
            if (obj != null)
            {
                foreach (ArtiObjectMember memberValue in obj.Members)
                {
                    AnalyzeExpression(memberValue.Value, scope);
                }

                return;
            }

            ArtiUnaryExpression unary = expression as ArtiUnaryExpression;
            if (unary != null)
            {
                AnalyzeExpression(unary.Operand, scope);
                return;
            }

            ArtiBinaryExpression binary = expression as ArtiBinaryExpression;
            if (binary != null)
            {
                AnalyzeExpression(binary.Left, scope);
                AnalyzeExpression(binary.Right, scope);
            }
        }

        private void AnalyzeExistenceTarget(ArtiExpression target, Scope scope)
        {
            if (target is ArtiNameExpression) return;
            if (target is ArtiMemberExpression member) AnalyzeExistenceTarget(member.Target, scope);
            else if (target is ArtiIndexExpression index)
            {
                AnalyzeExistenceTarget(index.Target, scope);
                AnalyzeExpression(index.Index, scope);
            }
            else AnalyzeExpression(target, scope);
        }

        private static bool IsValidIdentifierName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!char.IsLetter(name[0]) && name[0] != '_')
            {
                return false;
            }

            for (int index = 1; index < name.Length; index++)
            {
                char value = name[index];
                if (!char.IsLetterOrDigit(value) && value != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private enum BindingKind
        {
            Variable,
            Constant,
            Function,
            Parameter,
            Module,
            External
        }

        private sealed class Binding
        {
            public Binding(BindingKind kind, bool fromBlock = false)
            {
                Kind = kind;
                FromBlock = fromBlock;
            }

            public BindingKind Kind { get; }
            public bool FromBlock { get; }
        }

        private sealed class Scope
        {
            private readonly IDictionary<string, Binding> _bindings = new Dictionary<string, Binding>(StringComparer.Ordinal);
            private readonly HashSet<string> _declared = new HashSet<string>(StringComparer.Ordinal);
            private readonly bool transparent;

            public Scope(Scope parent, bool transparent = false)
            {
                // Blocks share function bindings but track duplicate declarations locally.
                this.transparent = transparent;
                Parent = transparent ? parent.Parent : parent;
                if (transparent) _bindings = parent._bindings;
            }

            public Scope Parent { get; }

            public bool TryDeclare(string name, BindingKind kind)
            {
                return TryDeclare(name, kind, false);
            }

            public bool TryDeclare(string name, BindingKind kind, bool allowExternalRedeclare)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }

                Binding existing;
                if (_bindings.TryGetValue(name, out existing))
                {
                    if ((allowExternalRedeclare && existing.Kind == BindingKind.External)
                        || (!_declared.Contains(name) && kind == BindingKind.Variable
                            && existing.Kind == BindingKind.Variable && (transparent || existing.FromBlock)))
                    {
                        _bindings[name] = new Binding(kind, transparent);
                        _declared.Add(name);
                        return true;
                    }

                    return false;
                }

                _bindings.Add(name, new Binding(kind, transparent));
                _declared.Add(name);
                return true;
            }

            public bool TryResolve(string name, out Binding binding)
            {
                if (_bindings.TryGetValue(name, out binding))
                {
                    return true;
                }

                return Parent != null && Parent.TryResolve(name, out binding);
            }
        }
    }
}
