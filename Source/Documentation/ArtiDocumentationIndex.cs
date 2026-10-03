using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;

namespace AdvancedRimTalk.Documentation
{
    internal sealed class ArtiDocumentationIndex
    {
        private readonly Dictionary<string, DocumentationEntry> routes = new Dictionary<string, DocumentationEntry>(StringComparer.Ordinal);
        private readonly Dictionary<string, DocumentationEntry> registeredRoutes = new Dictionary<string, DocumentationEntry>(StringComparer.Ordinal);
        private readonly List<Reference> references = new List<Reference>();
        private readonly DocumentationEntry context;
        private string cachedSource;

        internal ArtiDocumentationIndex(AdvancedRimTalkDocumentationCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            context = catalog.Find("data/context.md");
            foreach (DocumentationEntry entry in catalog.Entries)
            {
                string path = entry.RelativePath;
                if (!path.EndsWith(".md", StringComparison.Ordinal)) continue;
                string name = path.Substring(0, path.Length - 3);
                if (name.StartsWith("language/", StringComparison.Ordinal))
                    routes[name] = entry;
                else if (name.StartsWith("global/", StringComparison.Ordinal) && name != "global/index")
                    routes[name.Substring(7)] = entry;
                else if (name.StartsWith("core/string/", StringComparison.Ordinal))
                    routes[name.Replace('/', '.')] = entry;
                else if (name.StartsWith("memory/", StringComparison.Ordinal))
                    routes[(name.EndsWith("/index", StringComparison.Ordinal) ? name.Substring(0, name.Length - 6) : name).Replace('/', '.')] = entry;
                else if (name.StartsWith("data/", StringComparison.Ordinal) && name != "data/index")
                    registeredRoutes[name.Substring(5)] = entry;
            }

            Add(catalog, "core", "core/index.md");
            foreach (string name in new[] { "emit", "append", "emit_if", "mod", "packageid", "log", "once", "random", "value", "text", "escape", "diag", "string" })
                Add(catalog, "core." + name, "core/" + name + ".md");
            AddMembers(catalog, "random", new[] { "int", "float", "pick" });
            AddMembers(catalog, "value", new[] { "default", "coalesce" });
            AddMembers(catalog, "text", new[] { "join_nonempty", "trim_lines", "indent" });
            AddMembers(catalog, "escape", new[] { "xml_text", "json", "markdown" });
            AddMembers(catalog, "diag", new[] { "warn" });
            foreach (string name in ArtiStringFunctions.Names)
                Add(catalog, name, "core/string/" + name + ".md");
            foreach (string name in new[] { "game", "world", "find", "current", "maps", "map", "pawns", "pawn", "factions", "faction", "settlements", "sites", "caravans", "world_objects", "mods", "defs", "def", "thing", "things", "cell", "query", "read", "has", "keys" })
                Add(catalog, "core." + name, "data/" + name + ".md");
            Add(catalog, "core.engine", "data/find.md");
            Add(catalog, "core.data", "data/query.md");
            foreach (string name in new[] { "ctx", "settings", "prompt", "context", "pawn_context", "raw_prompt", "user_prompt", "json", "is_user", "is_from_user", "lang", "time", "hour", "day", "quadrum", "year", "season", "weather", "temperature", "wealth", "events", "GenDate" })
                Add(catalog, name, "data/context.md");
            foreach (string name in new[] { "pawn", "recipient", "pawns", "map", "game", "current", "world", "maps", "factions", "settlements", "sites", "caravans", "world_objects", "mods", "defs", "thing", "things", "cell", "query", "find", "chat" })
                Add(catalog, name, "data/" + name + ".md");
            Add(catalog, "Find", "data/find.md");
            Add(catalog, "PawnsFinder", "data/pawns.md");
            Add(catalog, "pawn.info", "data/pawn_info.md");
            Add(catalog, "recipient.info", "data/pawn_info.md");
            Add(catalog, "core.pawn.info", "data/pawn_info.md");
            Add(catalog, "core.recipient.info", "data/pawn_info.md");
            Add(catalog, "core.once.done", "core/once.md");
            foreach (string name in new[] { "array", "date", "html", "math", "object", "string", "timespan" })
                Add(catalog, name, "compatibility/scriban.md");
            Add(catalog, "regex", "regex.md");
            foreach (string name in new[] { "installed", "active", "available", "api_available", "package_id", "version" })
                Add(catalog, "memory." + name, "memory/use.md");
            foreach (string name in new[] { "active", "abm", "short_term", "situational", "scm", "event_log", "mid_term", "els", "long_term", "archive", "clpa" })
            {
                if (name != "active") Add(catalog, "memory." + name, "memory/layer.md");
                Add(catalog, "memory.pawn." + name, "memory/layer.md");
                foreach (string member in new[] { "list", "all", "count", "get", "add", "update", "remove", "delete", "pin", "unpin", "enable", "disable", "move" })
                {
                    Add(catalog, "memory." + name + "." + member, "memory/pawn/" + member + ".md");
                    Add(catalog, "memory.pawn." + name + "." + member, "memory/pawn/" + member + ".md");
                }
            }
        }

