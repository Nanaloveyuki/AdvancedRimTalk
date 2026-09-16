using System;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace AdvancedRimTalk.Prompt
{
    public sealed class ArtiPromptPreset : IExposable
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Advanced RimTalk";
        public List<ArtiPromptPart> Parts = new List<ArtiPromptPart>();
        public ArtiPromptInit Init = new ArtiPromptInit();

        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id", string.Empty);
            Scribe_Values.Look(ref Name, "name", "Advanced RimTalk");
            Scribe_Collections.Look(ref Parts, "parts", LookMode.Deep);
            Scribe_Deep.Look(ref Init, "init");
            Normalize();
        }

        public void Normalize()
        {
            if (string.IsNullOrEmpty(Id)) Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(Name)) Name = "Advanced RimTalk";
            if (Parts == null) Parts = new List<ArtiPromptPart>();
            Parts.RemoveAll(part => part == null);
            foreach (var part in Parts) part.Normalize();
            if (Init == null) Init = new ArtiPromptInit();
        }

        public ArtiPromptPreset Copy(string name)
        {
            return new ArtiPromptPreset { Name = name, Parts = Parts.Select(part => part.Copy()).ToList(), Init = Init.Copy() };
        }
    }

    public sealed class ArtiPromptInit : IExposable
    {
        public bool EnabledByPlayer;
        public string Title = string.Empty;
        public string Description = string.Empty;
        public List<string> Sources = new List<string>();
        public List<string> Anchors = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref EnabledByPlayer, "enabledByPlayer", false);
            Scribe_Values.Look(ref Title, "title", string.Empty);
            Scribe_Values.Look(ref Description, "description", string.Empty);
            Scribe_Collections.Look(ref Sources, "sources", LookMode.Value);
            Scribe_Collections.Look(ref Anchors, "anchors", LookMode.Value);
            if (Sources == null) Sources = new List<string>();
            if (Anchors == null) Anchors = new List<string>();
            Sources = Sources.Where(s => s != null && (s == "common_knowledge" || s == "long_term_memory" || s == "short_term_memory" || s == "event_log")).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Anchors = Anchors.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.Ordinal).ToList();
        }

        public ArtiPromptInit Copy()
        {
            return new ArtiPromptInit { EnabledByPlayer = EnabledByPlayer, Title = Title, Description = Description,
                Sources = new List<string>(Sources ?? new List<string>()), Anchors = new List<string>(Anchors ?? new List<string>()) };
        }
    }
}
