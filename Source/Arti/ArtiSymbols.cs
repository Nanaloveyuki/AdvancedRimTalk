using System;
using System.Collections.Generic;

namespace AdvancedRimTalk.Arti
{
    public interface IArtiSymbolCatalog
    {
        bool ContainsGlobal(string name);
    }

    public sealed class ArtiSymbolCatalog : IArtiSymbolCatalog
    {
        private readonly ISet<string> _globalNames;
        private readonly ISet<string> _knownPaths;

        public ArtiSymbolCatalog()
            : this(null)
        {
        }

        public ArtiSymbolCatalog(IEnumerable<string> globalNames)
        {
            _globalNames = new HashSet<string>(StringComparer.Ordinal);
            _knownPaths = new HashSet<string>(StringComparer.Ordinal);
            if (globalNames == null)
            {
                return;
            }

            foreach (string name in globalNames)
            {
                AddGlobal(name);
            }
        }

        public IEnumerable<string> GlobalNames
        {
            get { return _globalNames; }
        }

        public IEnumerable<string> KnownPaths
        {
            get { return _knownPaths; }
        }

        public void AddGlobal(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _globalNames.Add(name);
                _knownPaths.Add(name);
            }
        }

        public void AddPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _knownPaths.Add(path);
            int dot = path.IndexOf('.');
            int bracket = path.IndexOf('[');
            int end = path.Length;
            if (dot >= 0 && dot < end)
            {
                end = dot;
            }

            if (bracket >= 0 && bracket < end)
            {
                end = bracket;
            }

            if (end > 0)
            {
                AddGlobal(path.Substring(0, end));
            }
        }

        public bool ContainsGlobal(string name)
        {
            return !string.IsNullOrEmpty(name) && _globalNames.Contains(name);
        }

        public bool ContainsPath(string path)
        {
            return !string.IsNullOrEmpty(path) && _knownPaths.Contains(path);
        }
    }
}
