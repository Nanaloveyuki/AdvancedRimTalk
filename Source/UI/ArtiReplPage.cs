using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Integration;
using RimTalk.Prompt;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiReplPage
    {
        private const string EditorControlName = "AdvancedRimTalk.ArtiRepl.Editor";
        private const int MaxHistory = 100;
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.16f);
        private static readonly Color AccentColor = new Color(1f, 0.85f, 0.55f);

        private readonly List<ReplEntry> history = new List<ReplEntry>();
        private readonly ArtiReplSession session = new ArtiReplSession();
        private Vector2 outputScroll;
        private string input = string.Empty;
        private string transcript = string.Empty;
        private int historyIndex;
        private string historyDraft = string.Empty;
        private bool focusEditor = true;
        private bool scrollToBottom;

        public void Draw(Rect inRect)
        {
            if (inRect.width <= 1f || inRect.height <= 1f)
            {
                return;
            }

            GameFont previousFont = Text.Font;
            bool previousWordWrap = Text.WordWrap;
            Color previousColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.WordWrap = true;

                float toolbarHeight = 30f;
                float editorHeight = Mathf.Clamp(inRect.height * 0.28f, 88f, 170f);
                float outputHeight = Mathf.Max(1f, inRect.height - editorHeight - toolbarHeight - 16f);
                Rect outputRect = new Rect(inRect.x, inRect.y, inRect.width, outputHeight);
                DrawTranscript(outputRect);

                float editorY = outputRect.yMax + 8f;
                Rect editorPanel = new Rect(inRect.x, editorY, inRect.width, editorHeight);
                Widgets.DrawBoxSolid(editorPanel, PanelColor);
                GUI.color = AccentColor;
                Widgets.Label(new Rect(editorPanel.x + 6f, editorPanel.y + 6f, 24f, 24f), ">>>");
                GUI.color = Color.white;

                Rect editorRect = new Rect(
                    editorPanel.x + 32f,
                    editorPanel.y + 4f,
                    Mathf.Max(1f, editorPanel.width - 36f),
                    Mathf.Max(1f, editorPanel.height - 8f));
                ProcessEditorShortcuts();
                GUI.SetNextControlName(EditorControlName);
                input = NormalizeLineEndings(Widgets.TextArea(editorRect, input));

                Rect toolbar = new Rect(inRect.x, editorPanel.yMax + 6f, inRect.width, toolbarHeight);
                if (Widgets.ButtonText(new Rect(toolbar.x, toolbar.y, 82f, 26f), "AdvancedRimTalk.ArtiRepl.Run".Translate()))
                {
                    RunInput();
                }

                if (Widgets.ButtonText(new Rect(toolbar.x + 90f, toolbar.y, 82f, 26f), "AdvancedRimTalk.ArtiRepl.Clear".Translate()))
                {
                    ClearTranscript();
                }

                if (focusEditor && Event.current != null && Event.current.type == EventType.Repaint)
                {
                    GUI.FocusControl(EditorControlName);
                    focusEditor = false;
                }
            }
            finally
            {
                Text.Font = previousFont;
                Text.WordWrap = previousWordWrap;
                GUI.color = previousColor;
            }
        }

        private void DrawTranscript(Rect viewport)
        {
            Widgets.DrawBoxSolid(viewport, PanelColor);
            float contentWidth = Mathf.Max(1f, viewport.width - 20f);
            float contentHeight = Mathf.Max(
                viewport.height,
                Text.CalcHeight(transcript, Mathf.Max(1f, contentWidth - 8f)) + 8f);
            if (scrollToBottom)
            {
                outputScroll.y = float.MaxValue;
                scrollToBottom = false;
            }

            Widgets.BeginScrollView(
                viewport,
                ref outputScroll,
                new Rect(0f, 0f, contentWidth, contentHeight));
            try
            {
                if (transcript.Length > 0)
                {
                    Widgets.Label(
                        new Rect(4f, 4f, Mathf.Max(1f, contentWidth - 8f), Mathf.Max(1f, contentHeight - 8f)),
                        transcript);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void ProcessEditorShortcuts()
        {
            Event current = Event.current;
            if (current == null
                || current.type != EventType.KeyDown
                || !string.Equals(GUI.GetNameOfFocusedControl(), EditorControlName, StringComparison.Ordinal))
            {
                return;
            }

            bool command = current.control || current.command;
            bool isEnter = current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter;
            if (isEnter && !current.shift && !current.alt)
            {
                current.Use();
                RunInput();
                return;
            }

            if (ProcessHistoryNavigation(current))
            {
                return;
            }

            if (!command)
            {
                return;
            }

            if (current.shift && current.keyCode == KeyCode.C)
            {
                TextEditor editor = GetEditor();
                if (editor != null)
                {
                    editor.text = input;
                    GUIUtility.systemCopyBuffer = editor.hasSelection ? editor.SelectedText : input;
                }
                else
                {
                    GUIUtility.systemCopyBuffer = input;
                }

                current.Use();
                return;
            }

            if (current.shift && current.keyCode == KeyCode.V)
            {
                PasteIntoEditor();
                current.Use();
                return;
            }

            if (current.shift && current.keyCode == KeyCode.X)
            {
                CutEditorSelection();
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.L && !current.shift)
            {
                ClearTranscript();
                current.Use();
                return;
            }

            TextEditor focusedEditor = GetEditor();
            if (focusedEditor == null)
            {
                return;
            }

            focusedEditor.text = input;
            if (current.keyCode == KeyCode.A && !current.shift)
            {
                focusedEditor.SelectAll();
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.C && !current.shift)
            {
                if (focusedEditor.hasSelection)
                {
                    GUIUtility.systemCopyBuffer = focusedEditor.SelectedText;
                }

                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.V && !current.shift)
            {
                PasteIntoEditor(focusedEditor);
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.X && !current.shift)
            {
                CutEditorSelection(focusedEditor);
                current.Use();
            }
        }

        private bool ProcessHistoryNavigation(Event current)
        {
            if (current == null
                || current.shift
                || current.alt
                || current.control
                || current.command
                || (current.keyCode != KeyCode.UpArrow && current.keyCode != KeyCode.DownArrow)
                || history.Count == 0)
            {
                return false;
            }

            if (historyIndex >= history.Count && input.Length > 0)
            {
                return false;
            }

            if (historyIndex < 0 || historyIndex > history.Count)
            {
                historyIndex = history.Count;
            }

            if (current.keyCode == KeyCode.UpArrow)
            {
                if (historyIndex == history.Count)
                {
                    historyDraft = input;
                }

                if (historyIndex > 0)
                {
                    historyIndex--;
                    input = history[historyIndex].Source;
                }
            }
            else if (historyIndex < history.Count - 1)
            {
                historyIndex++;
                input = history[historyIndex].Source;
            }
            else
            {
                historyIndex = history.Count;
                input = historyDraft;
            }

            current.Use();
            return true;
        }

        private TextEditor GetEditor()
        {
            if (GUIUtility.keyboardControl == 0)
            {
                return null;
            }

            return GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
        }

        private void PasteIntoEditor()
        {
            PasteIntoEditor(GetEditor());
        }

        private void PasteIntoEditor(TextEditor editor)
        {
            if (editor == null)
            {
                return;
            }

            editor.text = input;
            if (editor.Paste())
            {
                input = NormalizeLineEndings(editor.text);
            }
        }

        private void CutEditorSelection()
        {
            CutEditorSelection(GetEditor());
        }

        private void CutEditorSelection(TextEditor editor)
        {
            if (editor == null)
            {
                return;
            }

            editor.text = input;
            if (!editor.hasSelection)
            {
                return;
            }

            GUIUtility.systemCopyBuffer = editor.SelectedText;
            editor.DeleteSelection();
            input = NormalizeLineEndings(editor.text);
        }

        private void RunInput()
        {
            string source = NormalizeLineEndings(input).TrimEnd('\r', '\n');
            input = string.Empty;
            historyIndex = history.Count;
            historyDraft = string.Empty;
            focusEditor = true;
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            string resultText;
            try
            {
                resultText = FormatResult(session.Execute(source));
            }
            catch (Exception exception)
            {
                Log.Error("Advanced RimTalk Arti REPL failed: " + exception);
                resultText = "REPL error: " + exception.Message;
            }

            history.Add(new ReplEntry(source, resultText));
            if (history.Count > MaxHistory)
            {
                history.RemoveAt(0);
            }

            historyIndex = history.Count;

            RebuildTranscript();
            scrollToBottom = true;
        }

        private void ClearTranscript()
        {
            history.Clear();
            transcript = string.Empty;
            outputScroll = Vector2.zero;
            historyIndex = 0;
            historyDraft = string.Empty;
        }

        private void RebuildTranscript()
        {
            StringBuilder builder = new StringBuilder();
            foreach (ReplEntry entry in history)
            {
                string source = NormalizeLineEndings(entry.Source);
                string[] lines = source.Split(new[] { '\n' }, StringSplitOptions.None);
                builder.Append(">>> ").AppendLine(lines.Length == 0 ? string.Empty : lines[0]);
                for (int index = 1; index < lines.Length; index++)
                {
                    builder.Append("... ").AppendLine(lines[index]);
                }

                if (entry.Result.Length > 0)
                {
                    builder.AppendLine(entry.Result);
                }
            }

            transcript = builder.ToString().TrimEnd('\r', '\n');
        }

        private static string FormatResult(ArtiExecutionResult execution)
        {
            StringBuilder builder = new StringBuilder();
            string output = execution == null ? string.Empty : NormalizeLineEndings(execution.Output).TrimEnd('\r', '\n');
            if (output.Length > 0)
            {
                builder.Append(output);
            }
            else if (execution != null && execution.Value != null)
            {
                builder.Append(FormatValue(execution.Value));
            }

            if (execution != null && execution.Diagnostics != null)
            {
                foreach (ArtiDiagnostic diagnostic in execution.Diagnostics)
                {
                    if (diagnostic == null || builder.ToString().IndexOf(diagnostic.Code, StringComparison.Ordinal) >= 0)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.Append(diagnostic.ToString());
                }
            }

            return builder.ToString();
        }

        private static string FormatValue(object value)
        {
            return FormatValue(value, 0);
        }

        private static string FormatValue(object value, int depth)
        {
            if (value == null)
            {
                return "null";
            }

            string text = value as string;
            if (text != null)
            {
                return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
            }

            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }

            if (depth >= 2)
            {
                return "...";
            }

            Thing thing = value as Thing;
            if (thing != null)
            {
                return thing.LabelShort ?? (thing.def == null ? "thing" : thing.def.defName);
            }

            Map map = value as Map;
            if (map != null)
            {
                return "Map#" + map.uniqueID.ToString(CultureInfo.InvariantCulture);
            }

            Def def = value as Def;
            if (def != null)
            {
                return def.defName ?? "def";
            }

            RimTalkArtiLazyNamespace lazy = value as RimTalkArtiLazyNamespace;
            if (lazy != null)
            {
                return "{" + string.Join(", ", lazy.GetMemberNames().ToArray()) + "}";
            }

            if (value is IArtiCallable)
            {
                return "<callable>";
            }

            IDictionary dictionary = value as IDictionary;
            if (dictionary != null)
            {
                return FormatDictionary(dictionary, depth);
            }

            IList list = value as IList;
            if (list != null)
            {
                return FormatList(list, depth);
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                return FormatEnumerable(enumerable, depth);
            }

            IFormattable formattable = value as IFormattable;
            if (formattable != null)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            try
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
            catch (Exception)
            {
                return "<value>";
            }
        }

        private static string FormatDictionary(IDictionary dictionary, int depth)
        {
            if (depth >= 2)
            {
                return "{...}";
            }

            StringBuilder builder = new StringBuilder("{");
            int count = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (count++ >= 32)
                {
                    builder.Append(", ...");
                    break;
                }

                if (count > 1)
                {
                    builder.Append(", ");
                }

                builder.Append(FormatValue(entry.Key, depth + 1))
                    .Append(": ")
                    .Append(FormatValue(entry.Value, depth + 1));
            }

            return builder.Append('}').ToString();
        }

        private static string FormatList(IList list, int depth)
        {
            StringBuilder builder = new StringBuilder("[");
            int count = Math.Min(list.Count, 32);
            for (int index = 0; index < count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(FormatValue(list[index], depth + 1));
            }

            if (list.Count > count)
            {
                builder.Append(", ...");
            }

            return builder.Append(']').ToString();
        }

        private static string FormatEnumerable(IEnumerable enumerable, int depth)
        {
            StringBuilder builder = new StringBuilder("[");
            IEnumerator iterator = null;
            int count = 0;
            try
            {
                iterator = enumerable.GetEnumerator();
                while (count < 32 && iterator.MoveNext())
                {
                    if (count > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(FormatValue(iterator.Current, depth + 1));
                    count++;
                }

                if (iterator.MoveNext())
                {
                    builder.Append(", ...");
                }
            }
            catch (Exception)
            {
                builder.Append("<unavailable>");
            }
            finally
            {
                IDisposable disposable = iterator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            return builder.Append(']').ToString();
        }

        private static string NormalizeLineEndings(string value)
        {
            return (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private sealed class ReplEntry
        {
            public ReplEntry(string source, string result)
            {
                Source = source ?? string.Empty;
                Result = result ?? string.Empty;
            }

            public string Source { get; }
            public string Result { get; }
        }
    }

    internal sealed class ArtiReplSession
    {
        private readonly LiveValueProvider provider = new LiveValueProvider();
        private ArtiExecutionContext context;
        private ArtiExecutor executor;
        private Game game;

        public ArtiExecutionResult Execute(string source)
        {
            EnsureInitialized();
            Game currentGame = GetCurrentGame();
            if (!ReferenceEquals(game, currentGame))
            {
                game = currentGame;
                context.Globals.Clear();
                executor = new ArtiExecutor();
            }

            provider.Refresh(CreatePromptContext());
            return executor.Execute(source, context);
        }

        private void EnsureInitialized()
        {
            if (context != null)
            {
                return;
            }

            context = new ArtiExecutionContext(
                valueProvider: provider,
                moduleCatalog: RimTalkArtiCatalog.CreateModuleCatalog(),
                symbolCatalog: RimTalkArtiCatalog.CreateSymbolCatalog(),
                options: new ArtiExecutionOptions
                {
                    PersistVariables = true,
                    IncludeRuntimeErrorsInOutput = true,
                    MaxSteps = 10000,
                    MaxCallDepth = 64
                });
            context.GetVariable = delegate(string key)
            {
                return ScribanParser.GetSessionVar(key);
            };
            context.SetVariable = delegate(string key, string value)
            {
                ScribanParser.SetSessionVar(key, value);
            };
            context.WarningSink = delegate(string message)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("Advanced RimTalk Arti REPL: " + message);
                }
            };
            executor = new ArtiExecutor();
        }

        private static Game GetCurrentGame()
        {
            try
            {
                return Current.Game;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static PromptContext CreatePromptContext()
        {
            List<Pawn> selectedPawns = new List<Pawn>();
            try
            {
                if (Find.Selector != null)
                {
                    List<Pawn> selected = Find.Selector.SelectedPawns;
                    if (selected != null)
                    {
                        foreach (Pawn pawn in selected)
                        {
                            if (pawn != null)
                            {
                                selectedPawns.Add(pawn);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            if (selectedPawns.Count > 0)
            {
                return new PromptContext(selectedPawns);
            }

            PromptContext context = new PromptContext();
            Map map = null;
            try
            {
                map = Find.CurrentMap;
            }
            catch (Exception)
            {
            }

            context.Map = map;
            if (map != null && map.mapPawns != null && map.mapPawns.AllPawns != null)
            {
                context.AllPawns = new List<Pawn>(map.mapPawns.AllPawns);
            }

            return context;
        }

        private sealed class LiveValueProvider : IArtiRuntimeValueProvider, IArtiCoreModuleProvider, IArtiRuntimeModuleProvider
        {
            private RimTalkArtiRuntimeValueProvider current;

            public void Refresh(PromptContext context)
            {
                current = new RimTalkArtiRuntimeValueProvider(context);
            }

            public bool TryGetGlobal(string name, out object value)
            {
                if (current == null)
                {
                    value = null;
                    return false;
                }

                return current.TryGetGlobal(name, out value);
            }

            public bool TryGetMember(object target, string member, out object value)
            {
                if (current == null)
                {
                    value = null;
                    return false;
                }

                return current.TryGetMember(target, member, out value);
            }

            public bool TryGetIndex(object target, object index, out object value)
            {
                if (current == null)
                {
                    value = null;
                    return false;
                }

                return current.TryGetIndex(target, index, out value);
            }

            public bool TryGetCoreMember(string member, out object value)
            {
                if (current == null)
                {
                    value = null;
                    return false;
                }

                return current.TryGetCoreMember(member, out value);
            }

            public bool TryGetModuleValue(string packageId, ArtiModuleInfo module, out object value)
            {
                if (current == null)
                {
                    value = null;
                    return false;
                }

                return current.TryGetModuleValue(packageId, module, out value);
            }
        }
    }
}
