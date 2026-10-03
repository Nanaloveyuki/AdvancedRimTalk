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

        internal static object GetVariable(Dictionary<string, object> values, string key)
        {
            object value;
            return !string.IsNullOrEmpty(key) && values.TryGetValue(key.ToLowerInvariant(), out value)
                ? value : string.Empty;
        }

        internal static void SetVariable(Dictionary<string, object> values, string key, object value)
        {
            if (!string.IsNullOrEmpty(key)) values[key.ToLowerInvariant()] = value;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            variables = previous;
        }
    }
}
