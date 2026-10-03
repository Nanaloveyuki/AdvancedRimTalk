using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AdvancedRimTalk.Integration;
using RimTalk.Data;
using RimTalk.Prompt;
using RimTalk.Service;
using Verse;

namespace AdvancedRimTalk.Prompt
{
    internal static class ContextPromptPreview
    {
        internal static PromptPreview Build(bool takeover, string request, out string diagnostics,
            ArtiPromptPreset takeoverPreset = null, PromptPreset rimTalkPreset = null)
        {
            if (Current.ProgramState != ProgramState.Playing || Current.Game == null)
                throw new InvalidOperationException("Context preview requires a loaded game.");
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn
                ?? Find.CurrentMap?.mapPawns.FreeColonistsSpawned.FirstOrDefault();
            PromptContext context = new PromptContext(pawn, new VariableStore())
            {
                IsPreview = true,
                Map = pawn?.Map ?? Find.CurrentMap,
                DialoguePrompt = request ?? string.Empty,
                TalkRequest = new TalkRequest(request ?? string.Empty, pawn)
            };
            List<string> errors = new List<string>();
            List<ValueTuple<Role, string>> messages = new List<ValueTuple<Role, string>>();
            using (new PromptPreviewSession())
            using (RimTalkTakeoverContextScope.Enter(takeover))
            {
                if (pawn != null)
                {
                    Action<string, Action> attempt = (name, read) =>
                    {
                        try { read(); }
                        catch (Exception exception) { errors.Add(name + ": " + exception.GetBaseException().Message); }
                    };
                    context.TalkRequest.Participants = context.AllPawns;
                    attempt("Dialogue context", () =>
                    {
                        var dialogue = PromptContextProvider.GetDialogueTypeData(context.TalkRequest, context.AllPawns);
                        context.DialogueType = dialogue.Item1;
                        context.Intent = dialogue.Item2;
                        context.ConversationTopic = dialogue.Item3;
                    });
                    context.TalkRequest.Prompt = request ?? string.Empty;
                    attempt("Pawn context", () =>
                    {
                        context.PawnContext = PromptService.CreatePawnContext(pawn, PromptService.InfoLevel.Normal);
                        context.TalkRequest.Context = context.PawnContext;
                    });
                    if (!takeover)
                        attempt("Request decoration", () =>
                        {
                            PromptService.DecoratePrompt(context.TalkRequest, context.AllPawns, string.Empty);
                            context.DialoguePrompt = context.TalkRequest.Prompt;
                        });
                    attempt("History", () =>
                    {
                        context.GetChatHistory(false);
                        context.GetChatHistory(true);
                    });
                }
                var runtime = new AdvancedRimTalk.Arti.ArtiGlobalRuntime();
                Func<string, string> render = source =>
                {
                    try
                    {
                        ArtiPromptRenderResult arti = ArtiPromptDocumentRenderer.Render(source, context, runtime);
                        foreach (var diagnostic in arti.Diagnostics) errors.Add(diagnostic.ToString());
                        string text = arti.TemplateText;
                        PromptExpansionResult expansion = null;
                        if (AdvancedRimTalkMod.IsPlaceholderLayerEnabled)
                        {
                            expansion = PromptTemplateExpander.Expand(text, PromptContextSnapshotFactory.From(context));
                            text = expansion.Template;
                            foreach (string error in expansion.Errors) errors.Add(error);
                            if (expansion.Errors.Count > 0) return string.Empty;
                        }
                        var parsed = Scriban.Template.Parse(text);
                        if (parsed.HasErrors)
                        {
                            errors.Add(string.Join("\n", parsed.Messages));
                            return string.Empty;
                        }
                        string result = ScribanParser.Render(text, context, false);
                        if (result == text && text.Contains("{{"))
                        {
                            errors.Add("Unresolved Scriban entry skipped.");
                            return string.Empty;
                        }
                        return arti.Restore(expansion == null ? result : expansion.Restore(result));
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception.GetBaseException().Message);
                        return string.Empty;
                    }
                };
                if (takeover)
                {
                    List<PromptMessageSegment> segments = new List<PromptMessageSegment>();
                    AdvancedRimTalkMod.Settings.EnsureTakeoverPromptParts();
                    foreach (ArtiPromptPart part in (takeoverPreset ?? AdvancedRimTalkMod.Settings.ActiveTakeoverPreset).Copy("Preview").Parts)
                    {
                        if (!part.Enabled) continue;
                        string text = render(part.Content);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            text = part.ApplyCustomRolePrefix(text).Trim();
                            messages.Add((part.ToMessageRole(), text));
                            segments.Add(new PromptMessageSegment(part.Id, part.Name, part.ToMessageRole(), text));
                        }
                    }
                    messages = AdvancedPromptMessageBuilder.FinalizeMessages(messages, segments);
                }
                else
                {
                    PromptPreset preset = rimTalkPreset ?? PromptManager.Instance.GetActivePreset();
                    if (preset == null) throw new InvalidOperationException("No active RimTalk preset.");
                    var assembled = AssembleEmbedMessages(preset, context, render,
                        preset.Id == PromptManager.Instance.GetActivePreset()?.Id);
                    foreach (var message in assembled) messages.Add(((Role)message.Item1, message.Item2));
                }
            }
            diagnostics = string.Join("\n", errors);
            return PromptPreview.FromMessages(messages);
        }

        internal static List<ValueTuple<PromptRole, string>> AssembleEmbedMessages(
            PromptPreset preset, PromptContext context, Func<string, string> render, bool activePreset)
        {
            var settings = RimTalk.Settings.Get();
            Type compactAssembler = typeof(PromptManager).Assembly.GetType("RimTalk.Prompt.PromptPresetAssembler", true);
            preset = preset.Clone();
            if (activePreset && !settings.UseAdvancedPromptMode)
            {
                MethodInfo simplePreset = compactAssembler.GetMethod("BuildSimpleModePreset", BindingFlags.Static | BindingFlags.NonPublic);
                preset = (PromptPreset)simplePreset.Invoke(null, new object[]
                {
                    preset, settings.SimpleModeInstruction, Constant.DefaultInstruction, "{{ json.format }}"
                });
            }
            bool compact = settings.Context?.UseCompactHistory ?? true;
            PromptEntry historyEntry = preset.Entries.FirstOrDefault(entry => entry.Enabled
                && entry.Position == PromptPosition.Relative && entry.IsMainChatHistory);
            List<ValueTuple<Role, string>> history = historyEntry == null ? null
                : context.GetChatHistory(compact || (historyEntry.Content ?? string.Empty)
                    .IndexOf("history_simplified", StringComparison.OrdinalIgnoreCase) >= 0);
            Type assembler = compact ? compactAssembler
                : typeof(PromptManager).Assembly.GetType("RimTalk.Prompt.LegacyMultiTurnPromptBuilder", true);
            MethodInfo assemble = assembler.GetMethod("AssembleMessages", BindingFlags.Static | BindingFlags.NonPublic);
            return (List<ValueTuple<PromptRole, string>>)assemble.Invoke(null,
                new object[] { preset, render, history, null });
        }
    }
}
