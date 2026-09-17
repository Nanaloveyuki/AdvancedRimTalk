using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Integration;
using RimTalk.Prompt;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiDocumentRenderChecks
    {
        internal static void Run()
        {
            var preview = new PromptContext { IsPreview = true };
            var triple = ArtiPromptDocumentRenderer.Render("before {{% core.emit('''one '\n%}}\nend''') %}} after", preview);
            Check(!triple.HasErrors && triple.Text == "before one '\n%}}\nend after", "triple quoted document marker");
            var mapped = ArtiPromptDocumentRenderer.Render("line1\nline2\nline3\n{{% core.emit(f\"{missing_probe}\") %}}", preview);
            Check(mapped.HasErrors && mapped.Diagnostics[0].Span.Line == 4 && mapped.Diagnostics[0].Span.Column == 18,
                "runtime diagnostics use document coordinates");
            const string counter = "{{% let count = 0; fn next() { count += 1; return count } %}}";
            var liveRuntime = new ArtiGlobalRuntime();
            var previewRuntime = new ArtiGlobalRuntime();
            Check(!ArtiPromptDocumentRenderer.Render(counter, preview, liveRuntime, "intro").HasErrors, "live declarations");
            Check(!ArtiPromptDocumentRenderer.Render(counter, preview, previewRuntime, "intro").HasErrors, "preview declarations");
            Check(ArtiPromptDocumentRenderer.Render("{{% core.emit(next()) %}}", preview, previewRuntime).Text == "1", "preview counter");
            Check(ArtiPromptDocumentRenderer.Render("{{% core.emit(next()) %}}", preview, liveRuntime).Text == "1", "live counter isolated");
            Check(ArtiPromptDocumentRenderer.Render("{{% core.emit(next()) %}}", preview, liveRuntime).Text == "2", "same generation shares closure");
            Check(ArtiPromptDocumentRenderer.Render("{{% core.emit(next()) %}}", preview).HasErrors, "next generation drops disabled definitions");
            Check(!ArtiPromptDocumentRenderer.Render("prefix\n" + counter, preview).HasErrors, "edited definition starts fresh");
            Check(!ArtiPromptDocumentRenderer.Render("{{% const n = 2 %}}{{% core.emit(n) %}}", preview).HasErrors, "document blocks share constants");
            var valid = ArtiPromptDocumentRenderer.Render("before {{% core.emit(\"value\") %}} after", preview);
            Check(valid.Text == "before value after" && !valid.HasErrors, "valid block");
            var interpolated = ArtiPromptDocumentRenderer.Render(
                "before {{% fn echo_marker(value) { return value }; core.emit(f\"{echo_marker(\"%}}\")}\") %}} after", preview);
            Check(!interpolated.HasErrors && interpolated.Text == "before %}} after",
                "interpolation quotes must not terminate the document block: " + string.Join(";", interpolated.Diagnostics));
            var language = ArtiPromptDocumentRenderer.Render(
                "{{% let lang = \"简体中文\"\n"
                + "core.emit(\"1.自主扮演游戏中的角色\", true)\n"
                + "core.emit(\"2.自动根据提供的信息以合适频率引出话题并模拟对话\", true)\n"
                + "core.emit(\"3.对话语言需与世界观,人物性格和情绪,状态因素相符合\", true)\n"
                + "if lang == \"简体中文\" { let language = \"必须使用通俗白话\"; setvar(\"language\", language) } "
                + "else if lang == \"繁體中文\" { let language = \"推荐使用通俗白话\"; setvar(\"language\", language) }\n"
                + "core.emit(\"4.禁止使用书面用语\", false)\n"
                + "if exists(language) { core.emit(language, false) } %}}", preview);
            Check(!language.HasErrors && language.Text.EndsWith("4.禁止使用书面用语必须使用通俗白话"),
                "user language prompt through global runtime: " + string.Join(";", language.Diagnostics));
            var generation = new ArtiGlobalRuntime();
            var global = ArtiPromptDocumentRenderer.Render("{{% fn render_global() { return \"global\" } %}}", preview, generation);
            var globalCall = ArtiPromptDocumentRenderer.Render("{{% core.emit(render_global()) %}}", preview, generation);
            Check(!global.HasErrors && !globalCall.HasErrors && globalCall.Text == "global",
                "global function across blocks: " + global.Text + " / " + globalCall.Text + " / "
                + string.Join(";", global.Diagnostics) + " / " + string.Join(";", globalCall.Diagnostics));
            var invalid = ArtiPromptDocumentRenderer.Render("before {{% let = %}} after", preview);
            Check(invalid.Text == "before  after" && invalid.HasErrors, "error block skipped with diagnostics");
            var live = ArtiPromptDocumentRenderer.Render("before {{% let = %}} after", new PromptContext());
            Check(live.HasErrors && live.Text.Contains("Arti error"), "live behavior preserved");
            ScribanParser.SetSessionVar("key", "real");
            using (new PromptPreviewSession())
            {
                var write = ArtiPromptDocumentRenderer.Render("{{% setvar(\"key\", \"preview\") %}}", preview);
                var read = ArtiPromptDocumentRenderer.Render("{{% core.emit(getvar(\"key\")) %}}", preview);
                Check(!write.HasErrors && read.Text == "preview", "shared preview variables across parts");
                Check(Equals(ScribanParser.GetSessionVar("key"), "real"), "Arti preview did not touch real session");
            }
            Check(Equals(ScribanParser.GetSessionVar("key"), "real"), "real session preserved after preview");
        }

        private static void Check(bool value, string name)
        {
            if (!value) throw new Exception("Arti document render: " + name);
        }
    }
}

// Only host integration is substituted; the production document renderer and Arti engine are linked.
namespace RimTalk.Prompt
{
    internal sealed class PromptContext { public bool IsPreview; }
    internal static class ScribanParser
    {
        private static readonly Dictionary<string, object> Values = new Dictionary<string, object>();
        public static void SetSessionVar(string key, object value) { Values[key] = value; }
        public static void ResetSessionVariables() { Values.Clear(); }
        public static object GetSessionVar(string key) => Values.TryGetValue(key, out object value) ? value : null;
    }
}
namespace Verse
{
    internal sealed class Game { }
    internal static class Current { internal static Game Game = new Game(); }
    internal static class Prefs { internal static bool DevMode => false; }
    internal static class Log
    {
        public static void Warning(string text) { }
        public static void Error(string text) { }
        public static void Message(string text) { }
    }
}
namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkArtiCatalog
    {
        public static ArtiSymbolCatalog CreateSymbolCatalog() => new ArtiSymbolCatalog();
        public static IArtiModuleCatalog CreateModuleCatalog() => null;
    }
    internal sealed class RimTalkArtiRuntimeValueProvider : IArtiRuntimeValueProvider
    {
        public RimTalkArtiRuntimeValueProvider(PromptContext context) { }
        public bool TryGetGlobal(string name, out object value) { value = null; return false; }
        public bool TryGetMember(object target, string member, out object value) { value = null; return false; }
        public bool TryGetIndex(object target, object index, out object value) { value = null; return false; }
    }
}
