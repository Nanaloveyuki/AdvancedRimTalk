using System;
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
    internal struct Vector2 { public Vector2(float x, float y) { } }
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
