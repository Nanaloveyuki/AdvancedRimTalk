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
            var missingMirror = new AdvancedRimTalkSettings();
            var recoveredPreset = new ArtiPromptPreset
            {
                Parts = new List<ArtiPromptPart> { new ArtiPromptPart("Saved", PromptRole.System, "keep this") }
            };
            missingMirror.TakeoverPresets.Add(recoveredPreset);
            missingMirror.ActiveTakeoverPresetId = recoveredPreset.Id;
            missingMirror.TakeoverPromptParts = null;
            missingMirror.EnsureTakeoverPromptParts();
            if (missingMirror.GetPrimaryTakeoverSystemDocument() != "keep this")
                throw new Exception("Missing legacy mirror discarded saved preset content.");
            var duplicate = recoveredPreset.Copy("Duplicate");
            duplicate.Id = recoveredPreset.Id;
            missingMirror.TakeoverPresets.Add(duplicate);
            missingMirror.EnsureTakeoverPromptParts();
            if (duplicate.Id == recoveredPreset.Id || missingMirror.ActiveTakeoverPreset != recoveredPreset)
                throw new Exception("Duplicate ID repair changed the active preset.");
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

            var original = settings.ActiveTakeoverPreset;
            if (original.Init.EnabledByPlayer) throw new Exception("Init must default to disabled.");
            original.Init.Title = "Knowledge anchors";
            original.Init.Description = "Author-provided description";
            original.Init.Sources.Add("common_knowledge");
            original.Init.Anchors.Add("world rules");
            original.Init.EnabledByPlayer = true;
            if (!ReferenceEquals(original.Parts, settings.TakeoverPromptParts)) throw new Exception("Legacy parts must migrate without copying away edits.");
            var copy = original.Copy("Second");
            copy.Init.Anchors.Add("copy only");
            if (original.Init.Anchors.Count != 1 || ReferenceEquals(original.Init, copy.Init)
                || ReferenceEquals(original.Init.Sources, copy.Init.Sources))
                throw new Exception("Preset copies must not share init declarations.");
            settings.TakeoverPresets.Add(copy);
            copy.Parts[0].Content = "independent";
            if (original.Parts[0].Content == "independent") throw new Exception("Presets must not share mutable parts.");
            settings.ActivateTakeoverPreset(copy.Id);
            if (settings.GetPrimaryTakeoverSystemDocument() != "independent") throw new Exception("Activation must change runtime parts.");
            settings.ActivateTakeoverPreset(original.Id);
            if (!ReferenceEquals(settings.TakeoverPromptParts, original.Parts)) throw new Exception("Switching back lost original parts.");
            var preview = copy.Copy("Preview");
            preview.Parts[0].Content = "preview mutation";
            if (copy.Parts[0].Content != "independent" || settings.ActiveTakeoverPresetId != original.Id)
                throw new Exception("Preview clone modified source or active preset.");
            settings.AddTakeoverPreset(original.Copy(original.Name));
            settings.AddTakeoverPreset(original.Copy(original.Name));
            if (settings.TakeoverPresets[2].Name == settings.TakeoverPresets[3].Name)
                throw new Exception("Duplicate preset names were not disambiguated.");
            Verse.TestScribe.BeginSave();
            settings.ExposeData();
            var persisted = Verse.TestScribe.Data;
            Verse.TestScribe.BeginLoad(persisted);
            var restored = new AdvancedRimTalkSettings();
            restored.ExposeData();
            if (restored.TakeoverPresets.Count != settings.TakeoverPresets.Count
                || restored.ActiveTakeoverPresetId != original.Id
                || restored.TakeoverPromptParts[0].Content != original.Parts[0].Content)
                throw new Exception("Preset serialization contract lost active state or parts.");
            restored.ActivateTakeoverPreset(copy.Id);
            var restoredInit = restored.ActiveTakeoverPreset.Init;
            if (!restoredInit.EnabledByPlayer || restoredInit.Title != copy.Init.Title
                || restoredInit.Description != copy.Init.Description
                || restoredInit.Sources.Count != 1 || restoredInit.Sources[0] != "common_knowledge"
                || restoredInit.Anchors.Count != 2 || restoredInit.Anchors[1] != "copy only")
                throw new Exception("Init serialization lost declaration or player selection.");
            if (restored.TakeoverPromptParts[0].Content != "independent")
                throw new Exception("Inactive preset content was not persisted.");
            persisted.Remove("takeoverPresets");
            persisted.Remove("activeTakeoverPresetId");
            Verse.TestScribe.BeginLoad(persisted);
            var migrated = new AdvancedRimTalkSettings();
            migrated.ExposeData();
            if (migrated.TakeoverPresets.Count != 1 || migrated.TakeoverPromptParts[0].Content != original.Parts[0].Content)
                throw new Exception("Legacy settings migration lost prompt content.");

            var blocks = AdvancedRimTalk.Documentation.DocumentationBlocks.Parse(
                "| Name | Meaning |\n| --- | :---: |\n| [array](array.md) | `a|b` |\n\n```arti\n| not | a table |\n| --- | --- |\n```\n");
            if (blocks.Count != 5 || !blocks[0].Header || blocks[1].Cells[1] != "`a|b`" || !blocks[3].Code)
                throw new Exception("Table parsing must preserve code fences and inline pipes.");
            var cells = AdvancedRimTalk.Documentation.DocumentationBlocks.Cells("| a\\|b | c |");
            if (cells.Length != 2 || cells[0] != "a|b") throw new Exception("Escaped table pipe was split.");

            var root = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !System.IO.File.Exists(System.IO.Path.Combine(root.FullName, "AdvancedRimTalk.csproj"))) root = root.Parent;
            if (root == null) throw new Exception("Documentation checks require the repository root.");
            var catalog = AdvancedRimTalk.Documentation.AdvancedRimTalkDocumentationCatalog.Create(root.FullName);
            if (!catalog.IsAvailable) throw new Exception("Documentation catalog is unavailable.");
            foreach (string language in new[] { "English", "ChineseSimplified" })
            {
                var translations = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(
                    root.FullName, "Languages", language, "Keyed", "AdvancedRimTalk.xml"));
                foreach (var category in catalog.Categories)
                {
                    string title = translations.Root.Element(category.TitleKey)?.Value;
                    if (string.IsNullOrWhiteSpace(title) || title == category.TitleKey)
                        throw new Exception("Missing documentation title: " + language + " / " + category.TitleKey);
                }
            }
            int documents = 0, links = 0;
            foreach (var document in catalog.Entries)
            {
                documents++;
                if (catalog.Find(document.RelativePath) != document) throw new Exception("Search ID does not resolve to its document.");
                foreach (var block in AdvancedRimTalk.Documentation.DocumentationBlocks.Parse(document.Markdown))
                {
                    if (block.Code) continue;
                    string text = block.Cells == null ? block.Text : string.Join(" ", block.Cells);
                    foreach (System.Text.RegularExpressions.Match match in AdvancedRimTalk.Documentation.DocumentationBlocks.Link.Matches(text))
                    {
                        string path = AdvancedRimTalk.Documentation.AdvancedRimTalkDocumentationCatalog.ResolvePath(document.RelativePath, match.Groups[2].Value);
                        if (path == null || !path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;
                        if (catalog.Find(path) == null) throw new Exception("Unresolvable document link: " + document.RelativePath + " -> " + path);
                        links++;
                    }
                }
            }
            Console.WriteLine("Documentation checks passed: " + documents + " searchable documents, " + links + " local links.");
        }
    }
}

