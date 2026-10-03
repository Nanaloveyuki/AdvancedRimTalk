using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiPartEditorWindow : Window
    {
        private readonly ArtiEditorWindowManager manager;
        private readonly ArtiEditorWorkspacePage page;
        internal ArtiEditorWorkspace Workspace { get; } = new ArtiEditorWorkspace();
        internal string Title => Workspace.Active?.Title ?? "AdvancedRimTalk.ArtiEditor.Title".Translate().ToString();

        internal ArtiPartEditorWindow(ArtiEditorWindowManager manager)
        {
            this.manager = manager;
            page = new ArtiEditorWorkspacePage(manager, Workspace);
            doCloseX = true;
            // Let the multiline editor receive Enter before the window consumes it.
            closeOnAccept = false;
            draggable = true;
            resizeable = true;
            // Peer windows and the preset browser remain clickable; real modals retain priority.
            absorbInputAroundWindow = false;
            onlyOneOfTypeAllowed = false;
            forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            layer = WindowLayer.Dialog;
        }

        public override Vector2 InitialSize => new Vector2(
            Mathf.Min(1100f, Verse.UI.screenWidth), Mathf.Min(760f, Verse.UI.screenHeight));

        public override void DoWindowContents(Rect inRect)
        {
            page.Draw(inRect, this);
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            int offset = (Find.WindowStack.Count % 5) * 24;
            windowRect.x = Mathf.Clamp(windowRect.x + offset, 0f, Mathf.Max(0f, Verse.UI.screenWidth - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y + offset, 0f, Mathf.Max(0f, Verse.UI.screenHeight - windowRect.height));
        }

        public override void PostClose()
        {
            manager.WindowClosed(this);
            base.PostClose();
        }
    }
}