        internal DocumentationEntry Find(string source, int offset, IEnumerable<string> playerNames = null, IArtiSymbolCatalog registeredSymbols = null)
        {
            source = source ?? string.Empty;
            if (offset < 0 || offset >= source.Length) return null;
            if (!string.Equals(cachedSource, source, StringComparison.Ordinal)) Cache(source);
            int low = 0, high = references.Count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                Reference reference = references[middle];
                if (offset < reference.Span.StartOffset) high = middle - 1;
                else if (offset >= reference.Span.EndOffset) low = middle + 1;
                else
                {
                    if (reference.Suppressed) return null;
                    if (playerNames != null && reference.Root != null)
                        foreach (string name in playerNames)
                            if (string.Equals(name, reference.Root, StringComparison.Ordinal)) return null;
                    if (reference.Entry != null) return reference.Entry;
                    if (reference.StringMember) return null;
                    return reference.Root != null && registeredSymbols != null && registeredSymbols.ContainsGlobal(reference.Root)
                        ? reference.RegisteredEntry ?? context : null;
                }
            }
            return null;
        }

        private void Add(AdvancedRimTalkDocumentationCatalog catalog, string name, string path)
        {
            DocumentationEntry entry = catalog.Find(path);
            if (entry != null) routes[name] = entry;
        }

        private void AddMembers(AdvancedRimTalkDocumentationCatalog catalog, string module, IEnumerable<string> names)
        {
            foreach (string name in names) Add(catalog, "core." + module + "." + name, "core/" + name + ".md");
        }

        private DocumentationEntry Route(string path)
        {
            DocumentationEntry entry;
            if (routes.TryGetValue(path, out entry)) return entry;
            // Host object members share their root page; module members must have an indexed route.
            int dot = path.LastIndexOf('.');
            while (dot > 0)
            {
                string prefix = path.Substring(0, dot);
                if (routes.TryGetValue(prefix, out entry)
                    && (entry.RelativePath.StartsWith("data/", StringComparison.Ordinal)
                        || entry.RelativePath == "compatibility/scriban.md" || entry.RelativePath == "regex.md")) return entry;
                dot = path.LastIndexOf('.', dot - 1);
            }
            return null;
        }

        private void Cache(string source)
        {
            references.Clear();
            ArtiDocumentParseResult document = new ArtiDocumentParser().Parse(source);
            var globals = new Scope(null, 0);
            foreach (ArtiCodeBlock block in document.CodeBlocks)
            {
                ArtiParseResult parsed = block.ParseResult;
                if (parsed == null) continue;
                foreach (ArtiToken token in parsed.Tokens)
                {
                    string keyword = Keyword(token.Kind);
                    DocumentationEntry entry;
                    if (keyword != null && routes.TryGetValue("language/" + keyword, out entry))
                        references.Add(new Reference(token.Span) { Entry = entry });
                }
                var scope = new Scope(globals, block.BodySpan.StartOffset);
                VisitStatements(parsed.Program.Statements, scope, parsed.Tokens, true);
                if (!block.HasErrors)
                    foreach (ArtiStatement statement in parsed.Program.Statements)
                    {
                        if (statement is ArtiFunctionDeclarationStatement function)
                            globals.Add(function.Name, block.Span.EndOffset, null, false);
                        else if (statement is ArtiVariableDeclarationStatement constant && constant.IsConst)
                            globals.Add(constant.Name, block.Span.EndOffset, null, IsString(constant.Value, scope));
                    }
            }
            foreach (Reference reference in references)
            {
                if (reference.Path == null) continue;
                Binding binding = reference.Scope.Resolve(reference.Root, reference.Span.StartOffset);
                if (reference.StringMember)
                {
                    reference.Entry = Route("core.string." + reference.Path.Substring(reference.Path.LastIndexOf('.') + 1));
                    continue;
                }
                if (binding != null)
                {
                    if (binding.Module == null) { reference.Suppressed = true; continue; }
                    reference.Path = binding.Module + reference.Path.Substring(reference.Root.Length);
                }
                reference.Path = NormalizeMemory(reference.Path);
                reference.Entry = Route(reference.Path);
                registeredRoutes.TryGetValue(reference.Root, out DocumentationEntry registered);
                reference.RegisteredEntry = registered;
                // A use alias, even for an unknown package, must never borrow registered globals.
                if (reference.Entry == null && (binding != null || routes.ContainsKey(reference.Root))) reference.Suppressed = true;
            }
            references.Sort((left, right) => left.Span.StartOffset.CompareTo(right.Span.StartOffset));
            cachedSource = source;
        }

