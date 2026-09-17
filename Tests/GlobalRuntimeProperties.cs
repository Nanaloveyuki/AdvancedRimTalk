using System;
using AdvancedRimTalk.Arti;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class GlobalRuntimeProperties
    {
        internal static void Run()
        {
            var rebinding = new ArtiGlobalRuntime();
            var context = new ArtiExecutionContext(symbolCatalog: new ArtiSymbolCatalog(new[] { "seed" }));
            context.Globals["seed"] = 1;
            const string capture = "let captured = seed; fn get() { return captured }";
            Require(!rebinding.Execute(capture, "a", context).HasErrors, 0, "first capture");
            Require(!rebinding.Execute("fn wrapper() { return get() }", "b").HasErrors, 0, "wrapper");
            context.Globals["seed"] = 2;
            Require(!rebinding.Execute(capture, "a", context).HasErrors, 0, "rebind capture");
            var rebound = rebinding.Execute("core.emit(get()); core.emit(wrapper())", "c");
            Require(!rebound.HasErrors && rebound.Output == "22", 0, "wrapper resolves current global binding");
            var shadow = new ArtiGlobalRuntime().Execute(
                "use optional sample.mod as lib; fn echo(lib) { return lib }; core.emit(echo(7))", "shadow");
            Require(!shadow.HasErrors && shadow.Output == "7", 0, "parameter shadows imported module");
            var retained = new ArtiGlobalRuntime();
            var references = RebindCapturedObjects(retained);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            int alive = 0;
            foreach (var reference in references) if (reference.IsAlive) alive++;
            Require(alive <= 2, alive, "rebindings do not retain historical closure environments");
            GC.KeepAlive(retained);

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
                Require(!runtime.Execute("let a = 1\nfn capture() { return a }", "capture").HasErrors, value, "local capture");
                result = runtime.Execute("core.emit(capture())", "next");
                Require(!result.HasErrors && result.Output == "1", value, "cross-block closure");
                Require(runtime.Execute("core.emit(a)", "next").HasErrors, value, "captured local stays private");
                Require(runtime.Execute("fn broken() { return undeclared }", "broken").HasErrors, value, "invalid definition");
                Require(runtime.Execute("broken()", "next").HasErrors, value, "failed definition not published");
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static WeakReference[] RebindCapturedObjects(ArtiGlobalRuntime runtime)
        {
            var references = new WeakReference[100];
            var context = new ArtiExecutionContext(symbolCatalog: new ArtiSymbolCatalog(new[] { "seed" }));
            for (int i = 0; i < references.Length; i++)
            {
                context.Globals["seed"] = new object();
                references[i] = new WeakReference(context.Globals["seed"]);
                Require(!runtime.Execute("let held = seed; fn first() { return held }", "first", context).HasErrors, i, "capture object");
                Require(!runtime.Execute("fn second() { return first() }", "second", context).HasErrors, i, "capture wrapper");
            }
            return references;
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
