using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Prompt;
using AdvancedRimTalk.UI;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiPartEditorWindowChecks
    {
        internal static void Run()
        {
            CheckDocumentIdentity();
            CheckSelectionAndClose();
            CheckSessionTransfer();
            CheckClosedWorkspaceReopens();
            CheckPreviousDefinitions();
            CheckCrossPresetDefinitions();
        }

        private static void CheckDocumentIdentity()
        {
            var firstPart = new ArtiPromptPart { Id = "part", Name = "Shared name", Content = "first" };
            var secondPart = new ArtiPromptPart { Id = "part", Name = "Shared name", Content = "second" };
            var firstPreset = new ArtiPromptPreset { Id = "preset", Name = "Shared preset", Parts = new List<ArtiPromptPart> { firstPart } };
            var secondPreset = new ArtiPromptPreset { Id = "preset", Name = "Shared preset", Parts = new List<ArtiPromptPart> { secondPart } };
            var workspace = new ArtiEditorWorkspace();
            ArtiEditorSession first = workspace.Open(firstPreset, firstPart);
            ArtiEditorSession second = workspace.Open(secondPreset, secondPart);
            if (workspace.Sessions.Count != 2 || ReferenceEquals(first, second) || ReferenceEquals(first.Editor, second.Editor))
                throw new Exception("Different preset documents must remain independent even when IDs and names match.");
            if (!ReferenceEquals(workspace.Open(firstPreset, firstPart), first) || !ReferenceEquals(workspace.Active, first)
                || workspace.Sessions.Count != 2 || !workspace.Sessions.SequenceEqual(new[] { first, second }))
                throw new Exception("Opening an existing document must select it without duplication or reordering.");
            firstPreset.Name = "Renamed preset";
            firstPart.Name = "Renamed part";
            if (first.Title != "Renamed preset / Renamed part" || second.Title != "Shared preset / Shared name")
                throw new Exception("Session titles must follow their own preset and part names.");

            var samePresetPart = new ArtiPromptPart { Id = firstPart.Id, Name = firstPart.Name };
            firstPreset.Parts.Add(samePresetPart);
            ArtiEditorSession third = workspace.Open(firstPreset, samePresetPart);
            if (ReferenceEquals(first, third) || workspace.Sessions.Count != 3)
                throw new Exception("Distinct part objects in one preset must not be merged by ID or name.");
            ArtiEditorSession sharedPart = workspace.Open(secondPreset, firstPart);
            if (ReferenceEquals(first, sharedPart) || workspace.Sessions.Count != 4)
                throw new Exception("Document identity must include the preset object as well as the part object.");
            workspace.Add(new ArtiEditorSession(firstPreset, firstPart));
            if (!ReferenceEquals(workspace.Active, first) || workspace.Sessions.Count != 4)
                throw new Exception("Adding another session for an open document must select the existing session.");
            workspace.Add(first);
            if (workspace.Sessions.Count != 4 || !ReferenceEquals(workspace.Sessions[0], first))
                throw new Exception("Adding an existing session must not duplicate or reorder it.");
        }

        private static void CheckSelectionAndClose()
        {
            var preset = new ArtiPromptPreset();
            var workspace = new ArtiEditorWorkspace();
            ArtiEditorSession first = workspace.Open(preset, new ArtiPromptPart());
            ArtiEditorSession second = workspace.Open(preset, new ArtiPromptPart());
            ArtiEditorSession third = workspace.Open(preset, new ArtiPromptPart());
            ArtiEditorSession fourth = workspace.Open(preset, new ArtiPromptPart());
            if (!workspace.Select(second) || !ReferenceEquals(workspace.Active, second))
                throw new Exception("Selecting a document must make it active.");
            var foreign = new ArtiEditorSession(preset, new ArtiPromptPart());
            if (workspace.Select(foreign) || workspace.Remove(foreign) || !ReferenceEquals(workspace.Active, second)
                || workspace.Sessions.Count != 4)
                throw new Exception("Unknown sessions must not change workspace selection or contents.");
            if (!workspace.Remove(second) || !ReferenceEquals(workspace.Active, third)
                || !workspace.Sessions.SequenceEqual(new[] { first, third, fourth }))
                throw new Exception("Closing an active middle document must select the next surviving document.");
            if (!workspace.Remove(first) || !ReferenceEquals(workspace.Active, third))
                throw new Exception("Closing an inactive document must preserve the active document.");
            workspace.Select(fourth);
            if (!workspace.Remove(fourth) || !ReferenceEquals(workspace.Active, third))
                throw new Exception("Closing the final active tab must select the preceding surviving document.");
            if (workspace.Remove(second) || !ReferenceEquals(workspace.Active, third))
                throw new Exception("Closing an already removed document must not change selection.");
        }

        private static void CheckSessionTransfer()
        {
            var preset = new ArtiPromptPreset();
            var source = new ArtiEditorWorkspace();
            var destination = new ArtiEditorWorkspace();
            ArtiEditorSession survivor = source.Open(preset, new ArtiPromptPart());
            ArtiEditorSession moving = source.Open(preset, new ArtiPromptPart { Content = "before detach" });
            ArtiEditorSession destinationTab = destination.Open(new ArtiPromptPreset(), new ArtiPromptPart());
            ArtiCodeEditorPage editor = moving.Editor;
            if (!source.Remove(moving) || !ReferenceEquals(source.Active, survivor))
                throw new Exception("Detaching an active document must select a surviving source tab.");
            destination.Add(moving);
            if (!ReferenceEquals(destination.Active, moving) || !ReferenceEquals(destination.Active.Editor, editor)
                || !destination.Sessions.SequenceEqual(new[] { destinationTab, moving })
                || source.Sessions.Contains(moving))
                throw new Exception("Transfer must move the same session and editor, not recreate them.");
            if (!ReferenceEquals(destination.Open(preset, moving.Part), moving))
                throw new Exception("Repeat-open must find the transferred session.");
            destination.Remove(moving);
            source.Add(moving);
            if (!ReferenceEquals(source.Active, moving) || !ReferenceEquals(source.Active.Editor, editor)
                || !ReferenceEquals(destination.Active, destinationTab))
                throw new Exception("Reattaching a document must retain its session and editor identity.");
        }

        private static void CheckClosedWorkspaceReopens()
        {
            var preset = new ArtiPromptPreset();
            var part = new ArtiPromptPart { Content = "keep document" };
            var workspace = new ArtiEditorWorkspace();
            if (workspace.Active != null || workspace.Sessions.Count != 0)
                throw new Exception("A new workspace must be empty.");
            ArtiEditorSession first = workspace.Open(preset, part);
            if (!workspace.Remove(first) || workspace.Active != null || workspace.Sessions.Count != 0)
                throw new Exception("Closing all documents must clear the workspace and active selection.");
            ArtiEditorSession reopened = workspace.Open(preset, part);
            if (workspace.Sessions.Count != 1 || !ReferenceEquals(workspace.Active, reopened)
                || ReferenceEquals(first, reopened) || part.Content != "keep document")
                throw new Exception("An emptied workspace must allow later opening without stale sessions or lost document text.");
        }

        private static void CheckCrossPresetDefinitions()
        {
            var firstPart = new ArtiPromptPart { Id = "part", Name = "Current", Content = "{{% let value = first_fn(first_title) %}}" };
            var secondPart = new ArtiPromptPart { Id = "part", Name = "Current", Content = "{{% let value = second_fn(second_title) %}}" };
            var firstIntro = new ArtiPromptPart { Content = "{{% fn first_fn(value) { return value }; const first_title = 'first' %}}" };
            var secondIntro = new ArtiPromptPart { Content = "{{% fn second_fn(value) { return value }; const second_title = 'second' %}}" };
            var firstPreset = new ArtiPromptPreset { Id = "preset", Name = "Same preset", Parts = new List<ArtiPromptPart> { firstIntro, firstPart } };
            var secondPreset = new ArtiPromptPreset { Id = "preset", Name = "Same preset", Parts = new List<ArtiPromptPart> { secondIntro, secondPart } };
            var workspace = new ArtiEditorWorkspace();
            ArtiEditorSession first = workspace.Open(firstPreset, firstPart);
            ArtiEditorSession second = workspace.Open(secondPreset, secondPart);
            var context = new ArtiPromptAnalysisContext();
            CheckSessionDefinitions(first, context, "first_fn", "first_title", "second_fn(second_title)");
            CheckSessionDefinitions(second, context, "second_fn", "second_title", "first_fn(first_title)");
            workspace.Select(first);
            CheckSessionDefinitions(workspace.Active, context, "first_fn", "first_title", "second_fn(second_title)");
            var detached = new ArtiEditorWorkspace();
            workspace.Remove(first);
            detached.Add(first);
            CheckSessionDefinitions(detached.Active, context, "first_fn", "first_title", "second_fn(second_title)");
            secondIntro.Content = "{{% fn second_renamed(value) { return value } %}}";
            if (context.Refresh(detached.Active.Preset.Parts, detached.Active.Part)
                || !new HashSet<string>(context.Names).SetEquals(new[] { "first_fn", "first_title" }))
                throw new Exception("Changes to another preset must not invalidate or alter detached document declarations.");
        }

        private static void CheckSessionDefinitions(ArtiEditorSession session, ArtiPromptAnalysisContext context,
            string function, string constant, string foreignExpression)
        {
            context.Refresh(session.Preset.Parts, session.Part);
            if (!new HashSet<string>(context.Names).SetEquals(new[] { function, constant })
                || !context.Functions.SequenceEqual(new[] { function }) || !context.Constants.SequenceEqual(new[] { constant }))
                throw new Exception("Preceding declaration context must belong only to the session's preset.");
            ArtiParseResult parsed = new ArtiDocumentParser().Parse(session.Part.Content).CodeBlocks[0].ParseResult;
            var analyzer = new ArtiAnalyzer(externalGlobals: context.Names, externalConstants: context.Constants,
                externalFunctions: context.Functions);
            if (parsed.HasErrors || analyzer.Analyze(parsed.Program).HasErrors)
                throw new Exception("A document must be able to use its own preset's preceding declarations.");
            if (!new ArtiAnalyzer(externalGlobals: context.Names, externalConstants: context.Constants,
                externalFunctions: context.Functions).Analyze(new ArtiParser().Parse("let foreign = " + foreignExpression).Program).HasErrors)
                throw new Exception("A document must not resolve preceding declarations from another preset.");
        }

        private static void CheckPreviousDefinitions()
        {
            var intro = new ArtiPromptPart { Content = "{{% fn mod_status(id) { return true, id }; const title = \"test\"; let private_value = 1 %}}" };
            var disabled = new ArtiPromptPart { Enabled = false, Content = "{{% fn disabled_fn() { return false } %}}" };
            var current = new ArtiPromptPart { Content = "{{% let status, _ = mod_status(title) %}}" };
            var later = new ArtiPromptPart { Content = "{{% fn later_fn() { return true } %}}" };
            var parts = new List<ArtiPromptPart> { intro, disabled, current, later };
            var context = new ArtiPromptAnalysisContext();
            if (!context.Refresh(parts, current) || !new HashSet<string>(context.Names).SetEquals(new[] { "mod_status", "title" }))
                throw new Exception("Part analysis must collect only earlier enabled top-level fn/const names.");
            var parsed = new ArtiDocumentParser().Parse(current.Content).CodeBlocks[0].ParseResult;
            if (new ArtiAnalyzer(externalGlobals: context.Names, externalConstants: context.Constants,
                externalFunctions: context.Functions).Analyze(parsed.Program).HasErrors)
                throw new Exception("Earlier function and constant remain unavailable to current part.");
            if (!context.Constants.SequenceEqual(new[] { "title" }) || !context.Functions.SequenceEqual(new[] { "mod_status" }))
                throw new Exception("Part analysis lost definition types.");
            foreach (string code in new[] { "const next = title + '!'", "title = 'changed'", "mod_status = 1" })
            {
                var analysis = new ArtiAnalyzer(externalGlobals: context.Names, externalConstants: context.Constants,
                    externalFunctions: context.Functions).Analyze(new ArtiParser().Parse(code).Program);
                if (analysis.HasErrors == code.StartsWith("const"))
                    throw new Exception("Cross-part constant/function metadata: " + code);
            }
            if (context.Refresh(parts, current)) throw new Exception("Unchanged part context should stay cached.");
            intro.Enabled = false;
            if (!context.Refresh(parts, current) || context.Names.Any() || context.Constants.Any() || context.Functions.Any())
                throw new Exception("Disabled definitions remained cached.");
            intro.Enabled = true;
            intro.Content = "{{% fn renamed() { const hidden = 1 }; if true { fn nested() { return 1 } } %}}";
            if (!context.Refresh(parts, current) || !context.Names.SequenceEqual(new[] { "renamed" }))
                throw new Exception("Renamed definitions or nested declarations leaked into context.");
            parts.Remove(intro); parts.Add(intro);
            if (!context.Refresh(parts, current) || context.Names.Any()) throw new Exception("Reordered definitions remained visible.");
            context.Refresh(parts, new ArtiPromptPart());
            if (context.Names.Any()) throw new Exception("Detached editor collected unrelated parts.");
            intro.Content = "{{% fn broken( %}}";
            parts.Remove(intro); parts.Insert(0, intro);
            context.Refresh(parts, current);
            if (context.Names.Any()) throw new Exception("Invalid preceding block exported names.");
        }
    }
}


namespace UnityEngine
{
    internal struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
    }
    internal struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
    }
    internal static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); }
}

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiCodeEditorPage
    {
        public ArtiCodeEditorPage(ArtiPromptPart part, ArtiPromptPreset preset) { }
        public void Activate() { }
        public void Suspend() { }
    }
}
