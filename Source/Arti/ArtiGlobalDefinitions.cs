using System;
using System.Collections.Generic;

namespace AdvancedRimTalk.Arti
{
    public sealed class ArtiGlobalDefinitions
    {
        private readonly object sync = new object();
        internal object SyncRoot => sync;
        private readonly Dictionary<string, ArtiGlobalDefinition> definitions =
            new Dictionary<string, ArtiGlobalDefinition>(StringComparer.Ordinal);

        internal ArtiGlobalDefinition[] Snapshot()
        {
            lock (sync)
            {
                var result = new ArtiGlobalDefinition[definitions.Count];
                definitions.Values.CopyTo(result, 0);
                return result;
            }
        }

        internal void Register(IList<ArtiGlobalDefinition> batch)
        {
            lock (sync)
            {
                var pending = new Dictionary<string, ArtiGlobalDefinition>(StringComparer.Ordinal);
                foreach (ArtiGlobalDefinition definition in batch)
                {
                    ArtiGlobalDefinition previous;
                    if (pending.TryGetValue(definition.Name, out previous)
                        || definitions.TryGetValue(definition.Name, out previous))
                    {
                        if (previous.Owner != definition.Owner || previous.Source != definition.Source
                            || previous.Declaration.GetType() != definition.Declaration.GetType())
                            throw new InvalidOperationException("Global Arti name '" + definition.Name
                                + "' is already defined by '" + previous.Owner + "'; replacement is not allowed.");
                    }
                    else pending.Add(definition.Name, definition);
                }
                // Validate the whole batch before making any definition visible.
                foreach (var item in pending) definitions.Add(item.Key, item.Value);
                foreach (ArtiGlobalDefinition definition in batch)
                    if (definition.HasRuntimeValue) definitions[definition.Name] = definition;
            }
        }
    }

    internal sealed class ArtiGlobalDefinition
    {
        internal ArtiGlobalDefinition(string owner, string source, ArtiStatement declaration)
        {
            if (string.IsNullOrEmpty(owner)) throw new ArgumentException("A declaration owner is required.", nameof(owner));
            if (declaration is ArtiFunctionDeclarationStatement function) Name = function.Name;
            else if (declaration is ArtiVariableDeclarationStatement constant && constant.IsConst) Name = constant.Name;
            else throw new ArgumentException("Only functions and constants may be registered globally.", nameof(declaration));
            Owner = owner;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Declaration = declaration;
        }

        internal string Name { get; }
        internal string Owner { get; }
        internal string Source { get; }
        internal ArtiStatement Declaration { get; }
        internal object RuntimeValue { get; private set; }
        internal bool HasRuntimeValue { get; private set; }

        internal void Bind(object value)
        {
            RuntimeValue = value;
            HasRuntimeValue = true;
        }
    }
}
