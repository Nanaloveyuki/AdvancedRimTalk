using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Text;

namespace AdvancedRimTalk.Arti
{
    public interface IArtiRandomSource
    {
        int NextInt(int minInclusive, int maxInclusive);

        double NextDouble(double minInclusive, double maxInclusive);
    }

    public sealed class SystemArtiRandomSource : IArtiRandomSource
    {
        private static readonly Random Random = new Random();
        private static readonly object SyncRoot = new object();

        public int NextInt(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            long range = (long)maxInclusive - minInclusive + 1L;
            lock (SyncRoot)
            {
                long offset = (long)(Random.NextDouble() * range);
                if (offset >= range)
                {
                    offset = range - 1L;
                }

                return (int)((long)minInclusive + offset);
            }
        }

        public double NextDouble(double minInclusive, double maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            lock (SyncRoot)
            {
                return minInclusive + ((maxInclusive - minInclusive) * Random.NextDouble());
            }
        }
    }

    public interface IArtiRuntimeValueProvider
    {
        bool TryGetGlobal(string name, out object value);

        bool TryGetMember(object target, string member, out object value);

        bool TryGetIndex(object target, object index, out object value);
    }

    public interface IArtiCoreModuleProvider
    {
        bool TryGetCoreMember(string member, out object value);
    }

    public interface IArtiRuntimeModuleProvider
    {
        bool TryGetModuleValue(string packageId, ArtiModuleInfo module, out object value);
    }

    public interface IArtiCallable
    {
        object Invoke(IList<object> positionalArguments, IDictionary<string, object> namedArguments);
    }

    public interface IArtiMethodCallable : IArtiCallable
    {
        bool TryInvokeWithReceiver(
            object receiver,
            IList<object> positionalArguments,
            IDictionary<string, object> namedArguments,
            out object value);
    }

    public sealed class ArtiExecutionOptions
    {
        public int MaxSteps { get; set; } = 10000;
        public int MaxCallDepth { get; set; } = 64;
        public bool IncludeRuntimeErrorsInOutput { get; set; } = true;
        public bool PersistVariables { get; set; } = false;
        public bool AllowGlobalRedeclare { get; set; } = false;
    }

    public sealed class ArtiExecutionContext
    {
        public ArtiExecutionContext(
            IArtiRuntimeValueProvider valueProvider = null,
            IArtiModuleCatalog moduleCatalog = null,
            IArtiSymbolCatalog symbolCatalog = null,
            IArtiRandomSource random = null,
            ArtiExecutionOptions options = null)
        {
            Globals = new Dictionary<string, object>(StringComparer.Ordinal);
            ValueProvider = valueProvider;
            ModuleCatalog = moduleCatalog;
            SymbolCatalog = symbolCatalog;
            Random = random ?? new SystemArtiRandomSource();
            Options = options ?? new ArtiExecutionOptions();
        }

        public IDictionary<string, object> Globals { get; }
        public IArtiRuntimeValueProvider ValueProvider { get; }
        public IArtiModuleCatalog ModuleCatalog { get; }
        public IArtiSymbolCatalog SymbolCatalog { get; }
        public IArtiRandomSource Random { get; }
        public ArtiExecutionOptions Options { get; }
        public Func<string, object> GetVariable { get; set; }
        public Action<string, string> SetVariable { get; set; }
        public Action<string> WarningSink { get; set; }

