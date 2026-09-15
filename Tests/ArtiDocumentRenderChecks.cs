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
            var valid = ArtiPromptDocumentRenderer.Render("before {{% core.emit(\"value\") %}} after", preview);
            Check(valid.Text == "before value after" && !valid.HasErrors, "valid block");
            var global = ArtiPromptDocumentRenderer.Render("{{% fn render_global() { return \"global\" } %}}", preview);
            var globalCall = ArtiPromptDocumentRenderer.Render("{{% core.emit(render_global()) %}}", preview);
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
    internal static class Log { public static void Warning(string text) { } }
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