        private static string NormalizeMemory(string path)
        {
            const string alias = "memory.common_knowledge";
            if (path == alias || path.StartsWith(alias + ".", StringComparison.Ordinal))
                return "memory.knowledge" + path.Substring(alias.Length);
            const string self = "memory.memory";
            if (path == self) return "memory";
            if (path.StartsWith(self + ".", StringComparison.Ordinal)) return NormalizeMemory("memory" + path.Substring(self.Length));
            return path;
        }

        private static string Keyword(ArtiTokenKind kind)
        {
            switch (kind)
            {
                case ArtiTokenKind.Fn: return "function";
                case ArtiTokenKind.As: return "use";
                case ArtiTokenKind.In: return "for";
                case ArtiTokenKind.True: case ArtiTokenKind.False: case ArtiTokenKind.Null: return "literal";
                case ArtiTokenKind.Use: case ArtiTokenKind.Optional: case ArtiTokenKind.Group:
                case ArtiTokenKind.Const: case ArtiTokenKind.Let: case ArtiTokenKind.If:
                case ArtiTokenKind.Else: case ArtiTokenKind.For: case ArtiTokenKind.While:
                case ArtiTokenKind.Return: case ArtiTokenKind.Break: case ArtiTokenKind.Continue:
                    return kind.ToString().ToLowerInvariant();
                default: return null;
            }
        }