        public void SetGlobal(string name, object value)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Globals[name] = value;
            }
        }

        internal bool TryGetGlobal(string name, out object value)
        {
            if (Globals.TryGetValue(name, out value))
            {
                return true;
            }

            return ValueProvider != null && ValueProvider.TryGetGlobal(name, out value);
        }

        internal bool TryGetMember(object target, string member, out object value)
        {
            value = null;
            return ValueProvider != null && ValueProvider.TryGetMember(target, member, out value);
        }

        internal bool TryGetCoreMember(string member, out object value)
        {
            value = null;
            IArtiCoreModuleProvider provider = ValueProvider as IArtiCoreModuleProvider;
            return provider != null && provider.TryGetCoreMember(member, out value);
        }

        internal bool TryGetModuleValue(string packageId, ArtiModuleInfo module, out object value)
        {
            value = null;
            IArtiRuntimeModuleProvider provider = ValueProvider as IArtiRuntimeModuleProvider;
            return provider != null && provider.TryGetModuleValue(packageId, module, out value);
        }

        internal bool TryGetIndex(object target, object index, out object value)
        {
            value = null;
            return ValueProvider != null && ValueProvider.TryGetIndex(target, index, out value);
        }
    }

    public sealed class ArtiExecutionResult
    {
        public ArtiExecutionResult(string output, object value, IList<ArtiDiagnostic> diagnostics)
        {
            Output = output ?? string.Empty;
            Value = value;
            Diagnostics = diagnostics ?? new List<ArtiDiagnostic>();
        }

        public string Output { get; }
        public object Value { get; }
        public object ReturnValue { get { return Value; } }
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

    public sealed class ArtiExecutor : ArtiDiagnosticReporter
    {
        private ArtiExecutionContext _context;
        private ArtiExecutionContext _persistentContext;
        private StringBuilder _output;
        private RuntimeObject _core;
        private RuntimeScope _rootScope;
        private int _steps;
        private int _callDepth;
        private object _lastValue;

        public ArtiExecutionResult Execute(string source, ArtiExecutionContext context = null)
        {
            ClearDiagnostics();
            ArtiParseResult parsed = new ArtiParser().Parse(source ?? string.Empty);
            AddDiagnostics(parsed.Diagnostics);
            if (!HasErrors(ReportedDiagnostics))
            {
                ArtiAnalysisResult analysis = new ArtiAnalyzer(
                    context == null ? null : context.ModuleCatalog,
                    context == null ? null : context.SymbolCatalog,
                    context == null || !context.Options.PersistVariables ? null : context.Globals.Keys,
                    context != null && context.Options.AllowGlobalRedeclare).Analyze(parsed.Program);
                AddDiagnostics(analysis.Diagnostics);
            }

            if (HasErrors(ReportedDiagnostics))
            {
                return new ArtiExecutionResult(string.Empty, null, SnapshotDiagnostics());
            }

            return ExecuteProgram(parsed.Program, context);
        }

        public ArtiExecutionResult Execute(ArtiParseResult parsed, ArtiExecutionContext context = null)
        {
            ClearDiagnostics();
            if (parsed == null)
            {
                ReportError(3000);
                return new ArtiExecutionResult(string.Empty, null, SnapshotDiagnostics());
            }

            AddDiagnostics(parsed.Diagnostics);
            if (!HasErrors(ReportedDiagnostics))
            {
                ArtiAnalysisResult analysis = new ArtiAnalyzer(
                    context == null ? null : context.ModuleCatalog,
                    context == null ? null : context.SymbolCatalog,
                    context == null || !context.Options.PersistVariables ? null : context.Globals.Keys,
                    context != null && context.Options.AllowGlobalRedeclare).Analyze(parsed.Program);
                AddDiagnostics(analysis.Diagnostics);
            }

            if (HasErrors(ReportedDiagnostics))
            {
                return new ArtiExecutionResult(string.Empty, null, SnapshotDiagnostics());
            }

            return ExecuteProgram(parsed.Program, context);
        }

        public ArtiExecutionResult Execute(ArtiProgram program, ArtiExecutionContext context = null)
        {
            ClearDiagnostics();
            return ExecuteProgram(program, context);
        }

        private ArtiExecutionResult ExecuteProgram(ArtiProgram program, ArtiExecutionContext context)
        {
            _context = context ?? new ArtiExecutionContext();
            _output = new StringBuilder();
            bool reuseScope = _context.Options.PersistVariables
                && ReferenceEquals(_persistentContext, _context)
                && _rootScope != null;
            if (!reuseScope || _core == null)
            {
                _core = CreateCoreModule();
            }

            if (!reuseScope)
            {
                _rootScope = new RuntimeScope(null);
                if (_context.Options.PersistVariables)
                {
                    _rootScope.ImportFrom(_context.Globals);
                }
            }

            _persistentContext = _context.Options.PersistVariables ? _context : null;
            _steps = 0;
            _callDepth = 0;
            _lastValue = null;

            if (program == null)
            {
                ReportError(3000);
                return CreateResult();
            }

            try
            {
                PrepareDeclarations(program.Statements, _rootScope);
                ExecuteStatements(program.Statements, _rootScope);
            }
            catch (ExecutionStopException)
            {
            }
            catch (ReturnSignal)
            {
                RecordRuntimeError(program.Span, "return escaped the root Arti program.");
            }
            catch (BreakSignal)
            {
                RecordRuntimeError(program.Span, "break escaped the root Arti program.");
            }
            catch (ContinueSignal)
            {
                RecordRuntimeError(program.Span, "continue escaped the root Arti program.");
            }
            catch (RuntimeFault fault)
            {
                RecordRuntimeError(program.Span, fault.Message);
            }
            catch (Exception exception)
            {
                RecordRuntimeError(program.Span, "Unhandled Arti runtime error: " + exception.Message);
            }

            return CreateResult();
        }

        private ArtiExecutionResult CreateResult()
        {
            if (_context != null && _context.Options.PersistVariables && _rootScope != null)
            {
                _rootScope.ExportTo(_context.Globals);
            }

            return new ArtiExecutionResult(_output == null ? string.Empty : _output.ToString(), _lastValue, SnapshotDiagnostics());
        }

        private void PrepareDeclarations(IList<ArtiStatement> statements, RuntimeScope scope)
        {
            if (statements == null)
            {
                return;
            }

            foreach (ArtiStatement statement in statements)
            {
                ArtiUseStatement use = statement as ArtiUseStatement;
                if (use != null)
                {
                    object module = ResolveModule(use.PackageId);
                    scope.TryDeclare(
                        use.Alias,
                        module,
                        true,
                        ShouldAllowRedeclare(scope));
                    continue;
                }

                ArtiFunctionDeclarationStatement function = statement as ArtiFunctionDeclarationStatement;
                if (function != null)
                {
                    scope.TryDeclare(
                        function.Name,
                        new RuntimeCallable(
                            delegate(RuntimeArguments arguments)
                            {
                                return InvokeUserFunction(function, arguments, scope);
                            }),
                        true,
                        ShouldAllowRedeclare(scope));
                }
            }
        }

        private void ExecuteStatements(IList<ArtiStatement> statements, RuntimeScope scope)
        {
            if (statements == null)
            {
                return;
            }

            foreach (ArtiStatement statement in statements)
            {
                Touch(statement == null ? ArtiSourceSpan.Empty : statement.Span);
                ExecuteStatement(statement, scope);
            }
        }

        private void ExecuteStatement(ArtiStatement statement, RuntimeScope scope)
        {
            if (statement == null)
            {
                return;
            }

            ArtiUseStatement use = statement as ArtiUseStatement;
            if (use != null)
            {
                return;
            }

            ArtiVariableDeclarationStatement variable = statement as ArtiVariableDeclarationStatement;
            if (variable != null)
            {
                object value = Evaluate(variable.Value, scope);
                if (!scope.TryDeclare(
                    variable.Name,
                    value,
                    variable.IsConst,
                    ShouldAllowRedeclare(scope)))
                {
                    RuntimeError(variable.Span, "The name '" + variable.Name + "' is already declared.");
                }

                _lastValue = value;
                return;
            }

            ArtiFunctionDeclarationStatement function = statement as ArtiFunctionDeclarationStatement;
            if (function != null)
            {
                return;
            }

            ArtiAssignmentStatement assignment = statement as ArtiAssignmentStatement;
            if (assignment != null)
            {
                object value = Evaluate(assignment.Value, scope);
                ArtiNameExpression name = assignment.Target as ArtiNameExpression;
                if (name == null)
                {
                    RuntimeError(assignment.Target == null ? assignment.Span : assignment.Target.Span, "Only a variable name can be assigned.");
                }

                if (assignment.Operator != ArtiTokenKind.Equal)
                {
                    object current;
                    if (!scope.TryGet(name.Name, out current))
                    {
                        RuntimeError(name.Span, "The name '" + name.Name + "' is not declared.");
                    }

                    value = ApplyBinary(CompoundOperator(assignment.Operator), current, value, assignment.Span);
                }

                string assignmentError;
                if (!scope.TryAssign(name.Name, value, out assignmentError))
                {
                    RuntimeError(name.Span, assignmentError);
                }

                _lastValue = value;
                return;
            }

            ArtiIfStatement conditional = statement as ArtiIfStatement;
            if (conditional != null)
            {
                foreach (ArtiIfBranch branch in conditional.Branches)
                {
                    if (IsTruthy(Evaluate(branch.Condition, scope)))
                    {
                        ExecuteBlock(branch.Body, scope);
                        return;
                    }
                }

                if (conditional.ElseBody != null)
                {
                    ExecuteBlock(conditional.ElseBody, scope);
                }

                return;
            }

            ArtiForStatement loop = statement as ArtiForStatement;
            if (loop != null)
            {
                object source = Evaluate(loop.Source, scope);
                RuntimeScope loopScope = new RuntimeScope(scope);
                if (!loopScope.TryDeclare(loop.VariableName, null, false))
                {
                    RuntimeError(loop.Span, "The loop variable '" + loop.VariableName + "' is already declared.");
                }

                foreach (object item in Enumerate(source, loop.Source == null ? loop.Span : loop.Source.Span))
                {
                    Touch(loop.Span);
                    string assignmentError;
                    if (!loopScope.TryAssign(loop.VariableName, item, out assignmentError))
                    {
                        RuntimeError(loop.Span, assignmentError);
                    }

                    try
                    {
                        ExecuteBlock(loop.Body, loopScope);
                    }
                    catch (ContinueSignal)
                    {
                    }
                    catch (BreakSignal)
                    {
                        break;
                    }
                }

                return;
            }

            ArtiWhileStatement whileLoop = statement as ArtiWhileStatement;
            if (whileLoop != null)
            {
                while (IsTruthy(Evaluate(whileLoop.Condition, scope)))
                {
                    Touch(whileLoop.Span);
                    try
                    {
                        ExecuteBlock(whileLoop.Body, scope);
                    }
                    catch (ContinueSignal)
                    {
                    }
                    catch (BreakSignal)
                    {
                        break;
                    }
                }

                return;
            }

            ArtiReturnStatement returnStatement = statement as ArtiReturnStatement;
            if (returnStatement != null)
            {
                throw new ReturnSignal(returnStatement.Value == null ? null : Evaluate(returnStatement.Value, scope));
            }

            if (statement is ArtiBreakStatement)
            {
                throw new BreakSignal();
            }

            if (statement is ArtiContinueStatement)
            {
                throw new ContinueSignal();
            }

            ArtiBlockStatement block = statement as ArtiBlockStatement;
            if (block != null)
            {
                ExecuteBlock(block, scope);
                return;
            }

            ArtiExpressionStatement expression = statement as ArtiExpressionStatement;
            if (expression != null)
            {
                _lastValue = Evaluate(expression.Expression, scope);
            }
        }

        private void ExecuteBlock(ArtiBlockStatement block, RuntimeScope parent)
        {
            if (block == null)
            {
                return;
            }

            RuntimeScope scope = new RuntimeScope(parent);
            PrepareDeclarations(block.Statements, scope);
            ExecuteStatements(block.Statements, scope);
        }

        private bool ShouldAllowRedeclare(RuntimeScope scope)
        {
            return _context != null
                && _context.Options.AllowGlobalRedeclare
                && ReferenceEquals(scope, _rootScope);
        }

        private object Evaluate(ArtiExpression expression, RuntimeScope scope)
        {
            if (expression == null || expression is ArtiErrorExpression)
            {
                return null;
            }

            Touch(expression.Span);

            ArtiLiteralExpression literal = expression as ArtiLiteralExpression;
            if (literal != null)
            {
                return literal.Value;
            }

            ArtiNameExpression name = expression as ArtiNameExpression;
            if (name != null)
            {
                object value;
                if (scope.TryGet(name.Name, out value))
                {
                    return value;
                }

                if (TryGetBuiltin(name.Name, out value))
                {
                    return value;
                }

                try
                {
                    if (_context.TryGetGlobal(name.Name, out value))
                    {
                        return value;
                    }
                }
                catch (Exception exception)
                {
                    RuntimeError(name.Span, "Global provider failed for '" + name.Name + "': " + exception.Message);
                }

                RuntimeError(name.Span, "The name '" + name.Name + "' is not declared.");
            }

            ArtiMemberExpression member = expression as ArtiMemberExpression;
            if (member != null)
            {
                object target = Evaluate(member.Target, scope);
                object value;
                if (TryGetMember(target, member.Member, out value))
                {
                    return value;
                }

                RuntimeError(member.Span, "The member '" + member.Member + "' is not available.");
            }

            ArtiIndexExpression index = expression as ArtiIndexExpression;
            if (index != null)
            {
                object target = Evaluate(index.Target, scope);
                object indexValue = Evaluate(index.Index, scope);
                object value;
                if (TryGetIndex(target, indexValue, out value))
                {
                    return value;
                }

                RuntimeError(index.Span, "The value cannot be indexed with '" + FormatValue(indexValue) + "'.");
            }

            ArtiCallExpression call = expression as ArtiCallExpression;
            if (call != null)
            {
                ArtiMemberExpression memberTarget = call.Target as ArtiMemberExpression;
                object receiver = null;
                object target;
                if (memberTarget != null)
                {
                    receiver = Evaluate(memberTarget.Target, scope);
                    if (!TryGetMember(receiver, memberTarget.Member, out target))
                    {
                        RuntimeError(memberTarget.Span, "The member '" + memberTarget.Member + "' is not available.");
                    }
                }
                else
                {
                    target = Evaluate(call.Target, scope);
                }

                RuntimeArguments arguments = new RuntimeArguments();
                foreach (ArtiArgument argument in call.Arguments)
                {
                    arguments.Add(new RuntimeArgument(argument == null ? null : argument.Name, Evaluate(argument == null ? null : argument.Value, scope)));
                }

                try
                {
                    return memberTarget == null
                        ? InvokeCallable(target, arguments)
                        : InvokeCallable(target, arguments, receiver);
                }
                catch (ExecutionStopException)
                {
                    throw;
                }
                catch (RuntimeFault fault)
                {
                    RuntimeError(call.Span, fault.Message);
                }
                catch (Exception exception)
                {
                    RuntimeError(call.Span, "Function call failed: " + exception.Message);
                }
            }

            ArtiArrayExpression array = expression as ArtiArrayExpression;
            if (array != null)
            {
                List<object> values = new List<object>();
                foreach (ArtiExpression item in array.Items)
                {
                    values.Add(Evaluate(item, scope));
                }

                return values;
            }

            ArtiObjectExpression obj = expression as ArtiObjectExpression;
            if (obj != null)
            {
                RuntimeObject value = new RuntimeObject();
                foreach (ArtiObjectMember item in obj.Members)
                {
                    value.Set(item.Name, Evaluate(item.Value, scope));
                }

                return value;
            }

            ArtiUnaryExpression unary = expression as ArtiUnaryExpression;
            if (unary != null)
            {
                object operand = Evaluate(unary.Operand, scope);
                switch (unary.Operator)
                {
                    case ArtiTokenKind.Bang:
                        return !IsTruthy(operand);
                    case ArtiTokenKind.Plus:
                        return NumericValue(operand, unary.Span);
                    case ArtiTokenKind.Minus:
                        return NegateNumber(operand, unary.Span);
                    default:
                        RuntimeError(unary.Span, "Unsupported unary operator.");
                        break;
                }
            }

            ArtiBinaryExpression binary = expression as ArtiBinaryExpression;
            if (binary != null)
            {
                object left = Evaluate(binary.Left, scope);
                if (binary.Operator == ArtiTokenKind.AndAnd && !IsTruthy(left))
                {
                    return false;
                }

                if (binary.Operator == ArtiTokenKind.OrOr && IsTruthy(left))
                {
                    return true;
                }

                if (binary.Operator == ArtiTokenKind.QuestionQuestion && left != null)
                {
                    return left;
                }

                object right = Evaluate(binary.Right, scope);
                if (binary.Operator == ArtiTokenKind.AndAnd)
                {
                    return IsTruthy(right);
                }

                if (binary.Operator == ArtiTokenKind.OrOr)
                {
                    return IsTruthy(right);
                }

                return ApplyBinary(binary.Operator, left, right, binary.Span);
            }

            return null;
        }

        private object InvokeCallable(object target, RuntimeArguments arguments)
        {
            RuntimeCallable runtimeCallable = target as RuntimeCallable;
            if (runtimeCallable != null)
            {
                return runtimeCallable.InvokeInternal(arguments);
            }

            IArtiCallable callable = target as IArtiCallable;
            if (callable != null)
            {
                Dictionary<string, object> named = new Dictionary<string, object>(StringComparer.Ordinal);
                List<object> positional = new List<object>();
                foreach (RuntimeArgument argument in arguments.Items)
                {
                    if (argument.Name == null)
                    {
                        positional.Add(argument.Value);
                    }
                    else
                    {
                        if (named.ContainsKey(argument.Name))
                        {
                            throw new RuntimeFault("The named argument '" + argument.Name + "' was supplied more than once.");
                        }

                        named.Add(argument.Name, argument.Value);
                    }
                }

                return callable.Invoke(positional, named);
            }

            throw new RuntimeFault("The value is not callable.");
        }

        private object InvokeCallable(object target, RuntimeArguments arguments, object receiver)
        {
            IArtiMethodCallable methodCallable = target as IArtiMethodCallable;
            if (methodCallable != null)
            {
                Dictionary<string, object> named = new Dictionary<string, object>(StringComparer.Ordinal);
                List<object> positional = new List<object>();
                foreach (RuntimeArgument argument in arguments.Items)
                {
                    if (argument.Name == null)
                    {
                        positional.Add(argument.Value);
                    }
                    else
                    {
                        if (named.ContainsKey(argument.Name))
                        {
                            throw new RuntimeFault("The named argument '" + argument.Name + "' was supplied more than once.");
                        }

                        named.Add(argument.Name, argument.Value);
                    }
                }

                object value;
                if (methodCallable.TryInvokeWithReceiver(receiver, positional, named, out value))
                {
                    return value;
                }
            }

            return InvokeCallable(target, arguments);
        }

        private object InvokeUserFunction(
            ArtiFunctionDeclarationStatement function,
            RuntimeArguments arguments,
            RuntimeScope closure)
        {
            if (function == null)
            {
                throw new RuntimeFault("The function declaration is missing.");
            }

            _callDepth++;
            if (_callDepth > Math.Max(1, _context.Options.MaxCallDepth))
            {
                _callDepth--;
                throw new RuntimeFault("The maximum Arti call depth was exceeded.");
            }

            RuntimeScope functionScope = new RuntimeScope(closure);
            bool[] assigned = new bool[function.Parameters.Count];
            int nextPositional = 0;
            try
            {
                foreach (RuntimeArgument argument in arguments.Items)
                {
                    int parameterIndex;
                    if (argument.Name != null)
                    {
                        parameterIndex = -1;
                        for (int index = 0; index < function.Parameters.Count; index++)
                        {
                            if (string.Equals(function.Parameters[index], argument.Name, StringComparison.Ordinal))
                            {
                                parameterIndex = index;
                                break;
                            }
                        }

                        if (parameterIndex < 0)
                        {
                            throw new RuntimeFault("The function has no parameter named '" + argument.Name + "'.");
                        }
                    }
                    else
                    {
                        while (nextPositional < assigned.Length && assigned[nextPositional])
                        {
                            nextPositional++;
                        }

                        parameterIndex = nextPositional++;
                    }

                    if (parameterIndex < 0 || parameterIndex >= assigned.Length)
                    {
                        throw new RuntimeFault("Too many arguments were supplied to '" + function.Name + "'.");
                    }

                    if (assigned[parameterIndex])
                    {
                        throw new RuntimeFault("The parameter '" + function.Parameters[parameterIndex] + "' was supplied more than once.");
                    }

                    assigned[parameterIndex] = true;
                    functionScope.TryDeclare(function.Parameters[parameterIndex], argument.Value, false);
                }

                for (int index = 0; index < assigned.Length; index++)
                {
                    if (!assigned[index])
                    {
                        throw new RuntimeFault("The parameter '" + function.Parameters[index] + "' was not supplied.");
                    }
                }

                PrepareDeclarations(function.Body == null ? null : function.Body.Statements, functionScope);
                try
                {
                    ExecuteStatements(function.Body == null ? null : function.Body.Statements, functionScope);
                }
                catch (ReturnSignal signal)
                {
                    return signal.Value;
                }

                return null;
            }
            finally
            {
                _callDepth--;
            }
        }

        private bool TryGetBuiltin(string name, out object value)
        {
            if (string.Equals(name, "core", StringComparison.Ordinal))
            {
                value = _core;
                return true;
            }

            if (string.Equals(name, "range", StringComparison.Ordinal))
            {
                value = new RuntimeCallable(CreateRange);
                return true;
            }

            if (string.Equals(name, "random", StringComparison.Ordinal))
            {
                value = new RuntimeCallable(delegate(RuntimeArguments arguments)
                {
                    int min = ToInt(arguments.Get(0, "min", true), ArtiSourceSpan.Empty);
                    int max = ToInt(arguments.Get(1, "max", true), ArtiSourceSpan.Empty);
                    return _context.Random.NextInt(min, max);
                });
                return true;
            }

            if (string.Equals(name, "getvar", StringComparison.Ordinal))
            {
                value = new RuntimeCallable(delegate(RuntimeArguments arguments)
                {
                    string key = ToText(arguments.Get(0, "key", true));
                    return _context.GetVariable == null ? null : _context.GetVariable(key);
                });
                return true;
            }

            if (string.Equals(name, "setvar", StringComparison.Ordinal))
            {
                value = new RuntimeCallable(delegate(RuntimeArguments arguments)
                {
                    string key = ToText(arguments.Get(0, "key", true));
                    string variableValue = ToText(arguments.Get(1, "value", true));
                    if (_context.SetVariable != null)
                    {
                        _context.SetVariable(key, variableValue);
                    }

                    return variableValue;
                });
                return true;
            }

            IArtiCallable stringCallable;
            if (ArtiStringFunctions.TryGetCallable(name, out stringCallable))
            {
                value = stringCallable;
                return true;
            }

            value = null;
            return false;
        }

        private RuntimeObject CreateCoreModule()
        {
            RuntimeObject core = new RuntimeObject();
            core.Set("mod", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                return CreateModuleValue(ToText(arguments.Get(0, "id", true)));
            }));
            core.Set("packageid", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                return CreateModuleValue(ToText(arguments.Get(0, "id", true)));
            }));
            core.Set("emit", new RuntimeCallable(Emit));
            core.Set("append", new RuntimeCallable(Emit));
            core.Set("emit_if", new RuntimeCallable(EmitIf));
            core.Set("log", new RuntimeCallable(Warn));

            RuntimeObject stringModule = new RuntimeObject();
            foreach (string name in ArtiStringFunctions.Names)
            {
                IArtiCallable stringCallable;
                if (ArtiStringFunctions.TryGetCallable(name, out stringCallable))
                {
                    stringModule.Set(name, stringCallable);
                }
            }
            core.Set("string", stringModule);

            RuntimeObject random = new RuntimeObject();
            random.Set("int", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                int min = ToInt(arguments.Get(0, "min", true), ArtiSourceSpan.Empty);
                int max = ToInt(arguments.Get(1, "max", true), ArtiSourceSpan.Empty);
                return _context.Random.NextInt(min, max);
            }));
            random.Set("float", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                double min = ToDouble(arguments.Get(0, "min", true), ArtiSourceSpan.Empty);
                double max = ToDouble(arguments.Get(1, "max", true), ArtiSourceSpan.Empty);
                return _context.Random.NextDouble(min, max);
            }));
            random.Set("pick", new RuntimeCallable(RandomPick));
            core.Set("random", random);

            RuntimeObject value = new RuntimeObject();
            value.Set("default", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                object first = arguments.Get(0, "value", true);
                object fallback = arguments.Get(1, "fallback", true);
                return IsEmpty(first) ? fallback : first;
            }));
            value.Set("coalesce", new RuntimeCallable(Coalesce));
            core.Set("value", value);

            RuntimeObject text = new RuntimeObject();
            text.Set("join_nonempty", new RuntimeCallable(JoinNonEmpty));
            text.Set("trim_lines", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                return TrimLines(ToText(arguments.Get(0, "text", true)));
            }));
            text.Set("indent", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                string input = ToText(arguments.Get(0, "text", true));
                int level = ToInt(arguments.Get(1, "level", true), ArtiSourceSpan.Empty);
                return Indent(input, level);
            }));
            core.Set("text", text);

            RuntimeObject escape = new RuntimeObject();
            escape.Set("xml_text", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                return SecurityElement.Escape(ToText(arguments.Get(0, "text", true))) ?? string.Empty;
            }));
            escape.Set("json", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                return EscapeJson(ToText(arguments.Get(0, "text", true)));
            }));
            escape.Set("markdown", new RuntimeCallable(delegate(RuntimeArguments arguments)
            {
                return EscapeMarkdown(ToText(arguments.Get(0, "text", true)));
            }));
            core.Set("escape", escape);

            RuntimeObject diagnostics = new RuntimeObject();
            diagnostics.Set("warn", new RuntimeCallable(Warn));
            core.Set("diag", diagnostics);
            return core;
        }

        private object Emit(RuntimeArguments arguments)
        {
            object value = arguments.Get(0, "text", true);
            bool newline = ToBool(arguments.Get(1, "newline", false), false);
            _output.Append(ToText(value));
            if (newline)
            {
                _output.Append('\n');
            }

            return value;
        }

        private object EmitIf(RuntimeArguments arguments)
        {
            object condition = arguments.Get(0, "condition", true);
            object value = arguments.Get(1, "text", true);
            if (IsTruthy(condition))
            {
                bool newline = ToBool(arguments.Get(2, "newline", false), false);
                _output.Append(ToText(value));
                if (newline)
                {
                    _output.Append('\n');
                }
            }

            return value;
        }

        private object Warn(RuntimeArguments arguments)
        {
            string message = ToText(arguments.Get(0, "message", true));
            if (_context.WarningSink != null)
            {
                _context.WarningSink(message);
            }

            return null;
        }

        private object RandomPick(RuntimeArguments arguments)
        {
            List<object> values = new List<object>();
            if (arguments.Positional.Count == 1 && IsEnumerableValue(arguments.Positional[0]))
            {
                values.AddRange(Enumerate(arguments.Positional[0], ArtiSourceSpan.Empty));
            }
            else
            {
                values.AddRange(arguments.Positional);
            }

            if (values.Count == 0)
            {
                return null;
            }

            int index = _context.Random.NextInt(0, values.Count - 1);
            return values[index];
        }

        private object Coalesce(RuntimeArguments arguments)
        {
            IEnumerable<object> values;
            if (arguments.Positional.Count == 1 && IsEnumerableValue(arguments.Positional[0]))
            {
                values = Enumerate(arguments.Positional[0], ArtiSourceSpan.Empty);
            }
            else
            {
                values = arguments.Positional;
            }

            foreach (object value in values)
            {
                if (!IsEmpty(value))
                {
                    return value;
                }
            }

            return null;
        }

        private object JoinNonEmpty(RuntimeArguments arguments)
        {
            string separator = ToText(arguments.Get(0, "separator", true));
            List<object> source = new List<object>();
            if (arguments.Positional.Count >= 2)
            {
                if (arguments.Positional.Count == 2 && IsEnumerableValue(arguments.Positional[1]))
                {
                    source.AddRange(Enumerate(arguments.Positional[1], ArtiSourceSpan.Empty));
                }
                else
                {
                    for (int index = 1; index < arguments.Positional.Count; index++)
                    {
                        source.Add(arguments.Positional[index]);
                    }
                }
            }

            List<string> values = new List<string>();
            foreach (object value in source)
            {
                string text = ToText(value);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    values.Add(text.Trim());
                }
            }

            return string.Join(separator, values);
        }

        private object CreateRange(RuntimeArguments arguments)
        {
            int count = arguments.Positional.Count;
            if (count < 1 || count > 3)
            {
                throw new RuntimeFault("range expects one to three positional arguments.");
            }

            int start;
            int stop;
            int step;
            if (count == 1)
            {
                start = 0;
                stop = ToInt(arguments.Positional[0], ArtiSourceSpan.Empty);
                step = 1;
            }
            else
            {
                start = ToInt(arguments.Positional[0], ArtiSourceSpan.Empty);
                stop = ToInt(arguments.Positional[1], ArtiSourceSpan.Empty);
                step = count == 3 ? ToInt(arguments.Positional[2], ArtiSourceSpan.Empty) : 1;
            }

            if (step == 0)
            {
                throw new RuntimeFault("range step cannot be zero.");
            }

            List<int> result = new List<int>();
            if (step > 0)
            {
                for (int value = start; value < stop; value += step)
                {
                    result.Add(value);
                    if (result.Count > Math.Max(1, _context.Options.MaxSteps))
                    {
                        throw new RuntimeFault("range produced too many values.");
                    }
                }
            }
            else
            {
                for (int value = start; value > stop; value += step)
                {
                    result.Add(value);
                    if (result.Count > Math.Max(1, _context.Options.MaxSteps))
                    {
                        throw new RuntimeFault("range produced too many values.");
                    }
                }
            }

            return result;
        }

        private object ResolveModule(string packageId)
        {
            if (string.Equals(packageId, "core", StringComparison.Ordinal))
            {
                return _core;
            }

            ArtiModuleInfo module = null;
            try
            {
                if (_context.ModuleCatalog != null)
                {
                    _context.ModuleCatalog.TryGetModule(packageId, out module);
                }
            }
            catch (Exception exception)
            {
                throw new RuntimeFault("Module provider failed for '" + packageId + "': " + exception.Message);
            }

            if (module == null)
            {
                module = new ArtiModuleInfo(packageId, false, false, false, string.Empty);
            }

            object runtimeValue;
            if (_context.TryGetModuleValue(packageId, module, out runtimeValue))
            {
                return runtimeValue;
            }

            return new RuntimeModule(module);
        }

        private object CreateModuleValue(string packageId)
        {
            return ResolveModule(packageId);
        }

        private bool TryGetMember(object target, string member, out object value)
        {
            RuntimeObject runtimeObject = target as RuntimeObject;
            if (runtimeObject != null && runtimeObject.TryGet(member, out value))
            {
                return true;
            }

            if (ReferenceEquals(target, _core) && _context.TryGetCoreMember(member, out value))
            {
                return true;
            }

            RuntimeModule module = target as RuntimeModule;
            if (module != null && module.TryGet(member, out value))
            {
                return true;
            }

            IDictionary<string, object> dictionary = target as IDictionary<string, object>;
            if (dictionary != null && dictionary.TryGetValue(member, out value))
            {
                return true;
            }

            IDictionary nonGenericDictionary = target as IDictionary;
            if (nonGenericDictionary != null && nonGenericDictionary.Contains(member))
            {
                value = nonGenericDictionary[member];
                return true;
            }

            string text = target as string;
            if (text != null)
            {
                if (string.Equals(member, "length", StringComparison.Ordinal))
                {
                    value = text.Length;
                    return true;
                }

                IArtiCallable stringCallable;
                if (ArtiStringFunctions.TryGetCallable(member, out stringCallable))
                {
                    value = stringCallable;
                    return true;
                }
            }

            IList list = target as IList;
            if (list != null)
            {
                if (string.Equals(member, "count", StringComparison.Ordinal)
                    || string.Equals(member, "size", StringComparison.Ordinal)
                    || string.Equals(member, "length", StringComparison.Ordinal))
                {
                    value = list.Count;
                    return true;
                }

                if (string.Equals(member, "first", StringComparison.Ordinal))
                {
                    value = list.Count == 0 ? null : list[0];
                    return true;
                }

                if (string.Equals(member, "last", StringComparison.Ordinal))
                {
                    value = list.Count == 0 ? null : list[list.Count - 1];
                    return true;
                }
            }

            if (_context.TryGetMember(target, member, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        private bool TryGetIndex(object target, object index, out object value)
        {
            RuntimeObject runtimeObject = target as RuntimeObject;
            string key = index as string;
            if (runtimeObject != null && key != null && runtimeObject.TryGet(key, out value))
            {
                return true;
            }

            IDictionary<string, object> dictionary = target as IDictionary<string, object>;
            if (dictionary != null && key != null && dictionary.TryGetValue(key, out value))
            {
                return true;
            }

            IDictionary nonGenericDictionary = target as IDictionary;
            if (nonGenericDictionary != null && nonGenericDictionary.Contains(index))
            {
                value = nonGenericDictionary[index];
                return true;
            }

            int numericIndex;
            if (TryGetInt(index, out numericIndex))
            {
                IList list = target as IList;
                if (list != null && numericIndex >= 0 && numericIndex < list.Count)
                {
                    value = list[numericIndex];
                    return true;
                }

                Array array = target as Array;
                if (array != null && array.Rank == 1 && numericIndex >= 0 && numericIndex < array.Length)
                {
                    value = array.GetValue(numericIndex);
                    return true;
                }

                string text = target as string;
                if (text != null && numericIndex >= 0 && numericIndex < text.Length)
                {
                    value = text[numericIndex].ToString();
                    return true;
                }
            }

            if (_context.TryGetIndex(target, index, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        private object ApplyBinary(ArtiTokenKind op, object left, object right, ArtiSourceSpan span)
        {
            switch (op)
            {
                case ArtiTokenKind.Pipe:
                    RuntimeArguments pipelineArguments = new RuntimeArguments();
                    pipelineArguments.Add(new RuntimeArgument(null, left));
                    return InvokeCallable(right, pipelineArguments);
                case ArtiTokenKind.QuestionQuestion:
                    return left ?? right;
                case ArtiTokenKind.EqualEqual:
                    return AreEqual(left, right);
                case ArtiTokenKind.BangEqual:
                    return !AreEqual(left, right);
                case ArtiTokenKind.Less:
                    return Compare(left, right, span) < 0;
                case ArtiTokenKind.LessEqual:
                    return Compare(left, right, span) <= 0;
                case ArtiTokenKind.Greater:
                    return Compare(left, right, span) > 0;
                case ArtiTokenKind.GreaterEqual:
                    return Compare(left, right, span) >= 0;
                case ArtiTokenKind.Plus:
                    if (left is string || right is string)
                    {
                        return ToText(left) + ToText(right);
                    }

                    return NumericOperation(left, right, op, span);
                case ArtiTokenKind.Minus:
                case ArtiTokenKind.Star:
                case ArtiTokenKind.Slash:
                case ArtiTokenKind.Percent:
                    return NumericOperation(left, right, op, span);
                default:
                    RuntimeError(span, "Unsupported binary operator.");
                    return null;
            }
        }

        private object NumericOperation(object left, object right, ArtiTokenKind op, ArtiSourceSpan span)
        {
            double leftNumber = ToDouble(left, span);
            double rightNumber = ToDouble(right, span);
            if ((op == ArtiTokenKind.Slash || op == ArtiTokenKind.Percent) && rightNumber == 0d)
            {
                RuntimeError(span, "Division by zero.");
            }

            double result;
            switch (op)
            {
                case ArtiTokenKind.Plus:
                    result = leftNumber + rightNumber;
                    break;
                case ArtiTokenKind.Minus:
                    result = leftNumber - rightNumber;
                    break;
                case ArtiTokenKind.Star:
                    result = leftNumber * rightNumber;
                    break;
                case ArtiTokenKind.Slash:
                    result = leftNumber / rightNumber;
                    break;
                case ArtiTokenKind.Percent:
                    result = leftNumber % rightNumber;
                    break;
                default:
                    RuntimeError(span, "Unsupported numeric operator.");
                    return null;
            }

            if (IsIntegral(left) && IsIntegral(right) && op != ArtiTokenKind.Slash)
            {
                if (result >= int.MinValue && result <= int.MaxValue)
                {
                    return (int)result;
                }

                return (long)result;
            }

            return result;
        }

        private static ArtiTokenKind CompoundOperator(ArtiTokenKind assignmentOperator)
        {
            switch (assignmentOperator)
            {
                case ArtiTokenKind.PlusEqual: return ArtiTokenKind.Plus;
                case ArtiTokenKind.MinusEqual: return ArtiTokenKind.Minus;
                case ArtiTokenKind.StarEqual: return ArtiTokenKind.Star;
                case ArtiTokenKind.SlashEqual: return ArtiTokenKind.Slash;
                case ArtiTokenKind.PercentEqual: return ArtiTokenKind.Percent;
                default: return ArtiTokenKind.EqualEqual;
            }
        }

        private static bool AreEqual(object left, object right)
        {
            if (left == null || right == null)
            {
                return left == null && right == null;
            }

            if (IsNumeric(left) && IsNumeric(right))
            {
                return Convert.ToDouble(left, CultureInfo.InvariantCulture)
                    == Convert.ToDouble(right, CultureInfo.InvariantCulture);
            }

            return Equals(left, right);
        }

        private static int Compare(object left, object right, ArtiSourceSpan span)
        {
            if (IsNumeric(left) && IsNumeric(right))
            {
                return Convert.ToDouble(left, CultureInfo.InvariantCulture)
                    .CompareTo(Convert.ToDouble(right, CultureInfo.InvariantCulture));
            }

            if (left is string && right is string)
            {
                return string.CompareOrdinal((string)left, (string)right);
            }

            throw new RuntimeFault("Values are not comparable.");
        }

        private static bool IsTruthy(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            if (IsNumeric(value))
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0d;
            }

            string text = value as string;
            if (text != null)
            {
                return !string.IsNullOrWhiteSpace(text);
            }

            ICollection collection = value as ICollection;
            if (collection != null)
            {
                return collection.Count > 0;
            }

            return true;
        }

        private static bool IsEmpty(object value)
        {
            if (value == null)
            {
                return true;
            }

            string text = value as string;
            if (text != null)
            {
                return string.IsNullOrWhiteSpace(text);
            }

            ICollection collection = value as ICollection;
            return collection != null && collection.Count == 0;
        }

        private static bool IsNumeric(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort
                || value is int || value is uint || value is long || value is ulong
                || value is float || value is double || value is decimal;
        }

        private static bool IsIntegral(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort
                || value is int || value is uint || value is long || value is ulong;
        }

        private static bool TryGetInt(object value, out int result)
        {
            if (IsNumeric(value))
            {
                try
                {
                    result = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    return true;
                }
                catch (Exception)
                {
                }
            }

            result = 0;
            return false;
        }

        private static int ToInt(object value, ArtiSourceSpan span)
        {
            int result;
            if (TryGetInt(value, out result))
            {
                return result;
            }

            string text = value as string;
            if (text != null && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            throw new RuntimeFault("Expected an integer.");
        }

        private static double ToDouble(object value, ArtiSourceSpan span)
        {
            if (IsNumeric(value))
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            string text = value as string;
            double result;
            if (text != null && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            throw new RuntimeFault("Expected a number.");
        }

        private static object NumericValue(object value, ArtiSourceSpan span)
        {
            return IsIntegral(value) ? (object)ToInt(value, span) : ToDouble(value, span);
        }

        private static object NegateNumber(object value, ArtiSourceSpan span)
        {
            if (IsIntegral(value))
            {
                return -ToInt(value, span);
            }

            return -ToDouble(value, span);
        }

        private static bool ToBool(object value, bool defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            bool parsed;
            return bool.TryParse(ToText(value), out parsed) ? parsed : defaultValue;
        }

        private static string ToText(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string text = value as string;
            if (text != null)
            {
                return text;
            }

            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }

            if (IsNumeric(value))
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            RuntimeModule module = value as RuntimeModule;
            if (module != null)
            {
                return module.Info.PackageId;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string FormatValue(object value)
        {
            return value == null ? "null" : ToText(value);
        }

        private static string TrimLines(string value)
        {
            string normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] sourceLines = normalized.Split(new[] { '\n' }, StringSplitOptions.None);
            List<string> lines = new List<string>(sourceLines.Length);
            foreach (string line in sourceLines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 && lines.Count > 0 && lines[lines.Count - 1].Length == 0)
                {
                    continue;
                }

                lines.Add(trimmed);
            }

            while (lines.Count > 0 && lines[0].Length == 0)
            {
                lines.RemoveAt(0);
            }

            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines);
        }

        private static string Indent(string value, int level)
        {
            if (level <= 0)
            {
                return value ?? string.Empty;
            }

            string prefix = new string(' ', level * 2);
            string normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None);
            for (int index = 0; index < lines.Length; index++)
            {
                lines[index] = prefix + lines[index];
            }

            return string.Join("\n", lines);
        }

        private static string EscapeJson(string value)
        {
            StringBuilder result = new StringBuilder();
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    default:
                        if (character < ' ')
                        {
                            result.Append("\\u");
                            result.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            result.Append(character);
                        }

                        break;
                }
            }

            return result.ToString();
        }

        private static string EscapeMarkdown(string value)
        {
            StringBuilder result = new StringBuilder();
            foreach (char character in value ?? string.Empty)
            {
                if ("\\`*_{}[]()#+-.!".IndexOf(character) >= 0)
                {
                    result.Append('\\');
                }

                result.Append(character);
            }

            return result.ToString();
        }

        private static bool IsEnumerableValue(object value)
        {
            return value != null && !(value is string) && value is IEnumerable;
        }

        private static IEnumerable<object> Enumerate(object value, ArtiSourceSpan span)
        {
            if (value == null)
            {
                yield break;
            }

            string text = value as string;
            if (text != null)
            {
                foreach (char character in text)
                {
                    yield return character.ToString();
                }

                yield break;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                throw new RuntimeFault("Expected an iterable value.");
            }

            foreach (object item in enumerable)
            {
                yield return item;
            }
        }

        private void Touch(ArtiSourceSpan span)
        {
            _steps++;
            if (_steps > Math.Max(1, _context.Options.MaxSteps))
            {
                RuntimeError(span, "The maximum Arti execution step count was exceeded.");
            }
        }

        private void RuntimeError(ArtiSourceSpan span, string message)
        {
            RecordRuntimeError(span, message);
            throw new ExecutionStopException();
        }

        private void RecordRuntimeError(ArtiSourceSpan span, string message)
        {
            ReportError(4000, span, message);
            if (_context.Options.IncludeRuntimeErrorsInOutput)
            {
                if (_output.Length > 0 && _output[_output.Length - 1] != '\n')
                {
                    _output.Append('\n');
                }

                _output.Append("[Advanced RimTalk Arti error: ")
                    .Append(message ?? "unknown error")
                    .Append(']');
            }
        }

        private void AddDiagnostics(IEnumerable<ArtiDiagnostic> diagnostics)
        {
            if (diagnostics == null)
            {
                return;
            }

            foreach (ArtiDiagnostic diagnostic in diagnostics)
            {
                AddDiagnostic(diagnostic);
            }
        }

        private static bool HasErrors(IEnumerable<ArtiDiagnostic> diagnostics)
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

        private sealed class RuntimeScope
        {
            private readonly IDictionary<string, Binding> _bindings =
                new Dictionary<string, Binding>(StringComparer.Ordinal);

            public RuntimeScope(RuntimeScope parent)
            {
                Parent = parent;
            }

            public RuntimeScope Parent { get; }

            public void ImportFrom(IDictionary<string, object> values)
            {
                if (values == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, object> value in values)
                {
                    TryDeclare(value.Key, value.Value, false);
                }
            }

            public void ExportTo(IDictionary<string, object> values)
            {
                if (values == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, Binding> binding in _bindings)
                {
                    values[binding.Key] = binding.Value.Value;
                }
            }

            public bool TryDeclare(string name, object value, bool isConst)
            {
                return TryDeclare(name, value, isConst, false);
            }

            public bool TryDeclare(string name, object value, bool isConst, bool allowReplace)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }

                if (_bindings.ContainsKey(name))
                {
                    if (!allowReplace)
                    {
                        return false;
                    }

                    _bindings[name] = new Binding(value, isConst);
                    return true;
                }

                _bindings.Add(name, new Binding(value, isConst));
                return true;
            }

            public bool TryGet(string name, out object value)
            {
                Binding binding;
                if (_bindings.TryGetValue(name, out binding))
                {
                    value = binding.Value;
                    return true;
                }

                value = null;
                return Parent != null && Parent.TryGet(name, out value);
            }

            public bool TryAssign(string name, object value, out string error)
            {
                Binding binding;
                if (_bindings.TryGetValue(name, out binding))
                {
                    if (binding.IsConst)
                    {
                        error = "The name '" + name + "' cannot be assigned.";
                        return false;
                    }

                    binding.Value = value;
                    error = null;
                    return true;
                }

                if (Parent != null)
                {
                    return Parent.TryAssign(name, value, out error);
                }

                error = "The name '" + name + "' is not declared.";
                return false;
            }
        }

        private sealed class Binding
        {
            public Binding(object value, bool isConst)
            {
                Value = value;
                IsConst = isConst;
            }

            public object Value { get; set; }
            public bool IsConst { get; }
        }

        private sealed class RuntimeObject
        {
            private readonly IDictionary<string, object> _members =
                new Dictionary<string, object>(StringComparer.Ordinal);

            public void Set(string name, object value)
            {
                _members[name] = value;
            }

            public bool TryGet(string name, out object value)
            {
                return _members.TryGetValue(name, out value);
            }
        }

        private sealed class RuntimeModule
        {
            public RuntimeModule(ArtiModuleInfo info)
            {
                Info = info;
            }

            public ArtiModuleInfo Info { get; }

            public bool TryGet(string name, out object value)
            {
                switch (name)
                {
                    case "package_id":
                    case "packageId":
                        value = Info.PackageId;
                        return true;
                    case "installed":
                        value = Info.Installed;
                        return true;
                    case "active":
                        value = Info.Active;
                        return true;
                    case "api_available":
                    case "apiAvailable":
                        value = Info.ApiAvailable;
                        return true;
                    case "version":
                        value = Info.Version;
                        return true;
                    case "exist":
                    case "exists":
                        value = new RuntimeCallable(delegate(RuntimeArguments arguments)
                        {
                            return Info.Installed;
                        });
                        return true;
                    default:
                        value = null;
                        return false;
                }
            }
        }

        private sealed class RuntimeCallable : IArtiCallable
        {
            private readonly Func<RuntimeArguments, object> _invoke;

            public RuntimeCallable(Func<RuntimeArguments, object> invoke)
            {
                _invoke = invoke;
            }

            public object InvokeInternal(RuntimeArguments arguments)
            {
                return _invoke(arguments);
            }

            public object Invoke(IList<object> positionalArguments, IDictionary<string, object> namedArguments)
            {
                RuntimeArguments arguments = new RuntimeArguments();
                if (positionalArguments != null)
                {
                    foreach (object argument in positionalArguments)
                    {
                        arguments.Add(new RuntimeArgument(null, argument));
                    }
                }

                if (namedArguments != null)
                {
                    foreach (KeyValuePair<string, object> argument in namedArguments)
                    {
                        arguments.Add(new RuntimeArgument(argument.Key, argument.Value));
                    }
                }

                return InvokeInternal(arguments);
            }
        }

        private sealed class RuntimeArguments
        {
            private readonly List<RuntimeArgument> _items = new List<RuntimeArgument>();

            public IList<RuntimeArgument> Items { get { return _items; } }
            public IList<object> Positional
            {
                get
                {
                    List<object> values = new List<object>();
                    foreach (RuntimeArgument item in _items)
                    {
                        if (item.Name == null)
                        {
                            values.Add(item.Value);
                        }
                    }

                    return values;
                }
            }

            public void Add(RuntimeArgument argument)
            {
                _items.Add(argument);
            }

            public object Get(int position, string name, bool required)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (RuntimeArgument item in _items)
                    {
                        if (string.Equals(item.Name, name, StringComparison.Ordinal))
                        {
                            return item.Value;
                        }
                    }
                }

                int current = 0;
                foreach (RuntimeArgument item in _items)
                {
                    if (item.Name == null)
                    {
                        if (current == position)
                        {
                            return item.Value;
                        }

                        current++;
                    }
                }

                if (required)
                {
                    throw new RuntimeFault("Required argument '" + (name ?? position.ToString(CultureInfo.InvariantCulture)) + "' is missing.");
                }

                return null;
            }
        }

        private sealed class RuntimeArgument
        {
            public RuntimeArgument(string name, object value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }
            public object Value { get; }
        }

        private sealed class RuntimeFault : Exception
        {
            public RuntimeFault(string message)
                : base(message)
            {
            }
        }

        private sealed class ExecutionStopException : Exception
        {
        }

        private sealed class ReturnSignal : Exception
        {
            public ReturnSignal(object value)
            {
                Value = value;
            }

            public object Value { get; }
        }

        private sealed class BreakSignal : Exception
        {
        }

        private sealed class ContinueSignal : Exception
        {
        }
    }
}
