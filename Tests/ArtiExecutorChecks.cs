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
            ExecutesModulesAndRandomValues();
            ExecutesStringFunctionsAndMethods();
            ExtendsCoreThroughRuntimeProvider();
            RejectsRuntimeStepOverflow();
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

        private static void ExtendsCoreThroughRuntimeProvider()
        {
            ArtiExecutionResult result = new ArtiExecutor().Execute(
                "core.emit(core.world.name)",
                new ArtiExecutionContext(valueProvider: new TestCoreProvider()));

            Assert(!result.HasErrors, "runtime providers can extend the core module");
            AssertEqual("test-world", result.Output, "core extension members remain regular Arti values");
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

        private sealed class TestModuleCatalog : IArtiModuleCatalog
        {
            public bool TryGetModule(string packageId, out ArtiModuleInfo module)
            {
                module = string.Equals(packageId, "example.module", StringComparison.Ordinal)
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
