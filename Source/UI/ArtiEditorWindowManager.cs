using System.Collections.Generic;
using AdvancedRimTalk.Prompt;
using AdvancedRimTalk.Settings;
using RimTalk.Prompt;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiEditorWindowManager
    {
        internal static readonly ArtiEditorWindowManager Shared = new ArtiEditorWindowManager();
        private readonly List<ArtiPartEditorWindow> windows = new List<ArtiPartEditorWindow>();
        private readonly ArtiEditorWorkspace embedded = new ArtiEditorWorkspace();
        private readonly ArtiEditorWorkspacePage embeddedPage;
        private ArtiPartEditorWindow lastWindow;
        private Window focusedHost;
        private bool seeded;

        private ArtiEditorWindowManager()
        {
            embeddedPage = new ArtiEditorWorkspacePage(this, embedded);
        }

        internal void DrawEmbedded(Rect rect)
        {
            var settings = AdvancedRimTalkMod.Settings;
            if (settings == null) return;
            settings.EnsureTakeoverPromptParts();
            Prune(embedded, settings);
            if (!seeded)
            {
                seeded = true;
                var preset = settings.ActiveTakeoverPreset;
                var part = preset.Parts.Find(candidate => candidate.Role == PromptRole.System);
                if (part != null && FindWindow(preset, part) == null) embedded.Open(preset, part);
            }
            embeddedPage.Draw(rect, Find.WindowStack.currentlyDrawnWindow);
        }

        internal void Open(ArtiPromptPreset preset, ArtiPromptPart part, ArtiEditorWorkspace destination = null)
        {
            if (preset == null || part == null) return;
            var existingWindow = FindWindow(preset, part);
            if (existingWindow != null)
            {
                existingWindow.Workspace.Open(preset, part);
                Raise(existingWindow);
                return;
            }
            if (destination == null)
            {
                destination = lastWindow != null && lastWindow.IsOpen ? lastWindow.Workspace
                    : windows.Count > 0 ? windows[windows.Count - 1].Workspace : CreateWindow().Workspace;
            }
            for (int i = 0; i < embedded.Sessions.Count; i++)
            {
                var session = embedded.Sessions[i];
                if (session.Preset != preset || session.Part != part) continue;
                if (destination != embedded) Move(embedded, destination, session);
                else embedded.Select(session);
                FocusWorkspace(destination);
                return;
            }
            destination.Open(preset, part);
            FocusWorkspace(destination);
        }

        internal void Detach(ArtiEditorWorkspace source)
        {
            if (source.Active == null) return;
            var window = CreateWindow();
            Move(source, window.Workspace, source.Active);
            Raise(window);
        }

        internal void ShowMoveMenu(ArtiEditorWorkspace source)
        {
            var session = source.Active;
            if (session == null) return;
            var options = new List<FloatMenuOption>();
            if (source != embedded)
                options.Add(new FloatMenuOption("AdvancedRimTalk.ArtiEditor.Dock".Translate(),
                    () => Move(source, embedded, session)));
            foreach (var window in windows)
            {
                if (window.Workspace == source) continue;
                var destination = window;
                options.Add(new FloatMenuOption(destination.Title,
                    () => Move(source, destination.Workspace, session)));
            }
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        internal bool CanMove(ArtiEditorWorkspace source)
        {
            return source != embedded || windows.Count > 0;
        }

        internal void ShowWindowsMenu()
        {
            var options = new List<FloatMenuOption>();
            foreach (var window in windows)
            {
                var target = window;
                options.Add(new FloatMenuOption(target.Title, () => Raise(target)));
            }
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        internal bool HasWindows => windows.Count > 0;

        internal bool CanFocus(Window host)
        {
            var stack = Find.WindowStack;
            if (host == null || !stack.GetsInput(host)) return false;
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                // Immediate overlays do not own keyboard focus; menus and dialogs do.
                if (stack[i].ID < 0) continue;
                if (stack[i] != host) return false;
                if (focusedHost != host)
                {
                    focusedHost = host;
                    var window = host as ArtiPartEditorWindow;
                    if (window != null) lastWindow = window;
                    (window == null ? embedded : window.Workspace).Active?.Editor.Activate();
                }
                return true;
            }
            return false;
        }

        internal void Prune(ArtiEditorWorkspace workspace, AdvancedRimTalkSettings settings)
        {
            for (int i = workspace.Sessions.Count - 1; i >= 0; i--)
            {
                var session = workspace.Sessions[i];
                if (!settings.TakeoverPresets.Contains(session.Preset) || !session.Preset.Parts.Contains(session.Part))
                    workspace.Remove(session);
            }
        }

        internal void CloseTab(ArtiEditorWorkspace workspace, ArtiEditorSession session)
        {
            workspace.Remove(session);
            CloseEmptyWindow(workspace);
        }

        internal void WindowClosed(ArtiPartEditorWindow window)
        {
            window.Workspace.Active?.Editor.Suspend();
            windows.Remove(window);
            if (lastWindow == window) lastWindow = null;
            if (focusedHost == window) focusedHost = null;
        }

        private ArtiPartEditorWindow CreateWindow()
        {
            var window = new ArtiPartEditorWindow(this);
            windows.Add(window);
            Find.WindowStack.Add(window);
            lastWindow = window;
            return window;
        }

        private ArtiPartEditorWindow FindWindow(ArtiPromptPreset preset, ArtiPromptPart part)
        {
            foreach (var window in windows)
                foreach (var session in window.Workspace.Sessions)
                    if (session.Preset == preset && session.Part == part) return window;
            return null;
        }

        private void Move(ArtiEditorWorkspace source, ArtiEditorWorkspace destination, ArtiEditorSession session)
        {
            if (source == destination || !source.Remove(session)) return;
            destination.Add(session);
            CloseEmptyWindow(source);
            FocusWorkspace(destination);
        }

        private void CloseEmptyWindow(ArtiEditorWorkspace workspace)
        {
            if (workspace.Sessions.Count != 0) return;
            foreach (var window in windows)
            {
                if (window.Workspace != workspace) continue;
                window.Close();
                return;
            }
        }

        private void FocusWorkspace(ArtiEditorWorkspace workspace)
        {
            foreach (var window in windows)
                if (window.Workspace == workspace) { Raise(window); return; }
            // Docking remains available even when the settings page is hidden.
            focusedHost = null;
        }

        private void Raise(ArtiPartEditorWindow window)
        {
            if (!window.IsOpen || !Find.WindowStack.GetsInput(window)) return;
            // The native operation preserves layer ordering and dismisses transient menus.
            Find.WindowStack.Notify_ClickedInsideWindow(window);
            lastWindow = window;
            focusedHost = null;
            window.Workspace.Active?.Editor.Activate();
        }
    }
}
