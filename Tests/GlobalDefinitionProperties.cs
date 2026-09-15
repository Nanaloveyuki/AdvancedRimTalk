using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedRimTalk.Arti;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class GlobalDefinitionProperties
    {
        internal static void Run()
        {
            var operations = new[]
            {
                Definition("one:0", "const a = 1"),
                Definition("two:0", "const a = 1"),
                Definition("one:0", "const a = 2"),
                Definition("one:20", "fn b() { return 3 }")
            };
            for (int trace = 0; trace < 1024; trace++)
            {
                var registry = new ArtiGlobalDefinitions();
                var model = new Dictionary<string, ArtiGlobalDefinition>();
                for (int step = 0, digits = trace; step < 5; step++, digits /= operations.Length)
                {
                    var operation = operations[digits % operations.Length];
                    bool conflict = model.TryGetValue(operation.Name, out var old)
                        && (old.Owner != operation.Owner || old.Source != operation.Source);
                    bool rejected = false;
                    try { registry.Register(new[] { operation }); }
                    catch (InvalidOperationException) { rejected = true; }
                    if (rejected != conflict) Fail(trace, step, "collision result");
                    if (!conflict) model[operation.Name] = operation;
                    var actual = registry.Snapshot();
                    if (actual.Length != model.Count || actual.Any(item =>
                        !model.TryGetValue(item.Name, out var expected) || item.Owner != expected.Owner || item.Source != expected.Source))
                        Fail(trace, step, "state differs from model");
                }
            }
            var atomic = new ArtiGlobalDefinitions();
            atomic.Register(new[] { operations[0] });
            try { atomic.Register(new[] { operations[3], operations[1] }); }
            catch (InvalidOperationException) { }
            if (atomic.Snapshot().Length != 1) throw new Exception("Failed registration batch partially committed.");
            Console.WriteLine("Global definition properties passed: 1024 operation traces.");
        }

        private static ArtiGlobalDefinition Definition(string owner, string source)
        {
            var parsed = new ArtiParser().Parse(source);
            if (parsed.HasErrors) throw new Exception("Invalid property fixture.");
            return new ArtiGlobalDefinition(owner, source, parsed.Program.Statements[0]);
        }

        private static void Fail(int trace, int step, string reason)
            => throw new Exception("Global definition property: trace=" + trace + "; step=" + step + "; " + reason);
    }
}