        private void VisitStatements(IList<ArtiStatement> statements, Scope scope, IList<ArtiToken> tokens, bool isRoot = false)
        {
            // Functions and module imports are prepared at entry by the executor.
            foreach (ArtiStatement statement in statements)
            {
                if (statement is ArtiFunctionDeclarationStatement function)
                    scope.Add(function.Name, scope.Start, null, false);
                else if (isRoot && statement is ArtiVariableDeclarationStatement constant && constant.IsConst)
                    scope.Add(constant.Name, scope.Start, null, IsString(constant.Value, scope));
                else if (statement is ArtiUseStatement use)
                {
                    string module = Module(use.PackageId);
                    scope.Add(use.Alias, scope.Start, module, false);
                    if (module != null)
                        foreach (ArtiToken token in tokens)
                        {
                            if (token.Span.StartOffset < use.Span.StartOffset) continue;
                            if (token.Span.EndOffset > use.Span.EndOffset || token.Kind == ArtiTokenKind.As) break;
                            if (token.Kind == ArtiTokenKind.Identifier)
                                references.Add(new Reference(token.Span) { Entry = Route(module) });
                        }
                }
            }
            foreach (ArtiStatement statement in statements)
            {
                if (statement is ArtiVariableDeclarationStatement variable)
                {
                    VisitExpression(variable.Value, scope, tokens);
                    scope.Add(variable.Name, variable.Span.EndOffset, null, IsString(variable.Value, scope));
                }
                else if (statement is ArtiUnpackDeclarationStatement unpack)
                {
                    VisitExpression(unpack.Value, scope, tokens);
                    foreach (ArtiExpression target in unpack.Targets.Items)
                        if (target is ArtiNameExpression name) scope.Add(name.Name, unpack.Span.EndOffset, null, false);
                }
                else if (statement is ArtiFunctionDeclarationStatement function)
                {
                    var inner = new Scope(scope, function.Body.Span.StartOffset);
                    foreach (string parameter in function.Parameters) inner.Add(parameter, inner.Start, null, false);
                    VisitStatements(function.Body.Statements, inner, tokens);
                }
                else if (statement is ArtiBlockStatement block)
                    VisitStatements(block.Statements, new Scope(scope, block.Span.StartOffset, true), tokens);
                else if (statement is ArtiIfStatement conditional)
                {
                    foreach (ArtiIfBranch branch in conditional.Branches)
                    {
                        VisitExpression(branch.Condition, scope, tokens);
                        VisitStatements(branch.Body.Statements, new Scope(scope, branch.Body.Span.StartOffset, true), tokens);
                    }
                    if (conditional.ElseBody != null)
                        VisitStatements(conditional.ElseBody.Statements, new Scope(scope, conditional.ElseBody.Span.StartOffset, true), tokens);
                }
                else if (statement is ArtiForStatement loop)
                {
                    VisitExpression(loop.Source, scope, tokens);
                    var inner = new Scope(scope, loop.Body.Span.StartOffset, true);
                    inner.Add(loop.VariableName, inner.Start, null, false);
                    VisitStatements(loop.Body.Statements, inner, tokens);
                }
                else if (statement is ArtiWhileStatement loopWhile)
                {
                    VisitExpression(loopWhile.Condition, scope, tokens);
                    VisitStatements(loopWhile.Body.Statements, new Scope(scope, loopWhile.Body.Span.StartOffset, true), tokens);
                }
                else if (statement is ArtiAssignmentStatement assignment)
                {
                    VisitExpression(assignment.Target, scope, tokens);
                    VisitExpression(assignment.Value, scope, tokens);
                    if (assignment.Target is ArtiNameExpression assigned)
                    {
                        Binding binding = scope.Resolve(assigned.Name, assigned.Span.StartOffset);
                        if (binding != null) scope.Add(assigned.Name, assignment.Span.EndOffset, null, false);
                    }
                }
                else if (statement is ArtiReturnStatement returned) VisitExpression(returned.Value, scope, tokens);
                else if (statement is ArtiExpressionStatement expression) VisitExpression(expression.Expression, scope, tokens);
            }
        }

        private void VisitExpression(ArtiExpression expression, Scope scope, IList<ArtiToken> tokens)
        {
            if (expression == null) return;
            if (expression is ArtiNameExpression name)
                references.Add(new Reference(name.Span) { Root = name.Name, Path = name.Name, Scope = scope });
            else if (expression is ArtiMemberExpression member)
            {
                VisitExpression(member.Target, scope, tokens);
                ArtiSourceSpan span = MemberSpan(member, tokens);
                string path = Path(member);
                if (IsString(member.Target, scope))
                    references.Add(new Reference(span) { Root = Root(member), Path = path,
                        Scope = scope, StringMember = true, Entry = Route("core.string." + member.Member) });
                else if (path != null)
                    references.Add(new Reference(span) { Root = Root(member), Path = path, Scope = scope });
            }
            else if (expression is ArtiCallExpression call)
            {
                VisitExpression(call.Target, scope, tokens);
                foreach (ArtiArgument argument in call.Arguments) VisitExpression(argument.Value, scope, tokens);
            }
            else if (expression is ArtiIndexExpression index)
            {
                VisitExpression(index.Target, scope, tokens);
                VisitExpression(index.Index, scope, tokens);
            }
            else if (expression is ArtiBinaryExpression binary)
            {
                VisitExpression(binary.Left, scope, tokens);
                VisitExpression(binary.Right, scope, tokens);
            }
            else if (expression is ArtiUnaryExpression unary) VisitExpression(unary.Operand, scope, tokens);
            else if (expression is ArtiArrayExpression array)
                foreach (ArtiExpression item in array.Items) VisitExpression(item, scope, tokens);
            else if (expression is ArtiObjectExpression obj)
                foreach (ArtiObjectMember item in obj.Members) VisitExpression(item.Value, scope, tokens);
            else if (expression is ArtiInterpolatedStringExpression interpolated)
                foreach (ArtiExpression part in interpolated.Parts) VisitExpression(part, scope, tokens);
        }

