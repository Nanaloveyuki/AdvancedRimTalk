using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Prompt;
using AdvancedRimTalk.UI;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiPartEditorWindowChecks
    {
        internal static void Run()
        {
            var window = new ArtiPartEditorWindow(new ArtiPromptPart());
            if (window.closeOnAccept || !window.absorbInputAroundWindow || !window.doCloseX)
                throw new Exception("Part editor must retain Enter without passing input to underlying windows.");
            window.OnAcceptKeyPressed();
            if (window.Closed) throw new Exception("Accept must not close the multiline editor.");
            CheckPreviousDefinitions();
        }

        private static void CheckPreviousDefinitions()
        {
            var intro = new ArtiPromptPart { Content = "{{% fn mod_status(id) { return true, id }; const title = \"test\"; let private_value = 1 %}}" };
            var disabled = new ArtiPromptPart { Enabled = false, Content = "{{% fn disabled_fn() { return false } %}}" };
            var current = new ArtiPromptPart { Content = "{{% let status, _ = mod_status(title) %}}" };
            var later = new ArtiPromptPart { Content = "{{% fn later_fn() { return true } %}}" };
            var parts = new List<ArtiPromptPart> { intro, disabled, current, later };
            var context = new ArtiPromptAnalysisContext();
            if (!context.Refresh(parts, current) || !new HashSet<string>(context.Names).SetEquals(new[] { "mod_status", "title" }))
                throw new Exception("Part analysis must collect only earlier enabled top-level fn/const names.");
            var parsed = new ArtiDocumentParser().Parse(current.Content).CodeBlocks[0].ParseResult;
            if (new ArtiAnalyzer(externalGlobals: context.Names, externalConstants: context.Constants,
                externalFunctions: context.Functions).Analyze(parsed.Program).HasErrors)
                throw new Exception("Earlier function and constant remain unavailable to current part.");
            if (!context.Constants.SequenceEqual(new[] { "title" }) || !context.Functions.SequenceEqual(new[] { "mod_status" }))
                throw new Exception("Part analysis lost definition types.");
            foreach (string code in new[] { "const next = title + '!'", "title = 'changed'", "mod_status = 1" })
            {
                var analysis = new ArtiAnalyzer(externalGlobals: context.Names, externalConstants: context.Constants,
                    externalFunctions: context.Functions).Analyze(new ArtiParser().Parse(code).Program);
                if (analysis.HasErrors == code.StartsWith("const"))
                    throw new Exception("Cross-part constant/function metadata: " + code);
            }
            if (context.Refresh(parts, current)) throw new Exception("Unchanged part context should stay cached.");
            intro.Enabled = false;
            if (!context.Refresh(parts, current) || context.Names.Any() || context.Constants.Any() || context.Functions.Any())
                throw new Exception("Disabled definitions remained cached.");
            intro.Enabled = true;
            intro.Content = "{{% fn renamed() { const hidden = 1 }; if true { fn nested() { return 1 } } %}}";
            if (!context.Refresh(parts, current) || !context.Names.SequenceEqual(new[] { "renamed" }))
                throw new Exception("Renamed definitions or nested declarations leaked into context.");
            parts.Remove(intro); parts.Add(intro);
            if (!context.Refresh(parts, current) || context.Names.Any()) throw new Exception("Reordered definitions remained visible.");
            context.Refresh(parts, new ArtiPromptPart());
            if (context.Names.Any()) throw new Exception("Detached editor collected unrelated parts.");
            intro.Content = "{{% fn broken( %}}";
            parts.Remove(intro); parts.Insert(0, intro);
            context.Refresh(parts, current);
            if (context.Names.Any()) throw new Exception("Invalid preceding block exported names.");
        }
    }
}

// Only the host window contract is simulated; TextEditor keyboard handling needs in-game validation.
namespace Verse
{
    internal class Window
    {
        public bool closeOnAccept = true;
        public bool doCloseX, draggable, resizeable, absorbInputAroundWindow;
        public bool Closed;
        public virtual UnityEngine.Vector2 InitialSize => default;
        public virtual void DoWindowContents(UnityEngine.Rect rect) { }
        public virtual void OnAcceptKeyPressed() { if (closeOnAccept) Closed = true; }
    }
    internal static class Widgets
    {
        public static void Label(UnityEngine.Rect rect, string text) { }
    }
}

namespace UnityEngine
{
    internal struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
    }
    internal struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
    }
    internal static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); }
}

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiCodeEditorPage
    {
        public ArtiCodeEditorPage(ArtiPromptPart part) { }
        public void Draw(UnityEngine.Rect rect) { }
    }
}
