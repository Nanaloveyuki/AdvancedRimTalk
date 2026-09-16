using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AdvancedRimTalk.Prompt
{
    public sealed class ArtiPromptPreset : IExposable
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Advanced RimTalk";
        public List<ArtiPromptPart> Parts = new List<ArtiPromptPart>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id", string.Empty);
            Scribe_Values.Look(ref Name, "name", "Advanced RimTalk");
            Scribe_Collections.Look(ref Parts, "parts", LookMode.Deep);
            Normalize();
        }

        public void Normalize()
        {
            if (string.IsNullOrEmpty(Id)) Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(Name)) Name = "Advanced RimTalk";
            if (Parts == null) Parts = new List<ArtiPromptPart>();
            Parts.RemoveAll(part => part == null);
            foreach (var part in Parts) part.Normalize();
        }

        public ArtiPromptPreset Copy(string name)
        {
            return new ArtiPromptPreset { Name = name, Parts = Parts.Select(part => part.Copy()).ToList() };
        }
    }
}