        private static ArtiSourceSpan MemberSpan(ArtiMemberExpression member, IList<ArtiToken> tokens)
        {
            // Member nodes include their target; only the final lexer token is clickable.
            int low = 0, high = tokens.Count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                ArtiToken token = tokens[middle];
                int start = member.Span.EndOffset - member.Member.Length;
                if (token.Span.StartOffset < start) low = middle + 1;
                else if (token.Span.StartOffset > start) high = middle - 1;
                else return token.Kind == ArtiTokenKind.Identifier && token.Span.EndOffset == member.Span.EndOffset
                    ? token.Span : ArtiSourceSpan.Empty;
            }
            return ArtiSourceSpan.Empty;
        }

        private bool IsString(ArtiExpression expression, Scope scope)
        {
            if (expression is ArtiLiteralExpression literal) return literal.Value is string;
            if (expression is ArtiInterpolatedStringExpression) return true;
            if (expression is ArtiNameExpression name)
                return scope.Resolve(name.Name, name.Span.StartOffset)?.IsString == true;
            if (expression is ArtiBinaryExpression binary && binary.Operator == ArtiTokenKind.Plus)
                return IsString(binary.Left, scope) || IsString(binary.Right, scope);
            if (expression is ArtiCallExpression call)
            {
                string function = Path(call.Target);
                bool inverted = call.Target is ArtiMemberExpression member && IsString(member.Target, scope);
                if (inverted) function = "core.string." + ((ArtiMemberExpression)call.Target).Member;
                if (function == null) return false;
                if (!inverted)
                {
                    string root = Root(call.Target);
                    Binding binding = root == null ? null : scope.Resolve(root, call.Span.StartOffset);
                    if (binding != null)
                    {
                        if (binding.Module == null) return false;
                        function = binding.Module + function.Substring(root.Length);
                    }
                }
                DocumentationEntry entry = Route(function);
                return entry != null && entry.RelativePath.StartsWith("core/string/", StringComparison.Ordinal)
                    && entry.RelativePath != "core/string/len.md" && entry.RelativePath != "core/string/contains.md"
                    && entry.RelativePath != "core/string/starts_with.md" && entry.RelativePath != "core/string/ends_with.md"
                    && entry.RelativePath != "core/string/is_empty.md" && entry.RelativePath != "core/string/split.md";
            }
            return false;
        }

        private static string Path(ArtiExpression expression)
        {
            if (expression is ArtiNameExpression name) return name.Name;
            if (expression is ArtiMemberExpression member)
            {
                string target = Path(member.Target);
                return target == null ? null : target + "." + member.Member;
            }
            return null;
        }

        private static string Root(ArtiExpression expression)
        {
            while (true)
            {
                if (expression is ArtiMemberExpression member) expression = member.Target;
                else if (expression is ArtiCallExpression call) expression = call.Target;
                else return (expression as ArtiNameExpression)?.Name;
            }
        }

        private static string Module(string package)
        {
            if (package == "core") return "core";
            return string.Equals(package, "memory", StringComparison.OrdinalIgnoreCase)
                || string.Equals(package, "cj.rimtalk.expandmemory", StringComparison.OrdinalIgnoreCase) ? "memory" : null;
        }

        private sealed class Reference
        {
            internal Reference(ArtiSourceSpan span) { Span = span; }
            internal readonly ArtiSourceSpan Span;
            internal string Root, Path;
            internal Scope Scope;
            internal bool Suppressed, StringMember;
            internal DocumentationEntry Entry, RegisteredEntry;
        }

        private sealed class Binding
        {
            internal string Name, Module;
            internal int Start;
            internal bool IsString;
        }

        private sealed class Scope
        {
            private readonly Scope parent;
            private readonly List<Binding> bindings = new List<Binding>();
            internal readonly int Start;
            internal Scope(Scope parent, int start, bool transparent = false)
            {
                this.parent = transparent ? parent.parent : parent;
                if (transparent) bindings = parent.bindings;
                Start = start;
            }
            internal void Add(string name, int start, string module, bool isString)
            {
                if (string.IsNullOrEmpty(name) || name == "_") return;
                bindings.Add(new Binding { Name = name, Start = start, Module = module, IsString = isString });
            }
            internal Binding Resolve(string name, int offset)
            {
                for (int index = bindings.Count - 1; index >= 0; index--)
                    if (bindings[index].Start <= offset && bindings[index].Name == name) return bindings[index];
                return parent?.Resolve(name, offset);
            }
        }
    }
}
