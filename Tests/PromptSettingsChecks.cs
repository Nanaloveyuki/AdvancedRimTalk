using System;
using System.Collections.Generic;
using AdvancedRimTalk.Prompt;
using AdvancedRimTalk.Settings;
using RimTalk.Prompt;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class PromptSettingsChecks
    {
        internal static void Run()
        {
            var settings = new AdvancedRimTalkSettings();
            settings.EnsureTakeoverPromptParts();
            int count = settings.TakeoverPromptParts.Count;
            settings.SetPrimaryTakeoverSystemDocument("changed");
            if (settings.GetPrimaryTakeoverSystemDocument() != "changed"
                || settings.TakeoverPromptParts[0].Content != "changed"
                || settings.TakeoverPromptParts.Count != count)
                throw new Exception("Primary editor must write the existing part.");
            settings.TakeoverPromptParts[0].Content = "part edit";
            if (settings.GetPrimaryTakeoverSystemDocument() != "part edit")
                throw new Exception("Primary editor must read the part, not a separate document.");
            settings.TakeoverPromptParts = new List<ArtiPromptPart> { new ArtiPromptPart("User", PromptRole.User, "request") };
            if (settings.GetPrimaryTakeoverSystemDocument() != string.Empty)
                throw new Exception("Absent system part must not display unexecuted content.");
            settings.SetPrimaryTakeoverSystemDocument("new system");
            if (settings.TakeoverPromptParts.Count != 2 || settings.TakeoverPromptParts[1].Content != "request")
                throw new Exception("Adding a system part changed the user part.");

            string source = "{{ if pawn }}{{ pawn.name }}{{ end }} {{ chat.history }}";
            var imported = ArtiPromptPart.FromRimTalkEntry(new PromptEntry
            {
                Content = source, Role = PromptRole.Assistant, Enabled = false, CustomRole = "Narrator"
            }, "Imported");
            if (imported.Content != source || imported.Enabled || imported.Role != PromptRole.Assistant
                || imported.CustomRole != "Narrator") throw new Exception("Import must preserve source and part metadata.");
        }
    }
}

// Persistence and game types are host doubles; these checks exercise settings logic, not Scribe round-trips.
namespace Verse
{
    public interface IExposable { void ExposeData(); }
    public class ModSettings { public virtual void ExposeData() { } }
    public enum LookMode { Deep }
    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string name, T defaultValue) { }
    }
    public static class Scribe_Collections
    {
        public static void Look<T>(ref List<T> value, string name, LookMode mode) { }
    }
}

namespace RimTalk.Data { public enum Role { System, User, AI } }
namespace RimTalk.Prompt
{
    public enum PromptRole { System, User, Assistant }
    public sealed class PromptEntry
    {
        public string Content, CustomRole;
        public PromptRole Role;
        public bool Enabled;
    }
}
