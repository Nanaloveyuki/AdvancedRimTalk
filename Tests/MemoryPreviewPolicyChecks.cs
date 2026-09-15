using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Integration;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class MemoryPreviewPolicyChecks
    {
        internal static void Run()
        {
            var target = new MemoryValue();
            var callback = new Callback();
            foreach (string operation in new[] { "add", "update", "delete", "pin", "move", "setcontent", "addkeyword", "clear", "import", "summarize" })
            {
                IArtiCallable guarded = (IArtiCallable)MemoryPreviewPolicy.Guard(true, target, operation, callback);
                bool failed = false;
                try { guarded.Invoke(null, null); }
                catch (InvalidOperationException error) { failed = error.Message.Contains(operation); }
                if (!failed || callback.Count != 0) throw new Exception("Preview executed a write: " + operation);
                if (!ReferenceEquals(MemoryPreviewPolicy.Guard(false, target, operation, callback), callback))
                    throw new Exception("Normal execution was changed.");
            }
            var read = (IArtiCallable)MemoryPreviewPolicy.Guard(true, target, "list", callback);
            read.Invoke(null, null);
            if (callback.Count != 1) throw new Exception("Preview read was blocked.");
            if (!ReferenceEquals(MemoryPreviewPolicy.Guard(true, new object(), "add", callback), callback))
                throw new Exception("Non-memory callback was intercepted.");
        }

        private sealed class MemoryValue : IMemoryArtiValue { }
        private sealed class Callback : IArtiCallable
        {
            internal int Count;
            public object Invoke(IList<object> positionalArguments, IDictionary<string, object> namedArguments)
            {
                Count++;
                return null;
            }
        }
    }
}
