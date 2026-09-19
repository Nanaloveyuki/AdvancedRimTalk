using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiExecutorChecks
    {
        public static void Run()
        {
            ExecutesStatementsAndFunctions();
            EmitsWithoutImplicitNewlines();
            UsesVariableCallbacks();
            SharesControlFlowScope();
            DetectsExistence();
            ReturnsAndUnpacksValues();
            InterpolatesStrings();
            ExecutesMultilineSyntax();
            CapturesPromptFunctionLocals();
            ExecutesModulesAndRandomValues();
            ExecutesRuntimeModuleValues();
            ExecutesStringFunctionsAndMethods();
            ExtendsCoreThroughRuntimeProvider();
            ExecutesMutableLet();
            PersistsVariablesBetweenExecutions();
            AllowsReplRedeclarationBetweenExecutions();
            RejectsRuntimeStepOverflow();
            RunsOncePerStoredId();
        }

        private static void ExecutesMultilineSyntax()
        {
            var cases = new[]
            {
                ("core.emit(\"\"\"first\n  single \" quote\nlast\"\"\")", "first\n  single \" quote\nlast"),
                ("core.emit('''first\n  single ' quote\nlast''')", "first\n  single ' quote\nlast"),
                ("core.emit('legacy\ntext')", "legacy\ntext"),
                ("core.emit(\"legacy\ntext\")", "legacy\ntext"),
                ("core.emit(f'''value: {1 +\n2}\nend''')", "value: 3\nend"),
                ("core.emit(\"\"\"a\r\nb\rc\"\"\")", "a\nb\nc"),
                ("core.emit('a\\\nb')", "ab"),
                ("let n = 1 + \\\r\n2; core.emit(n)", "3"),
                ("core.emit((\n1\n+\n2 // comment\n))", "3"),
                ("core.emit(\n[\n1,\n2,\n][\n1\n],\nnewline\n:\nfalse,\n)", "2"),
                ("core.emit({\nvalue:\n1\n+2,\n}.value)", "3"),
                ("fn pair(\na,\nb,\n) {\nlet sum = a+b\nreturn (\ntrue,\nf'value {sum}',\n)\n}\nlet ok, text = pair(\n1,\n2,\n)\ncore.emit(f'{ok}:{text}')", "true:value 3"),
                ("core.emit((\n'hello '\nf'{1 + 2}'\n'!'\n))", "hello 3!"),
                ("if true {\nlet n = 1\nn += 2\ncore.emit(n)\n}", "3")
            };
            foreach (var test in cases)
            {
                var result = new ArtiExecutor().Execute(test.Item1);
                Assert(!result.HasErrors && result.Output == test.Item2,
                    "multiline: " + test.Item1 + " => " + result.Output + "; " + string.Join(";", result.Diagnostics));
            }
            foreach (string source in new[] { "core.emit('''missing)", "core.emit((1 +\n))", "fn bad(\nx,\n { return x }" })
                Assert(new ArtiExecutor().Execute(source).HasErrors, "malformed multiline syntax rejected");

            var repl = new ArtiExecutor();
            var context = new ArtiExecutionContext(options: new ArtiExecutionOptions { PersistVariables = true, AllowGlobalRedeclare = true });
            Assert(!repl.Execute("const base = 1", context).HasErrors, "REPL constant declaration");
            Assert(!repl.Execute("const next = base + 1", context).HasErrors, "REPL imported constant expression");
            Assert(repl.Execute("base = 2", context).HasErrors, "REPL imported constant assignment");
            Assert(!repl.Execute("let base = 3", context).HasErrors, "REPL replaces constant with mutable variable");
            Assert(!repl.Execute("base = 4", context).HasErrors, "REPL replacement is mutable");
            Assert(repl.Execute("const invalid = base", context).HasErrors, "REPL replacement loses constant metadata");
        }

        private static void CapturesPromptFunctionLocals()
        {
            string library = @"
fn mod_status(mod_id) {
    let mod_instance = core.mod(mod_id)
    if mod_instance.active { return true, f""Mod: {mod_id} active."" }
    else if mod_instance.installed { return false, f""Mod: {mod_id} inactive."" }
    else { return false, f""Mod: {mod_id} not found."" }
}
let kiiro = ""Ancot.KiiroRace""
let ratkin = ""Solaris.RatkinRaceMod""
let wolfein = ""MelonDove.WolfeinRace""
fn check_kiiro() { let check_kiiro, _ = mod_status(kiiro); return check_kiiro }
fn check_ratkin() { let check_ratkin, _ = mod_status(ratkin); return check_ratkin }
fn check_wolfein() { let check_wolfein, _ = mod_status(wolfein); return check_wolfein }
";
            foreach (string active in new[] { "", "Ancot.KiiroRace", "Solaris.RatkinRaceMod", "MelonDove.WolfeinRace" })
            {
                var runtime = new ArtiGlobalRuntime();
                // Calls must use their own module catalog, not the declaration context.
                var defined = runtime.Execute(library, "intro", new ArtiExecutionContext(moduleCatalog: new RaceModuleCatalog("")));
                Assert(!defined.HasErrors, "user helper library registers: " + string.Join(";", defined.Diagnostics));
                var caller = new ArtiExecutionContext(moduleCatalog: new RaceModuleCatalog(active));
                var result = runtime.Execute("let enable = false; if check_kiiro() { enable = true } "
                    + "else if check_ratkin() { enable = true } else if check_wolfein() { enable = true }; "
                    + "if enable { core.emit(\"小爪子\") }", "rules", caller);
                Assert(!result.HasErrors && result.Output == (active == "" ? "" : "小爪子"),
                    "user cross-part rule executes: " + active + " / " + string.Join(";", result.Diagnostics));
                Assert(runtime.Execute("core.emit(kiiro)", "private").HasErrors, "captured race IDs do not become globals");
                result = runtime.Execute("if check_kiiro { core.emit(\"wrong\") }", "uncalled", caller);
                Assert(result.HasErrors, "function value is not implicitly true or auto-called");
            }

            var state = new ArtiGlobalRuntime();
            Assert(!state.Execute("let count = 0; let saved_emit = core.emit; "
                + "fn next_count() { count += 1; return count }; fn get_count() { return count }; "
                + "fn write_count() { saved_emit(count) }", "state").HasErrors, "register shared mutable closure");
            var next = state.Execute("core.emit(next_count()); core.emit(get_count()); write_count()", "call-one");
            Assert(!next.HasErrors && next.Output == "111", "sibling closures share bindings and current output");
            next = state.Execute("core.emit(next_count()); write_count()", "call-two");
            Assert(!next.HasErrors && next.Output == "22", "closure state survives later caller blocks");
            Assert(!state.Execute("let saved_core = core; fn read_saved_core() { return saved_core.world.name }", "core-alias").HasErrors,
                "capture core alias");
            next = state.Execute("core.emit(read_saved_core())", "core-caller", new ArtiExecutionContext(valueProvider: new TestCoreProvider()));
            Assert(!next.HasErrors && next.Output == "test-world", "captured core alias resolves current provider");
            Assert(state.Execute("fn failed_capture() { return count }; let count = 0", "bad-order").HasErrors,
                "declaration-order analysis remains explicit");
            Assert(state.Execute("fn unpublished() { return 1 }; let x = 1 / 0", "failed").HasErrors,
                "execution failure rejects publication");
            Assert(state.Execute("unpublished()", "after-failure").HasErrors, "failed closure not published");
            Assert(!state.Execute("const first = 2", "constant-one").HasErrors, "first constant");
            Assert(!state.Execute("const second = first + 1", "constant-two").HasErrors, "cross-block constant expression");
            next = state.Execute("core.emit(second)", "constant-call");
            Assert(!next.HasErrors && next.Output == "3", "constant value imported without replay");
            var hoisted = new ArtiGlobalRuntime();
            next = hoisted.Execute("fn read_later() { return later_constant }; const later_constant = 4; core.emit(read_later())", "hoisted");
            Assert(!next.HasErrors && next.Output == "4", "global constant hoisting remains supported");
            Assert(hoisted.Execute("later_constant = 5", "immutable").HasErrors, "imported constants remain immutable");

            var refresh = new ArtiGlobalRuntime();
            string capture = "let captured = dynamic_value; fn read_capture() { return captured }";
            var symbols = new ArtiSymbolCatalog(new[] { "dynamic_value" });
            foreach (int number in new[] { 1, 2 })
            {
                var context = new ArtiExecutionContext(symbolCatalog: symbols);
                context.Globals["dynamic_value"] = number;
                Assert(!refresh.Execute(capture, "same-library", context).HasErrors, "idempotent library refresh");
                next = refresh.Execute("core.emit(read_capture())", "read");
                Assert(!next.HasErrors && next.Output == number.ToString(), "refresh binds successful new closure");
            }
        }

        private sealed class RaceModuleCatalog : IArtiModuleCatalog
        {
            private readonly string active;
            internal RaceModuleCatalog(string active) { this.active = active; }
            public bool TryGetModule(string packageId, out ArtiModuleInfo module)
            {
                module = new ArtiModuleInfo(packageId, packageId == active, packageId == active);
                return true;
            }
        }

        private static void InterpolatesStrings()
        {
            AssertOutput("let value = \"world\"; core.emit(f\"hello {value}\")", "hello world", "interpolate name");
            AssertOutput("fn name() { return \"player\" }; core.emit(f\"hello {name()}\")", "hello player", "interpolate call");
            AssertOutput("let n = 0; fn next() { n += 1; return n }; core.emit(f\"{next()}:{next()}\"); core.emit(n)", "1:22", "ordered single evaluation");
            AssertOutput("let data = { name: \"玩家\" }; core.emit(f\"{data.name} {1 + 2} {false}\")", "玩家 3 false", "Arti expression interpolation");
            AssertOutput("let value = 3; core.emit(f\"{{{value}}}\\n\")", "{3}\n", "literal braces and escapes");
            AssertOutput("core.emit(f\"plain\"); core.emit(f\"\"); core.emit(\"{plain}\")", "plain{plain}", "ordinary strings unchanged");
            AssertOutput("core.emit(f'{true}:{null}')", "true:", "native text conversion");
            AssertOutput("fn echo(x) { return x }; core.emit(f\"{echo(\"inside\")}\")", "inside", "quoted call argument");
            AssertOutput("const n = 1; const text = f\"n={n}\"; core.emit(text)", "n=1", "constant interpolation");
            AssertOutput("fn data() { return true, f\"{1 + 2}\" }; let status, text = data(); core.emit(f\"{status}:{text}\")", "true:3", "interpolated multiple return");
            foreach (string source in new[] { "core.emit(f\"{missing}\")", "core.emit(f\"{1:04}\")",
                "core.emit(f\"{1!r}\")", "core.emit(f\"{}\")", "core.emit(f\"}\")",
                "core.emit(f\"{value\")", "core.emit(f\"unterminated", "core.emit(f\"{1 / 0}\")",
                "fn f() { return 1 }; const value = f\"{f()}\"" })
                Assert(new ArtiExecutor().Execute(source).HasErrors, "invalid interpolation rejected: " + source);

            string code = "core.emit(f\"hello {missing}\")";
            var parsed = new ArtiParser().Parse(code, 40, 3, 5);
            var diagnostics = new ArtiAnalyzer().Analyze(parsed.Program).Diagnostics;
            Assert(diagnostics.Count == 1 && diagnostics[0].Code == "ART3009"
                && diagnostics[0].Span.StartOffset == 40 + code.IndexOf("missing", StringComparison.Ordinal)
                && diagnostics[0].Span.Line == 3
                && diagnostics[0].Span.Column == 5 + code.IndexOf("missing", StringComparison.Ordinal), "interpolation source spans");
        }

        private static void ReturnsAndUnpacksValues()
        {
            const string function = "fn pair() { return true, \"ready\" }; ";
            AssertOutput(function + "let status, text = pair(); core.emit(status); core.emit(text)", "trueready", "declare multiple results");
            AssertOutput(function + "let status = false; status, _ = pair(); core.emit(status); core.emit(_.exists())", "truefalse", "assign and discard");
            AssertOutput(function + "let _, text = pair(); core.emit(text)", "ready", "discard first result");
            AssertOutput(function + "let _, _ = pair(); _, _ = pair(); _ = pair(); core.emit(_.exists())", "false", "repeated discard has no binding");
            AssertOutput(function + "let result = pair(); core.emit(result[0]); core.emit(result[1])", "trueready", "capture aggregate result");
            AssertOutput("let a, b = 1, 2; a, b = b, a; core.emit(a); core.emit(b)", "21", "parallel assignment");
            AssertOutput("fn pair() { return (false, \"no\") }; let status, text = pair(); core.emit(status); core.emit(text)", "falseno", "parenthesized results");
            AssertOutput("fn forward() { return pair() }; " + function + "let a, b = forward(); core.emit(a); core.emit(b)", "trueready", "forward multiple results");
            AssertOutput("fn use_first(value, _, _) { return value }; core.emit(use_first(7, 8, 9))", "7", "discard parameters");
            AssertOutput("let n = 0; fn pair() { n += 1; return n, \"x\" }; let a, _ = pair(); core.emit(n)", "1", "evaluate RHS once");
            AssertOutput("fn pair(x) { return x, x + 1 }; for i in [1, 2] { let a, b = pair(i) }; core.emit(a); core.emit(b)", "23", "unpack in repeated loop");
            AssertOutput("fn no_value() { return }; fn one() { return 7 }; core.emit(no_value() == null); core.emit(one())", "true7", "existing returns unchanged");
            foreach (string source in new[] { "let a, b = [1]", "let a, b = [1, 2, 3]", "let a, b = true",
                "let a, a = [1, 2]", "missing, _ = [1, 2]", "const a = 1; a, _ = [2, 3]",
                "let a = 1; a, _ += [1, 2]", "let a = [1]; a[0], _ = [2, 3]",
                "let a, b = [1, 2]; core.emit(_)", "fn ignored(_) { return _ }", "_ += 1",
                "fn ignored(_, _) { return 1 }; ignored(1)", "fn ignored(_) { return 1 }; ignored(_: 1)" })
                Assert(new ArtiExecutor().Execute(source).HasErrors, "invalid unpack rejected: " + source);

            foreach (string assignment in new[] { "a, b = [1]", "a, fixed_value = [1, 2]", "let fresh, fixed_value = [1, 2]" })
            {
                var context = new ArtiExecutionContext(options: new ArtiExecutionOptions { PersistVariables = true });
                // Exercise runtime validation independently of the analyzer.
                var result = new ArtiExecutor().Execute(new ArtiParser().Parse(
                    "let a = 7; let b = 8; const fixed_value = 9; " + assignment).Program, context);
                Assert(result.HasErrors && Convert.ToInt64(context.Globals["a"]) == 7
                    && Convert.ToInt64(context.Globals["b"]) == 8 && !context.Globals.ContainsKey("fresh"),
                    "invalid unpack does not partially assign: " + assignment);
            }

            var runtime = new ArtiGlobalRuntime();
            Assert(!runtime.Execute(function, "intro").HasErrors, "register multi-return function");
            var call = runtime.Execute("let status, _ = pair(); core.emit(status)", "system");
            Assert(!call.HasErrors && call.Output == "true", "multi-return works across prompt blocks");
        }

        private static void SharesControlFlowScope()
        {
            foreach (string language in new[] { "简体中文", "繁體中文", "English" })
            {
                string code = "let lang = \"" + language + "\"\n"
                    + "if lang == \"简体中文\" { let language = \"必须使用通俗白话\" } "
                    + "else if lang == \"繁體中文\" { let language = \"推荐使用通俗白话\" }\n"
                    + "if language.exists() { core.emit(language) }";
                AssertOutput(code, language == "简体中文" ? "必须使用通俗白话"
                    : language == "繁體中文" ? "推荐使用通俗白话" : "", "conditional declaration");
            }
            AssertOutput("let x = 1\nif true { let x = 2 }\ncore.emit(x)", "2", "no block shadowing");
            AssertOutput("if true { if true { let x = 3 } }\ncore.emit(x)", "3", "nested branches");
            AssertOutput("{ let x = 4 }\ncore.emit(x)", "4", "plain block");
            AssertOutput("for item in [1, 2] { let x = item; const c = 3 }\ncore.emit(x + item + c)", "7", "loop declarations reinitialize");
            AssertOutput("let i = 0\nwhile i < 3 { let x = i; i += 1 }\ncore.emit(x)", "2", "while scope");
            AssertOutput("for item in [] { let x = item }\ncore.emit(exists(item)); core.emit(exists(x))", "falsefalse", "empty loop");
            AssertOutput("for item in [1, 2] { let x = item; continue }\ncore.emit(x)", "2", "continue retains declarations");
            AssertOutput("while true { let x = 1; break }\ncore.emit(x)", "1", "break retains declarations");
            AssertOutput("let x = 1\nfn f() { if true { let x = 2; let hidden = 3 }; return x }\ncore.emit(f()); core.emit(x); core.emit(hidden.exists())", "21false", "function isolation");
            AssertOutput("let x = 1\nfn f() { return x }\nif true { let x = 2; core.emit(f()) }", "2", "closures see control-flow updates");
            foreach (string code in new[] { "let x = 1; let x = 2", "if true { let x = 1; let x = 2 }",
                "const x = 1; if true { let x = 2 }", "if true { const x = 1 }; x = 2",
                "fn f() { let hidden = 1 }; f(); core.emit(hidden)" })
                Assert(new ArtiExecutor().Execute(code).HasErrors, "invalid declaration remains rejected: " + code);
        }

        private static void DetectsExistence()
        {
            string setup = "let x = null; let data = { name: \"a\", empty: null }; let items = [null, 2]; ";
            foreach (string expression in new[] { "x", "data", "data.name", "data.empty", "data[\"empty\"]",
                "items[0]", "items[1]", "null", "false", "0", "\"\"", "[]", "{}", "core.emit" })
            {
                AssertOutput(setup + "core.emit(exists(" + expression + "))", "true", "exists value: " + expression);
                AssertOutput(setup + "core.emit((" + expression + ").exists())", "true", "inverted exists: " + expression);
            }
            foreach (string expression in new[] { "missing", "missing.child", "data.missing", "data[\"missing\"]",
                "items[-1]", "items[5]", "x.child", "missing[0].child" })
            {
                AssertOutput(setup + "core.emit(exists(" + expression + "))", "false", "missing value: " + expression);
                AssertOutput(setup + "core.emit((" + expression + ").exists())", "false", "inverted missing: " + expression);
            }
            AssertOutput("core.emit(exists(value: missing)); core.emit(exists(value: null))", "falsetrue", "named exists");
            AssertOutput("let check = exists; core.emit(check(42)); core.emit(check(null))", "truetrue", "first-class exists");
            AssertOutput("fn exists(x) { return false }; core.emit(exists(1))", "false", "local function precedence");
            AssertOutput("fn custom() { return false }; let data = { exists: custom }; core.emit(data.exists())", "false", "member callable precedence");
            AssertOutput("let m = core.mod(\"missing\"); core.emit(exists(m)); core.emit(m.exists())", "truefalse", "module compatibility");
            AssertOutput("let calls = 0; fn f() { calls += 1; return null }; core.emit(exists(f())); core.emit(f().exists()); core.emit(calls)", "truetrue2", "single evaluation");
            var supplied = new ArtiExecutor().Execute("core.emit(exists(ambient)); core.emit(ambient.exists()); core.emit(exists(absent))",
                new ArtiExecutionContext(valueProvider: new ExistenceProvider()));
            Assert(!supplied.HasErrors && supplied.Output == "truetruefalse", "provider presence is distinct from null");
            var failed = new ArtiExecutor().Execute("core.emit(exists(broken))",
                new ArtiExecutionContext(valueProvider: new ExistenceProvider()));
            Assert(failed.HasErrors, "existence checks report provider failures");
            foreach (string code in new[] { "exists()", "exists(1, 2)", "1.exists(2)", "exists(other: 1)",
                "let items = []; exists(items[unknown])", "fn f() { return 1 / 0 }; exists(f())" })
                Assert(new ArtiExecutor().Execute(code).HasErrors, "exists does not hide invalid calls: " + code);
        }

        private static void AssertOutput(string source, string expected, string name)
        {
            var result = new ArtiExecutor().Execute(source);
            Assert(!result.HasErrors, name + ": " + string.Join(" | ", result.Diagnostics));
            AssertEqual(expected, result.Output, name);
        }

        private static void ExecutesStatementsAndFunctions()
        {
            string source = @"
use core
fn plus_one(value) {
    return value + 1
}

let result = plus_one(2)
for item in [""a"", ""b""] {
    core.emit(item)
}
if result == 3 && result > 2 {
    core.emit(""!"", newline: true)
}
";

            ArtiExecutionResult result = new ArtiExecutor().Execute(
                source,
                new ArtiExecutionContext(
                    options: new ArtiExecutionOptions
                     {
                         IncludeRuntimeErrorsInOutput = true,
                         MaxSteps = 100
                     }));

            Assert(!result.HasErrors, "executor accepts valid statements");
            AssertEqual("ab!\n", result.Output, "executor preserves statement order and named arguments");
        }

        private static void EmitsWithoutImplicitNewlines()
        {
            ArtiExecutionResult result = new ArtiExecutor().Execute(
                "core.emit(true)\ncore.emit(false)\ncore.emit(\"x\")",
                new ArtiExecutionContext());

            Assert(!result.HasErrors, "executor emits literal booleans");
            AssertEqual("truefalsex", result.Output, "core.emit only adds a newline when newline is explicitly true");
        }

        private static void UsesVariableCallbacks()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
            ArtiExecutionContext context = new ArtiExecutionContext();
            context.GetVariable = delegate(string key)
            {
                string value;
                return values.TryGetValue(key, out value) ? value : string.Empty;
            };
            context.SetVariable = delegate(string key, string value)
            {
                values[key] = value;
            };

            ArtiExecutionResult result = new ArtiExecutor().Execute(
                "setvar(\"mood\", \"calm\")\ncore.emit(getvar(\"mood\"))",
                context);

            Assert(!result.HasErrors, "executor invokes variable callbacks");
            AssertEqual("calm", result.Output, "executor forwards getvar and setvar");
        }

        private static void ExecutesModulesAndRandomValues()
        {
            string source = @"
let module = core.mod(""example.module"")
if module.active {
    core.emit(core.random.int(1, 6))
}
";

            ArtiExecutionResult result = new ArtiExecutor().Execute(
                source,
                new ArtiExecutionContext(
                    moduleCatalog: new TestModuleCatalog(),
                    random: new SequenceRandom(4)));

            Assert(!result.HasErrors, "executor resolves module status");
            AssertEqual("4", result.Output, "executor uses the prompt random source");
        }

        private static void ExecutesRuntimeModuleValues()
        {
            string source = @"
use sample.memory as memory
core.emit(memory.enabled)
core.emit(memory.echo(""ok""))
";

            ArtiExecutionResult result = new ArtiExecutor().Execute(
                source,
                new ArtiExecutionContext(
                    valueProvider: new TestRuntimeModuleProvider(),
                    moduleCatalog: new TestModuleCatalog()));

            Assert(!result.HasErrors, "executor resolves provider-backed module values");
            AssertEqual("trueok", result.Output, "runtime module values expose callable members");
        }

        private static void ExtendsCoreThroughRuntimeProvider()
        {
            ArtiExecutionResult result = new ArtiExecutor().Execute(
                "core.emit(core.world.name)",
                new ArtiExecutionContext(valueProvider: new TestCoreProvider()));

            Assert(!result.HasErrors, "runtime providers can extend the core module");
            AssertEqual("test-world", result.Output, "core extension members remain regular Arti values");
        }

        private static void ExecutesMutableLet()
        {
            ArtiExecutionResult result = new ArtiExecutor().Execute(
                "let value = 1\nvalue += 2\ncore.emit(value)",
                new ArtiExecutionContext());

            Assert(!result.HasErrors, "let bindings can be reassigned");
            AssertEqual("3", result.Output, "let reassignment keeps the updated value");
        }

        private static void ExecutesStringFunctionsAndMethods()
        {
            string source = @"
let normal = len(""1"")
let method = ""1"".len()
let module = core.string.len(""123"")
let normal_append = append(""a"", ""b"")
let method_append = ""a"".append(""b"")
let named_prepend = prepend(value: ""a"", prefix: ""b"")
let chain = "" hello world "".trim().upper().lower().capitalize().title()
let compact = remove_space("" a  b "")
let replaced = ""a-b"".replace(""-"", ""_"")
core.emit(normal)
core.emit(""|"")
core.emit(method)
core.emit(""|"")
core.emit(module)
core.emit(""|"")
core.emit(normal_append)
core.emit(""|"")
core.emit(method_append)
core.emit(""|"")
core.emit(named_prepend)
core.emit(""|"")
core.emit(chain)
core.emit(""|"")
core.emit(compact)
core.emit(""|"")
core.emit(replaced)
";

            ArtiExecutionResult result = new ArtiExecutor().Execute(source, new ArtiExecutionContext());

            Assert(!result.HasErrors, "string functions and inverted methods execute");
            AssertEqual("1|1|3|ab|ab|ba|Hello World|ab|a_b", result.Output, "string functions share normal and method behavior");
        }

        private static void RejectsRuntimeStepOverflow()
        {
            ArtiExecutionResult result = new ArtiExecutor().Execute(
                "while true { }",
                new ArtiExecutionContext(
                    options: new ArtiExecutionOptions
                    {
                        MaxSteps = 8,
                        IncludeRuntimeErrorsInOutput = true
                    }));

            Assert(result.HasErrors, "executor stops an unbounded loop");
            Assert(result.Diagnostics[0].Code == "ART4000", "runtime errors use the runtime diagnostic code");
            Assert(result.Output.Contains("Advanced RimTalk Arti error"), "runtime errors remain visible");
        }

        private static void RunsOncePerStoredId()
        {
            var store = new MemoryArtiOnceStore();
            var context = new ArtiExecutionContext { OnceStore = store, OnceScope = "world:a", OnceOwner = "test" };
            string source = "let n = 0\nfn bump() { n += 1 }\ncore.emit(core.once(\"seed\", bump))\ncore.emit(core.once(\"seed\", bump))\ncore.emit(n)\ncore.emit(core.once.done(\"seed\"))";
            ArtiExecutionResult first = new ArtiExecutor().Execute(source, context);
            Assert(!first.HasErrors && first.Output == "truefalse1true", "once runs the first time and skips later: " + first.Output);
            Assert(store.Snapshot().Count == 1 && store.Snapshot()[0].Action == "bump", "once stores the function name");

            var preview = new ArtiExecutionContext { OnceStore = store, OnceScope = "world:a", OncePreview = true };
            ArtiExecutionResult previewResult = new ArtiExecutor().Execute(
                "let n = 0\nfn bump() { n += 1 }\ncore.emit(core.once(\"other\", bump))\ncore.emit(n)", preview);
            Assert(!previewResult.HasErrors && previewResult.Output == "false0", "preview skips once without running: " + previewResult.Output);
            Assert(store.Snapshot().Count == 1, "preview does not record");

            var otherWorld = new ArtiExecutionContext { OnceStore = store, OnceScope = "world:b" };
            ArtiExecutionResult secondWorld = new ArtiExecutor().Execute(
                "fn bump() { }\ncore.emit(core.once(\"seed\", bump))", otherWorld);
            Assert(!secondWorld.HasErrors && secondWorld.Output == "true", "once is scoped per world");

            store.Remove("world:a", "seed");
            ArtiExecutionResult rerun = new ArtiExecutor().Execute(
                "fn bump() { }\ncore.emit(core.once(\"seed\", bump))", context);
            Assert(!rerun.HasErrors && rerun.Output == "true", "deleting a record re-enables once");

            ArtiExecutionResult failed = new ArtiExecutor().Execute(
                "fn boom() { return 1 / 0 }\ncore.once(\"broken\", boom)", new ArtiExecutionContext { OnceStore = store, OnceScope = "world:a" });
            Assert(failed.HasErrors && !store.Contains("world:a", "broken"), "failed once calls are not recorded");

            ArtiExecutionResult missingId = new ArtiExecutor().Execute(
                "fn bump() { }\ncore.once(\"\", bump)", new ArtiExecutionContext { OnceStore = store });
            Assert(missingId.HasErrors, "empty once id is rejected");
        }

        private static void PersistsVariablesBetweenExecutions()
        {
            ArtiExecutionContext context = new ArtiExecutionContext(
                options: new ArtiExecutionOptions
                {
                    PersistVariables = true
                });
            ArtiExecutor executor = new ArtiExecutor();

            ArtiExecutionResult first = executor.Execute(
                "use core\nlet total = 2\nfn double(value) { return value * 2 }",
                context);
            ArtiExecutionResult second = executor.Execute(
                "total += 3\ncore.emit(double(total))",
                context);

            Assert(!first.HasErrors, "persistent executor accepts the first submission");
            Assert(!second.HasErrors, "persistent executor resolves previous declarations");
            AssertEqual("10", second.Output, "persistent executor keeps variables and functions");

            ArtiExecutionContext providerContext = new ArtiExecutionContext(
                valueProvider: new TestCoreProvider(),
                options: new ArtiExecutionOptions
                {
                    PersistVariables = true
            });
            ArtiExecutor providerExecutor = new ArtiExecutor();
            ArtiExecutionResult providerFirst = providerExecutor.Execute(
                "use core\ncore.world.name",
                providerContext);
            ArtiExecutionResult providerResult = providerExecutor.Execute(
                "core.emit(core.world.name)",
                providerContext);

            Assert(!providerFirst.HasErrors, "persistent executor keeps the first provider-backed submission valid");
            Assert(!providerResult.HasErrors, "persistent executor keeps provider-backed core members");
            AssertEqual("test-world", providerResult.Output, "persistent core bindings stay connected");
        }

        private static void AllowsReplRedeclarationBetweenExecutions()
        {
            ArtiExecutionContext context = new ArtiExecutionContext(
                options: new ArtiExecutionOptions
                {
                    PersistVariables = true,
                    AllowGlobalRedeclare = true
                });
            ArtiExecutor executor = new ArtiExecutor();

            ArtiExecutionResult first = executor.Execute("use core\nlet a = \"one\"", context);
            ArtiExecutionResult second = executor.Execute("let a = \"two\"\ncore.emit(a)", context);
            ArtiExecutionResult third = executor.Execute("core.emit(a)", context);

            Assert(!first.HasErrors, "REPL redeclare accepts the first submission");
            Assert(!second.HasErrors, "REPL redeclare updates previous variables");
            Assert(!third.HasErrors, "REPL redeclare keeps the updated variable");
            AssertEqual("two", second.Output, "REPL redeclare outputs the new value");
            AssertEqual("two", third.Output, "REPL redeclare persists the new value");

            ArtiExecutionResult duplicateInOneSubmission = executor.Execute(
                "let a = \"three\"\nlet a = \"four\"",
                context);
            Assert(
                duplicateInOneSubmission.HasErrors,
                "REPL redeclare still rejects duplicate declarations in one submission");
        }

        private sealed class TestModuleCatalog : IArtiModuleCatalog
        {
            public bool TryGetModule(string packageId, out ArtiModuleInfo module)
            {
                module = (string.Equals(packageId, "example.module", StringComparison.Ordinal)
                    || string.Equals(packageId, "sample.memory", StringComparison.Ordinal))
                    ? new ArtiModuleInfo(packageId, true, true)
                    : null;
                return module != null;
            }
        }

        private sealed class SequenceRandom : IArtiRandomSource
        {
            private readonly int _integer;

            public SequenceRandom(int integer)
            {
                _integer = integer;
            }

            public int NextInt(int minInclusive, int maxInclusive)
            {
                return _integer;
            }

            public double NextDouble(double minInclusive, double maxInclusive)
            {
                return minInclusive;
            }
        }

        private sealed class TestCoreProvider : IArtiRuntimeValueProvider, IArtiCoreModuleProvider
        {
            public bool TryGetGlobal(string name, out object value)
            {
                value = null;
                return false;
            }

            public bool TryGetMember(object target, string member, out object value)
            {
                value = null;
                return false;
            }

            public bool TryGetIndex(object target, object index, out object value)
            {
                value = null;
                return false;
            }

            public bool TryGetCoreMember(string member, out object value)
            {
                if (string.Equals(member, "world", StringComparison.Ordinal))
                {
                    value = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        { "name", "test-world" }
                    };
                    return true;
                }

                value = null;
                return false;
            }
        }

        private sealed class TestRuntimeModuleProvider : IArtiRuntimeValueProvider, IArtiRuntimeModuleProvider
        {
            public bool TryGetGlobal(string name, out object value)
            {
                value = null;
                return false;
            }

            public bool TryGetMember(object target, string member, out object value)
            {
                value = null;
                return false;
            }

            public bool TryGetIndex(object target, object index, out object value)
            {
                value = null;
                return false;
            }

            public bool TryGetModuleValue(string packageId, ArtiModuleInfo module, out object value)
            {
                value = string.Equals(packageId, "sample.memory", StringComparison.Ordinal)
                    ? (object)new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        { "enabled", true },
                        { "echo", new EchoCallable() }
                    }
                    : null;
                return value != null;
            }
        }

        private sealed class ExistenceProvider : IArtiRuntimeValueProvider
        {
            public bool TryGetGlobal(string name, out object value)
            {
                if (name == "broken") throw new InvalidOperationException("provider failure");
                value = null;
                return name == "ambient";
            }
            public bool TryGetMember(object target, string member, out object value) { value = null; return false; }
            public bool TryGetIndex(object target, object index, out object value) { value = null; return false; }
        }

        private sealed class EchoCallable : IArtiCallable
        {
            public object Invoke(IList<object> positionalArguments, IDictionary<string, object> namedArguments)
            {
                return positionalArguments != null && positionalArguments.Count > 0
                    ? positionalArguments[0]
                    : string.Empty;
            }
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
