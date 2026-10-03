using System;
using System.IO;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Documentation;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiDocumentationIndexChecks
    {
        internal static void Run()
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "AdvancedRimTalk.csproj"))) root = root.Parent;
            if (root == null) throw new Exception("Documentation index checks require the repository root.");
            var index = new ArtiDocumentationIndex(AdvancedRimTalkDocumentationCatalog.Create(root.FullName));
            CheckQualifiedRoutes(index);
            CheckPlayerScopes(index);
            CheckNonCode(index);
            CheckRegisteredSymbols(index);
        }

        private static void CheckQualifiedRoutes(ArtiDocumentationIndex index)
        {
            Check(index, "{{% core.em|it('a') %}}", "core/emit.md");
            Check(index, "{{% core.app|end('a') %}}", "core/append.md");
            Check(index, "{{% core.string.app|end('a', 'b') %}}", "core/string/append.md");
            Check(index, "{{% core.random.in|t(1, 2) %}}", "core/int.md");
            Check(index, "{{% core.value.def|ault(null, 'a') %}}", "core/default.md");
            Check(index, "{{% core.text.ind|ent('a', 1) %}}", "core/indent.md");
            Check(index, "{{% core.escape.jso|n('a') %}}", "core/json.md");
            Check(index, "{{% core.diag.wa|rn('a') %}}", "core/warn.md");
            Check(index, "{{% ct|x.topic %}}", "data/context.md");
            Check(index, "{{% pawn.in|fo.health %}}", "data/pawn_info.md");
            Check(index, "{{% core.pawn.in|fo.health %}}", "data/pawn_info.md");
            Check(index, "{{% use optional memory as mem; mem.pawn.con|text() %}}", "memory/pawn/context.md");
            Check(index, "{{% use cj.rimtalk.expandmemory as mem; mem.knowledge.exi|sts('x') %}}", "memory/knowledge/exists.md");
            Check(index, "{{% use memory as mem; mem.common_knowledge.exi|sts('x') %}}", "memory/knowledge/exists.md");
            Check(index, "{{% use memory as mem; mem.pawn.abm.li|st() %}}", "memory/pawn/list.md");
            Check(index, "{{% core.once.do|ne('x') %}}", "core/once.md");
            Check(index, "{{% use core as c; c.string.tr|im('a') %}}", "core/string/trim.md");
            Check(index, "{{% string.up|case('a') %}}", "compatibility/scriban.md");
            Check(index, "{{% 'a'.tr|im().upper() %}}", "core/string/trim.md");
            Check(index, "{{% 'a'.trim().up|per() %}}", "core/string/upper.md");
            Check(index, "{{% let text = 'a'; text.tr|im() %}}", "core/string/trim.md");
            Check(index, "{{% f|n custom(v) { return v } %}}", "language/function.md");
            Check(index, "{{% use core a|s c %}}", "language/use.md");
            Check(index, "{{% for v i|n [1] { core.emit(v) } %}}", "language/for.md");
            Check(index, "{{% core.emit(tr|ue) %}}", "language/literal.md");
            Check(index, "{{% em|it('a') %}}", null);
            Check(index, "{{% core.unk|nown() %}}", null);
            Check(index, "{{% core.string.tr|immed() %}}", null);
            Check(index, "{{% core.emit|('a') %}}", null);
        }

        private static void CheckPlayerScopes(ArtiDocumentationIndex index)
        {
            Check(index, "{{% tr|im('a'); fn trim(v) { return v } %}}", null);
            Check(index, "{{% fn custom(pawn) { pawn.na|me }; pawn.name %}}", null);
            Check(index, "{{% fn custom(pawn) { pawn.name }; pawn.na|me %}}", "data/pawn.md");
            Check(index, "{{% core.emit(pa|wn); let pawn = {}; pawn.name %}}", "data/pawn.md");
            Check(index, "{{% let pawn = pawn; pawn.na|me %}}", null);
            Check(index, "{{% let pawn = pa|wn %}}", "data/pawn.md");
            Check(index, "{{% let pa|wn = {} %}}", null);
            Check(index, "{{% if true { let pawn = {}; pawn.na|me }; pawn.name %}}", null);
            Check(index, "{{% if true { let pawn = {} }; pawn.na|me %}}", null);
            Check(index, "{{% for pawn in [pawn] { pawn.na|me }; pawn.name %}}", null);
            Check(index, "{{% for pawn in [pa|wn] { pawn.name } %}}", "data/pawn.md");
            Check(index, "{{% for pawn in [1] { pawn.name }; pawn.na|me %}}", null);
            Check(index, "{{% let pawn, map = [1, 2]; map.na|me %}}", null);
            Check(index, "{{% let obj = { trim: 1 }; obj.tr|im() %}}", null);
            Check(index, "{{% let core = {}; core.em|it('a') %}}", null);
            Check(index, "{{% core.emit(pa|wn); const pawn = 'local' %}}", null);
            Check(index, "{{% use unrelated.mod as core; core.em|it('a') %}}", null);
            Check(index, "{{% use unrelated.mod as memory; memory.pawn.con|text() %}}", null);
            Check(index, "{{% fn trim(v) { return v } %}} {{% tr|im('a') %}}", null);
            Check(index, "{{% const pawn = 'local' %}} {{% pa|wn %}}", null);
            Check(index, "{{% let pawn = 'local' %}} {{% pa|wn %}}", "data/pawn.md");
            Check(index, "{{% let obj = { pa|wn: 1 } %}}", null);
            Check(index, "{{% core.emit(pa|wn: 'a') %}}", null);
        }

        private static void CheckNonCode(ArtiDocumentationIndex index)
        {
            Check(index, "core.em|it('a')", null);
            Check(index, "{{ core.em|it('a') }}", null);
            Check(index, "{{ '{{% core.em|it() %}}' }}", null);
            Check(index, "```arti\n{{% core.em|it('a') %}}\n```", null);
            Check(index, "`{{% core.em|it('a') %}}`", null);
            Check(index, "{{% // core.em|it('a')\ncore.emit('b') %}}", null);
            Check(index, "{{% /* core.em|it('a') */ core.emit('b') %}}", null);
            Check(index, "{{% core.emit('pawn.na|me') %}}", null);
            Check(index, "{{% core.emit(f'core.em|it {pawn.name}') %}}", null);
            Check(index, "{{% core.emit(f'Value: {pawn.na|me}') %}}", "data/pawn.md");
        }

        private static void CheckRegisteredSymbols(ArtiDocumentationIndex index)
        {
            var symbols = new ArtiSymbolCatalog(new[] { "mod_value", "def", "trim", "pawn", "core" });
            Check(index, "{{% mod_va|lue %}}", "data/context.md", null, symbols);
            Check(index, "{{% de|f %}}", "data/def.md", null, symbols);
            Check(index, "{{% mod_va|lue %}}", null);
            Check(index, "{{% mod_va|lue %}}", null, new[] { "mod_value" }, symbols);
            Check(index, "{{% tr|im('a') %}}", null, new[] { "trim" }, symbols);
            Check(index, "{{% pawn.na|me %}}", null, new[] { "pawn" }, symbols);
            Check(index, "{{% core.em|it('a') %}}", null, new[] { "core" }, symbols);
            Check(index, "{{% let mod_value = 1; mod_va|lue %}}", null, null, symbols);
        }

        private static void Check(ArtiDocumentationIndex index, string markedSource, string expected,
            string[] players = null, IArtiSymbolCatalog symbols = null)
        {
            int offset = markedSource.IndexOf('|');
            string source = markedSource.Remove(offset, 1);
            string actual = index.Find(source, offset, players, symbols)?.RelativePath;
            if (actual != expected)
                throw new Exception("Documentation route for " + markedSource + ": expected " + (expected ?? "no jump") + ", got " + (actual ?? "no jump"));
        }
    }
}
