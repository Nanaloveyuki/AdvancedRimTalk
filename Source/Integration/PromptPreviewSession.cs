using System;
using System.Collections.Generic;

namespace AdvancedRimTalk.Integration
{
    internal sealed class PromptPreviewSession : IDisposable
    {
        [ThreadStatic]
        private static Dictionary<string, object> variables;
        private readonly Dictionary<string, object> previous;
        private bool disposed;

        internal static Dictionary<string, object> Variables => variables;

        internal PromptPreviewSession()
        {
            previous = variables;
            variables = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            variables = previous;
        }
    }
}
