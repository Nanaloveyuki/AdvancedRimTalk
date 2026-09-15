using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Integration;
using AdvancedRimTalk.Settings;
using RimTalk.Data;
using RimTalk.Prompt;
using RimTalk.Service;
using Verse;

namespace AdvancedRimTalk.Prompt
{
    internal static class AdvancedPromptMessageBuilder
    {
        private static readonly MethodInfo LastContextSetter = typeof(PromptManager)
            .GetProperty("LastContext", BindingFlags.Public | BindingFlags.Static)
            ?.GetSetMethod(true);

        public static List<ValueTuple<Role, string>> Build(TalkRequest talkRequest, List<Pawn> pawns, string status, out List<PromptMessageSegment> segments)
        {
            List<Pawn> participants = NormalizeParticipants(talkRequest, pawns);
            if (talkRequest != null && talkRequest.Participants == null && participants.Count > 0)
            {
                talkRequest.Participants = participants;
            }

            PromptContext promptContext = CreateRimTalkContext(talkRequest, participants, status);
            SetLastContext(promptContext);
            ScribanParser.ResetSessionVariables();

            segments = new List<PromptMessageSegment>();
            List<ValueTuple<Role, string>> messages = new List<ValueTuple<Role, string>>();
            List<RenderedPromptPart> renderedParts = RenderTakeoverPromptParts(promptContext);
            foreach (RenderedPromptPart renderedPart in renderedParts)
            {
                AddMessage(
                    messages,
                    segments,
                    renderedPart.Part.Id,
                    renderedPart.Part.Name,
                    renderedPart.Part.ToMessageRole(),
                    renderedPart.Part.ApplyCustomRolePrefix(renderedPart.RenderResult.Text));
            }

            if (messages.Count == 0)
            {
                AddMessage(messages, segments, "advancedrimtalk.takeover.system", "Advanced RimTalk System", Role.System, BuildFallbackSystemPrompt(promptContext));
                AddMessage(
                    messages,
                    segments,
                    "advancedrimtalk.takeover.request",
                    "Advanced RimTalk Request",
                    Role.User,
                    BuildRequestMessage(promptContext, talkRequest));
            }

            LogDiagnostics(renderedParts);
            return FinalizeMessages(messages, segments);
        }

        internal static List<ValueTuple<Role, string>> FinalizeMessages(List<ValueTuple<Role, string>> messages, List<PromptMessageSegment> segments)
        {
            return MergeConsecutiveRoles(ApplyCharacterBudget(messages, segments));
        }

        private static List<ValueTuple<Role, string>> ApplyCharacterBudget(List<ValueTuple<Role, string>> messages, List<PromptMessageSegment> segments)
        {
            int budget = AdvancedRimTalkMod.Settings == null ? 24000 : AdvancedRimTalkMod.Settings.TakeoverPromptCharacterBudget;
            List<string> contents = new List<string>();
            foreach (ValueTuple<Role, string> message in messages) contents.Add(message.Item2);
            // Reserve separators before merging adjacent roles; removed parts can create new adjacency.
            int contentBudget = Math.Max(0, budget - Math.Max(0, messages.Count - 1) * 2);
            string[] bounded = PromptCharacterBudget.Apply(contents, contentBudget);
            List<ValueTuple<Role, string>> result = new List<ValueTuple<Role, string>>();
            bool truncated = false;
            for (int index = 0; index < messages.Count; index++)
            {
                string text = bounded[index];
                truncated |= text != contents[index];
                segments[index].Content = text;
                if (text.Length == 0) continue;
                result.Add(new ValueTuple<Role, string>(messages[index].Item1, text));
            }
            segments.RemoveAll(segment => string.IsNullOrEmpty(segment.Content));
            if (truncated) Log.Warning("Advanced RimTalk trimmed prompt parts to the configured character budget (" + budget + ").");
            return result;
        }

        private static string BuildSystemInstruction()
        {
            return "You are writing in-character RimWorld colonist dialogue.\n"
                + "Return JSONL only. Each line must be a JSON object with keys \"name\" and \"text\".\n"
                + "Use participant names exactly as provided. Do not include Markdown, code fences, or commentary.";
        }

