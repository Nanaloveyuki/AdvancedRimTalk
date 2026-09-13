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
            ArtiPromptRenderResult renderedDocument = RenderTakeoverDocument(promptContext);
            string systemPrompt = renderedDocument == null || string.IsNullOrWhiteSpace(renderedDocument.Text)
                ? BuildFallbackSystemPrompt(promptContext)
                : renderedDocument.Text;
            AddMessage(messages, segments, "advancedrimtalk.takeover.system", "Advanced RimTalk System", Role.System, systemPrompt);
            AddMessage(
                messages,
                segments,
                "advancedrimtalk.takeover.request",
                "Advanced RimTalk Request",
                Role.User,
                BuildRequestMessage(promptContext, talkRequest));
            LogDiagnostics(renderedDocument);
            return MergeConsecutiveRoles(messages);
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
            participants = participants ?? new List<Pawn>();
            ValueTuple<string, string, string> dialogueTypeData = talkRequest == null
                ? new ValueTuple<string, string, string>(string.Empty, string.Empty, string.Empty)
                : PromptContextProvider.GetDialogueTypeData(talkRequest, participants);
            string context = PromptService.BuildContext(
                participants,
                talkRequest != null && talkRequest.IsAnnouncement);

            if (talkRequest != null)
            {
                talkRequest.Context = context;
                PromptService.DecoratePrompt(talkRequest, participants, status);
            }

            PromptContext promptContext = talkRequest == null
                ? new PromptContext(participants)
                : PromptContext.FromTalkRequest(talkRequest, participants);
            promptContext.PawnContext = context ?? string.Empty;
            promptContext.DialogueType = dialogueTypeData.Item1 ?? string.Empty;
            promptContext.Intent = dialogueTypeData.Item2 ?? string.Empty;
            promptContext.ConversationTopic = dialogueTypeData.Item3 ?? string.Empty;
            promptContext.DialogueStatus = status ?? string.Empty;
            promptContext.DialoguePrompt = talkRequest == null ? string.Empty : talkRequest.Prompt ?? string.Empty;
            return promptContext;
        }

        private static ArtiPromptRenderResult RenderTakeoverDocument(PromptContext promptContext)
        {
            string document = AdvancedRimTalkMod.Settings == null
                ? AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument
                : AdvancedRimTalkMod.Settings.TakeoverArtiPromptDocument;
            if (string.IsNullOrWhiteSpace(document))
            {
                return null;
            }

            return ArtiPromptDocumentRenderer.Render(document, promptContext);
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

        private static void LogDiagnostics(ArtiPromptRenderResult renderedDocument)
        {
            if (!Prefs.DevMode || renderedDocument == null || renderedDocument.Diagnostics.Count == 0)
            {
                return;
            }

            StringBuilder diagnostics = new StringBuilder();
            foreach (ArtiDiagnostic diagnostic in renderedDocument.Diagnostics)
            {
                if (diagnostics.Length > 0)
                {
                    diagnostics.AppendLine();
                }

                diagnostics.Append(diagnostic);
            }

            Log.Warning("Advanced RimTalk takeover Arti diagnostics:\n" + diagnostics);
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
    }
}