// This checks the serialized field contract using host doubles, not RimWorld's XML loader.
namespace Verse
{
    internal static class TestScribe
    {
        internal static Dictionary<string, object> Data = new Dictionary<string, object>();
        internal static bool Loading;
        internal static void BeginSave() { Loading = false; Data = new Dictionary<string, object>(); }
        internal static void BeginLoad(Dictionary<string, object> data) { Loading = true; Data = data; }
    }
    public interface IExposable { void ExposeData(); }
    public class ModSettings { public virtual void ExposeData() { } }
    public enum LookMode { Deep, Value }
    public static class Scribe_Deep
    {
        public static void Look<T>(ref T value, string name) where T : class, IExposable, new()
        {
            var parent = TestScribe.Data;
            try
            {
                if (TestScribe.Loading)
                {
                    value = null;
                    if (!parent.TryGetValue(name, out var stored) || stored == null) return;
                    TestScribe.Data = (Dictionary<string, object>)stored;
                    value = new T();
                    value.ExposeData();
                }
                else
                {
                    if (value == null) { parent[name] = null; return; }
                    TestScribe.Data = new Dictionary<string, object>();
                    parent[name] = TestScribe.Data;
                    value.ExposeData();
                }
            }
            finally { TestScribe.Data = parent; }
        }
    }
    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string name, T defaultValue)
        {
            if (!TestScribe.Loading) TestScribe.Data[name] = value;
            else value = TestScribe.Data.TryGetValue(name, out var stored) ? (T)stored : defaultValue;
        }
    }
    public static class Scribe_Collections
    {
        public static void Look<T>(ref List<T> value, string name, LookMode mode)
        {
            if (mode == LookMode.Value)
            {
                if (TestScribe.Loading)
                    value = TestScribe.Data.TryGetValue(name, out var stored) && stored != null ? new List<T>((List<T>)stored) : null;
                else TestScribe.Data[name] = value == null ? null : new List<T>(value);
                return;
            }
            var parent = TestScribe.Data;
            try
            {
                if (TestScribe.Loading)
                {
                    value = null;
                    if (!parent.TryGetValue(name, out var stored) || stored == null) return;
                    value = new List<T>();
                    foreach (var node in (List<Dictionary<string, object>>)stored)
                    {
                        TestScribe.Data = node;
                        T item = Activator.CreateInstance<T>();
                        ((IExposable)item).ExposeData();
                        value.Add(item);
                    }
                }
                else
                {
                    var nodes = new List<Dictionary<string, object>>();
                    parent[name] = value == null ? null : nodes;
                    if (value == null) return;
                    foreach (T item in value)
                    {
                        TestScribe.Data = new Dictionary<string, object>();
                        ((IExposable)item).ExposeData();
                        nodes.Add(TestScribe.Data);
                    }
                }
            }
            finally { TestScribe.Data = parent; }
        }
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