        private static string BuildFallbackSystemPrompt(PromptContext promptContext)
        {
            string context = promptContext == null ? string.Empty : promptContext.PawnContext;
            if (string.IsNullOrWhiteSpace(context))
            {
                return BuildSystemInstruction();
            }

            return BuildSystemInstruction()
                + "\n\nCurrent RimWorld context:\n"
                + context.Trim();
        }

        private static PromptContext CreateRimTalkContext(TalkRequest talkRequest, List<Pawn> participants, string status)
        {
            using (RimTalkTakeoverContextScope.Enter())
            {
                participants = participants ?? new List<Pawn>();
                string originalPrompt = talkRequest == null ? string.Empty : talkRequest.Prompt ?? string.Empty;
                string rawPrompt = talkRequest == null ? string.Empty : talkRequest.RawPrompt ?? originalPrompt;
                ValueTuple<string, string, string> dialogueTypeData = talkRequest == null
                    ? new ValueTuple<string, string, string>(string.Empty, string.Empty, string.Empty)
                    : PromptContextProvider.GetDialogueTypeData(talkRequest, participants);
                if (talkRequest != null)
                {
                    talkRequest.Prompt = originalPrompt;
                }

                string context = PromptService.BuildContext(
                    participants,
                    talkRequest != null && talkRequest.IsAnnouncement);

                if (talkRequest != null)
                {
                    talkRequest.Context = context;
                }

                PromptContext promptContext = talkRequest == null
                    ? new PromptContext(participants)
                    : PromptContext.FromTalkRequest(talkRequest, participants);
                promptContext.PawnContext = context ?? string.Empty;
                promptContext.DialogueType = dialogueTypeData.Item1 ?? string.Empty;
                promptContext.Intent = dialogueTypeData.Item2 ?? string.Empty;
                promptContext.ConversationTopic = dialogueTypeData.Item3 ?? string.Empty;
                promptContext.DialogueStatus = status ?? string.Empty;
                promptContext.DialoguePrompt = rawPrompt ?? string.Empty;
                return promptContext;
            }
        }

        private static List<RenderedPromptPart> RenderTakeoverPromptParts(PromptContext promptContext)
        {
            AdvancedRimTalkSettings settings = AdvancedRimTalkMod.Settings;
            List<ArtiPromptPart> parts;
            if (settings == null)
            {
                parts = ArtiPromptPart.CreateDefaultParts(AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument);
            }
            else
            {
                settings.EnsureTakeoverPromptParts();
                parts = settings.TakeoverPromptParts;
            }

            List<RenderedPromptPart> rendered = new List<RenderedPromptPart>();
            if (parts == null)
            {
                return rendered;
            }

            foreach (ArtiPromptPart part in parts)
            {
                if (part == null || !part.Enabled || string.IsNullOrWhiteSpace(part.Content))
                {
                    continue;
                }

                part.Normalize();
                ArtiPromptRenderResult arti = ArtiPromptDocumentRenderer.Render(part.Content, promptContext);
                if (arti != null && !string.IsNullOrEmpty(arti.Text) && arti.Text.Contains("{{"))
                {
                    try
                    {
                        arti = new ArtiPromptRenderResult(ScribanParser.Render(arti.Text, promptContext, true), arti.Diagnostics);
                    }
                    catch (Exception exception)
                    {
                        Log.Warning("Advanced RimTalk takeover Scriban rendering failed: " + exception);
                    }
                }
                rendered.Add(new RenderedPromptPart(part, arti));
            }

            return rendered;
        }

        private static string BuildRequestMessage(PromptContext promptContext, TalkRequest talkRequest)
        {
            StringBuilder builder = new StringBuilder();
            AppendHistory(builder, promptContext == null ? null : promptContext.ChatHistory);

            string prompt = promptContext == null ? null : promptContext.DialoguePrompt;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                prompt = talkRequest == null ? null : talkRequest.RawPrompt;
            }

            if (builder.Length > 0 && !string.IsNullOrWhiteSpace(prompt))
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                builder.Append("Generate the next dialogue for the current RimWorld situation.");
            }
            else
            {
                builder.Append(prompt.Trim());
            }

