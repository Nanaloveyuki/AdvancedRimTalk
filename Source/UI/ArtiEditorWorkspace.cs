using System;
using System.Collections.Generic;
using AdvancedRimTalk.Prompt;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiEditorSession
    {
        public ArtiPromptPreset Preset { get; }
        public ArtiPromptPart Part { get; }
        public ArtiCodeEditorPage Editor { get; }
        public string Title => Preset.Name + " / " + Part.Name;

        public ArtiEditorSession(ArtiPromptPreset preset, ArtiPromptPart part)
        {
            Preset = preset;
            Part = part;
            Editor = new ArtiCodeEditorPage(part, preset);
        }
    }

    internal sealed class ArtiEditorWorkspace
    {
        private readonly List<ArtiEditorSession> sessions = new List<ArtiEditorSession>();
        public IReadOnlyList<ArtiEditorSession> Sessions { get; }
        public ArtiEditorSession Active { get; private set; }

        public ArtiEditorWorkspace()
        {
            Sessions = sessions.AsReadOnly();
        }

        public ArtiEditorSession Open(ArtiPromptPreset preset, ArtiPromptPart part)
        {
            ArtiEditorSession session = Find(preset, part);
            if (session == null)
            {
                session = new ArtiEditorSession(preset, part);
                sessions.Add(session);
            }
            SetActive(session);
            return session;
        }

        public void Add(ArtiEditorSession session)
        {
            ArtiEditorSession existing = Find(session.Preset, session.Part);
            if (existing == null)
            {
                sessions.Add(session);
                existing = session;
            }
            SetActive(existing);
        }

        public bool Select(ArtiEditorSession session)
        {
            if (!sessions.Contains(session)) return false;
            SetActive(session);
            return true;
        }

        public bool Remove(ArtiEditorSession session)
        {
            int index = sessions.IndexOf(session);
            if (index < 0) return false;
            sessions.RemoveAt(index);
            if (ReferenceEquals(Active, session))
                SetActive(sessions.Count == 0 ? null : sessions[Math.Min(index, sessions.Count - 1)]);
            return true;
        }

        private ArtiEditorSession Find(ArtiPromptPreset preset, ArtiPromptPart part)
        {
            foreach (ArtiEditorSession session in sessions)
                if (ReferenceEquals(session.Preset, preset) && ReferenceEquals(session.Part, part))
                    return session;
            return null;
        }

        private void SetActive(ArtiEditorSession session)
        {
            if (ReferenceEquals(Active, session)) return;
            Active?.Editor.Suspend();
            Active = session;
            Active?.Editor.Activate();
        }
    }
}
