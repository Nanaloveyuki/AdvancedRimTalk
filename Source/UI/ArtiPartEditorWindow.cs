using AdvancedRimTalk.Prompt;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiPartEditorWindow : Window
    {
        private readonly ArtiPromptPart part;
        private readonly ArtiCodeEditorPage editor;

        public ArtiPartEditorWindow(ArtiPromptPart part)
        {
            this.part = part;
            editor = new ArtiCodeEditorPage(part);
            doCloseX = true;
            draggable = true;
            resizeable = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(1100f, 760f);

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 40f, 28f), part.Name);
            editor.Draw(new Rect(inRect.x, inRect.y + 32f, inRect.width, Mathf.Max(1f, inRect.height - 32f)));
        }
    }
}