            return builder.ToString().Trim();
        }

        private static void AppendHistory(
            StringBuilder builder,
            IList<ValueTuple<Role, string>> history)
        {
            if (history == null || history.Count == 0)
            {
                return;
            }

            builder.AppendLine("Conversation history (reference only; do not repeat or continue):");
            for (int index = 0; index < history.Count; index++)
            {
                string message = (history[index].Item2 ?? string.Empty)
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ");
                builder.Append("- ")
                    .Append(index + 1)
                    .Append(" | role=")
                    .Append(history[index].Item1)
                    .Append(" | text=")
                    .AppendLine(message);
            }
        }

        private static List<Pawn> NormalizeParticipants(TalkRequest talkRequest, List<Pawn> pawns)
        {
            List<Pawn> participants = new List<Pawn>();
            if (pawns != null)
            {
                AddDistinct(participants, pawns);
            }
            else if (talkRequest != null && talkRequest.Participants != null)
            {
                AddDistinct(participants, talkRequest.Participants);
            }

            if (talkRequest != null)
            {
                AddDistinct(participants, talkRequest.Initiator);
                AddDistinct(participants, talkRequest.Recipient);
            }

            return participants;
        }

        private static void AddDistinct(List<Pawn> pawns, IEnumerable<Pawn> candidates)
        {
            foreach (Pawn candidate in candidates)
            {
                AddDistinct(pawns, candidate);
            }
        }

        private static void AddDistinct(List<Pawn> pawns, Pawn candidate)
        {
            if (candidate == null || pawns.Contains(candidate))
            {
                return;
            }

            pawns.Add(candidate);
        }

        private static void AddMessage(List<ValueTuple<Role, string>> messages, List<PromptMessageSegment> segments, string entryId, string entryName, Role role, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            string trimmed = content.Trim();
            messages.Add(new ValueTuple<Role, string>(role, trimmed));
            segments.Add(new PromptMessageSegment(entryId, entryName, role, trimmed));
        }

        private static List<ValueTuple<Role, string>> MergeConsecutiveRoles(List<ValueTuple<Role, string>> messages)
        {
            if (messages == null || messages.Count <= 1)
            {
                return messages;
            }

            List<ValueTuple<Role, string>> merged = new List<ValueTuple<Role, string>>();
            foreach (ValueTuple<Role, string> message in messages)
            {
                if (merged.Count > 0 && merged[merged.Count - 1].Item1 == message.Item1)
                {
                    ValueTuple<Role, string> previous = merged[merged.Count - 1];
                    merged[merged.Count - 1] = new ValueTuple<Role, string>(previous.Item1, previous.Item2 + "\n\n" + message.Item2);
                    continue;
                }

                merged.Add(message);
            }

            return merged;
        }

        private static void LogDiagnostics(List<RenderedPromptPart> renderedParts)
        {
            if (!Prefs.DevMode || renderedParts == null || renderedParts.Count == 0)
            {
                return;
            }

            StringBuilder diagnostics = new StringBuilder();
            foreach (RenderedPromptPart renderedPart in renderedParts)
            {
                if (renderedPart.RenderResult == null || renderedPart.RenderResult.Diagnostics.Count == 0)
                {
                    continue;
                }

                foreach (ArtiDiagnostic diagnostic in renderedPart.RenderResult.Diagnostics)
                {
                    if (diagnostics.Length > 0)
                    {
                        diagnostics.AppendLine();
                    }

                    diagnostics
                        .Append(renderedPart.Part.Name)
                        .Append(": ")
                        .Append(diagnostic);
                }
            }

            if (diagnostics.Length > 0)
            {
                Log.Warning("Advanced RimTalk takeover Arti diagnostics:\n" + diagnostics);
            }
        }

        private static void SetLastContext(PromptContext context)
        {
            if (LastContextSetter == null)
            {
                return;
            }

            try
            {
                LastContextSetter.Invoke(null, new object[] { context });
            }
            catch (Exception exception)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("Advanced RimTalk could not update PromptManager.LastContext: " + exception);
                }
            }
        }

        private sealed class RenderedPromptPart
        {
            public RenderedPromptPart(ArtiPromptPart part, ArtiPromptRenderResult renderResult)
            {
                Part = part;
                RenderResult = renderResult;
            }

            public ArtiPromptPart Part { get; }
            public ArtiPromptRenderResult RenderResult { get; }
        }
    }
}
