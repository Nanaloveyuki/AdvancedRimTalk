using System.Collections.Generic;
using AdvancedRimTalk.Prompt;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiEditorWorkspacePage
    {
        private const float TabWidth = 240f;
        private const float Gap = 6f;
        private readonly ArtiEditorWindowManager manager;
        private readonly ArtiEditorWorkspace workspace;
        private Vector2 tabScroll;
        private ArtiEditorSession revealedSession;

        internal ArtiEditorWorkspacePage(ArtiEditorWindowManager manager,
            ArtiEditorWorkspace workspace)
        {
            this.manager = manager;
            this.workspace = workspace;
        }

        internal void Draw(Rect rect, Window host)
        {
            var settings = AdvancedRimTalkMod.Settings;
            if (settings == null) return;
            manager.Prune(workspace, settings);
            if (host is ArtiPartEditorWindow && workspace.Sessions.Count == 0)
            {
                host.Close();
                return;
            }
            bool canFocus = manager.CanFocus(host);
            bool previousEnabled = GUI.enabled;
            GameFont previousFont = Text.Font;
            bool previousWordWrap = Text.WordWrap;
            try
            {
                Text.Font = GameFont.Small;
                Text.WordWrap = false;
                GUI.enabled = previousEnabled && host != null && Find.WindowStack.GetsInput(host);
                Widgets.Label(new Rect(rect.x, rect.y, Mathf.Max(1f, rect.width - 36f), 28f),
                    workspace.Active?.Title ?? "AdvancedRimTalk.ArtiEditor.Title".Translate().ToString());
                float toolbarY = rect.y + 32f;
                bool compact = rect.width < 620f;
                int columns = compact ? 2 : 4;
                float buttonWidth = Mathf.Max(1f, (rect.width - Gap * (columns - 1)) / columns);
                if (Button(rect.x, toolbarY, buttonWidth, "Open")) ShowOpenMenu();
                GUI.enabled = GUI.enabled && workspace.Active != null;
                if (Button(rect.x + buttonWidth + Gap, toolbarY, buttonWidth, "Detach"))
                {
                    manager.Detach(workspace);
                    return;
                }
                float secondY = compact ? toolbarY + 32f : toolbarY;
                float secondX = compact ? rect.x : rect.x + (buttonWidth + Gap) * 2f;
                GUI.enabled = previousEnabled && host != null && Find.WindowStack.GetsInput(host)
                    && workspace.Active != null && manager.CanMove(workspace);
                if (Button(secondX, secondY, buttonWidth, "Move")) manager.ShowMoveMenu(workspace);
                GUI.enabled = previousEnabled && host != null && Find.WindowStack.GetsInput(host) && manager.HasWindows;
                if (Button(secondX + buttonWidth + Gap, secondY, buttonWidth, "Windows")) manager.ShowWindowsMenu();
                GUI.enabled = previousEnabled && host != null && Find.WindowStack.GetsInput(host);

                float tabsY = secondY + 34f;
                DrawTabs(new Rect(rect.x, tabsY, rect.width, 50f));
                if (host is ArtiPartEditorWindow && !host.IsOpen) return;
                canFocus = manager.CanFocus(host);
                HandleTabShortcut(canFocus);
                if (host is ArtiPartEditorWindow && !host.IsOpen) return;
                Rect editorRect = new Rect(rect.x, tabsY + 56f, rect.width, Mathf.Max(1f, rect.yMax - tabsY - 56f));
                if (workspace.Active == null)
                {
                    Text.WordWrap = true;
                    Widgets.Label(editorRect, "AdvancedRimTalk.ArtiEditor.Empty".Translate());
                }
                else
                    workspace.Active.Editor.Draw(editorRect, manager.CanFocus(host));
            }
            finally
            {
                GUI.enabled = previousEnabled;
                Text.Font = previousFont;
                Text.WordWrap = previousWordWrap;
            }
        }

        private static bool Button(float x, float y, float width, string key)
        {
            return Widgets.ButtonText(new Rect(x, y, width, 28f), ("AdvancedRimTalk.ArtiEditor." + key).Translate());
        }

        private void DrawTabs(Rect rect)
        {
            float contentWidth = Mathf.Max(rect.width, workspace.Sessions.Count * (TabWidth + Gap));
            if (revealedSession != workspace.Active)
            {
                revealedSession = workspace.Active;
                for (int i = 0; i < workspace.Sessions.Count; i++)
                {
                    if (workspace.Sessions[i] != revealedSession) continue;
                    float left = i * (TabWidth + Gap);
                    if (left < tabScroll.x) tabScroll.x = left;
                    else if (left + TabWidth > tabScroll.x + rect.width)
                        tabScroll.x = Mathf.Max(0f, left + TabWidth - rect.width);
                    break;
                }
            }
            Widgets.BeginScrollView(rect, ref tabScroll, new Rect(0f, 0f, contentWidth, 32f));
            ArtiEditorSession selected = null;
            ArtiEditorSession closed = null;
            try
            {
                for (int i = 0; i < workspace.Sessions.Count; i++)
                {
                    var session = workspace.Sessions[i];
                    Rect tab = new Rect(i * (TabWidth + Gap), 0f, TabWidth, 30f);
                    if (session == workspace.Active) Widgets.DrawHighlightSelected(tab);
                    if (Widgets.ButtonText(new Rect(tab.x, tab.y, tab.width - 30f, tab.height), session.Title, false))
                        selected = session;
                    if (Widgets.ButtonText(new Rect(tab.xMax - 28f, tab.y + 2f, 26f, 26f), "×", false))
                        closed = session;
                    TooltipHandler.TipRegion(new Rect(tab.x, tab.y, tab.width - 30f, tab.height), session.Title);
                    TooltipHandler.TipRegion(new Rect(tab.xMax - 28f, tab.y, 28f, tab.height),
                        "AdvancedRimTalk.ArtiEditor.CloseTab".Translate());
                }
            }
            finally { Widgets.EndScrollView(); }
            if (closed != null) manager.CloseTab(workspace, closed);
            else if (selected != null) workspace.Select(selected);
        }

        private void ShowOpenMenu()
        {
            var settings = AdvancedRimTalkMod.Settings;
            settings.EnsureTakeoverPromptParts();
            var options = new List<FloatMenuOption>();
            foreach (var preset in settings.TakeoverPresets)
                foreach (var part in preset.Parts)
                {
                    ArtiPromptPreset targetPreset = preset;
                    ArtiPromptPart targetPart = part;
                    options.Add(new FloatMenuOption(preset.Name + " / " + part.Name,
                        () => manager.Open(targetPreset, targetPart, workspace)));
                }
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        private void HandleTabShortcut(bool canFocus)
        {
            Event current = Event.current;
            if (!canFocus || current == null || current.type != EventType.KeyDown || !current.control) return;
            if (current.keyCode == KeyCode.Tab && workspace.Sessions.Count > 1)
            {
                for (int i = 0; i < workspace.Sessions.Count; i++)
                {
                    if (workspace.Sessions[i] != workspace.Active) continue;
                    int next = (i + (current.shift ? workspace.Sessions.Count - 1 : 1)) % workspace.Sessions.Count;
                    workspace.Select(workspace.Sessions[next]);
                    current.Use();
                    break;
                }
            }
            else if (current.keyCode == KeyCode.W && workspace.Active != null)
            {
                manager.CloseTab(workspace, workspace.Active);
                current.Use();
            }
        }
    }
}
