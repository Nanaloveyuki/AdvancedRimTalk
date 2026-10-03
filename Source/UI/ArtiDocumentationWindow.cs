using AdvancedRimTalk.Documentation;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiDocumentationWindow : Window
    {
        private readonly DocumentationPage page;

        private ArtiDocumentationWindow(AdvancedRimTalkDocumentationCatalog catalog)
        {
            page = new DocumentationPage(catalog);
            optionalTitle = "AdvancedRimTalk.Documentation.Title".Translate().ToString();
            doCloseX = true;
            closeOnAccept = false;
            draggable = true;
            resizeable = true;
            absorbInputAroundWindow = false;
            forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            layer = WindowLayer.Dialog;
        }

        public override Vector2 InitialSize => new Vector2(
            Mathf.Min(1100f, Verse.UI.screenWidth), Mathf.Min(760f, Verse.UI.screenHeight));

        internal static void Open(DocumentationEntry entry = null)
        {
            var stack = Find.WindowStack;
            var window = stack.WindowOfType<ArtiDocumentationWindow>();
            if (window == null)
            {
                window = new ArtiDocumentationWindow(
                    LoadedModManager.GetMod<AdvancedRimTalkMod>().Documentation.Catalog);
                if (entry != null) window.page.Focus(entry.RelativePath);
                stack.Add(window);
            }
            else if (entry != null)
            {
                window.page.Focus(entry.RelativePath);
            }

            // Preserve modal and layer priority, matching the editor peer windows.
            if (window.IsOpen && stack.GetsInput(window))
                stack.Notify_ClickedInsideWindow(window);
        }

        public override void WindowOnGUI()
        {
            ConstrainSize();
            base.WindowOnGUI();
            // Verse.Window owns its resizer privately and applies resize requests later.
            ConstrainSize();
        }

        private void ConstrainSize()
        {
            windowRect.width = Mathf.Clamp(windowRect.width,
                Mathf.Min(640f, Verse.UI.screenWidth), Verse.UI.screenWidth);
            windowRect.height = Mathf.Clamp(windowRect.height,
                Mathf.Min(420f, Verse.UI.screenHeight), Verse.UI.screenHeight);
            windowRect.x = Mathf.Clamp(windowRect.x, 0f,
                Mathf.Max(0f, Verse.UI.screenWidth - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f,
                Mathf.Max(0f, Verse.UI.screenHeight - windowRect.height));
        }

        public override void DoWindowContents(Rect inRect)
        {
            bool previousEnabled = GUI.enabled;
            try
            {
                GUI.enabled = previousEnabled && Find.WindowStack.GetsInput(this);
                page.Draw(inRect);
            }
            finally
            {
                GUI.enabled = previousEnabled;
            }
        }
    }
}
