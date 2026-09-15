using System;
using AdvancedRimTalk.Arti;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class GlobalRuntimeProperties
    {
        internal static void Run()
        {
            for (int value = -20; value <= 20; value++)
            {
                var runtime = new ArtiGlobalRuntime();
                string definition = "const n = " + value + "\nfn number() { return n }";
                Require(!runtime.Execute(definition, "library").HasErrors, value, "declaration");
                Require(!runtime.Execute(definition, "library").HasErrors, value, "idempotence");
                var result = runtime.Execute("core.emit(number())", "caller");
                Require(!result.HasErrors && result.Output == value.ToString(), value, "cross-block call");
                Require(runtime.Execute(definition, "other-library").HasErrors, value, "owner collision");
                Require(!runtime.Execute("let temporary = 1", "local").HasErrors, value, "local declaration");
                Require(runtime.Execute("core.emit(temporary)", "next").HasErrors, value, "local isolation");
                Require(runtime.Execute("let a = 1\nfn capture() { return a }", "capture").HasErrors, value, "no capture");
                Require(runtime.Execute("capture()", "next").HasErrors, value, "failed definition not published");
                var symbols = new ArtiSymbolCatalog(new[] { "dynamic_value" });
                var declarationContext = new ArtiExecutionContext(new DynamicProvider(999), symbolCatalog: symbols);
                Require(!runtime.Execute("fn current() { return dynamic_value }", "dynamic", declarationContext).HasErrors,
                    value, "dynamic function declaration");
                var callerContext = new ArtiExecutionContext(new DynamicProvider(value), symbolCatalog: symbols);
                result = runtime.Execute("core.emit(current())", "dynamic-caller", callerContext);
                Require(!result.HasErrors && result.Output == value.ToString(), value, "current call context, not captured declaration context");
            }
            Console.WriteLine("Global runtime properties passed: 41 values with cross-block operation traces.");
        }

        private static void Require(bool condition, int value, string property)
        {
            if (!condition) throw new Exception("Global runtime property failed: value=" + value + "; " + property);
        }

        private sealed class DynamicProvider : IArtiRuntimeValueProvider
        {
            private readonly int current;
            internal DynamicProvider(int current) { this.current = current; }
            public bool TryGetGlobal(string name, out object value) { value = current; return name == "dynamic_value"; }
            public bool TryGetMember(object target, string member, out object value) { value = null; return false; }
            public bool TryGetIndex(object target, object index, out object value) { value = null; return false; }
        }
    }
}
