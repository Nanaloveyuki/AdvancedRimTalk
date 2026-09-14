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
        private static readonly Color CommentColor = new Color(0.42f, 0.55f, 0.47f);
        private static readonly Color KeywordColor = new Color(0.83f, 0.62f, 0.96f);
        private static readonly Color StringColor = new Color(0.9f, 0.73f, 0.43f);
        private static readonly Color NumberColor = new Color(0.48f, 0.78f, 0.91f);
        private static readonly Color BooleanColor = new Color(0.82f, 0.58f, 0.94f);
        private static readonly Color BuiltinColor = new Color(0.38f, 0.77f, 0.84f);
        private static readonly Color IdentifierColor = new Color(0.84f, 0.87f, 0.92f);
        private static readonly Color OperatorColor = new Color(0.78f, 0.81f, 0.86f);
        private static readonly Color PunctuationColor = new Color(0.62f, 0.69f, 0.76f);
        private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f);

        private readonly List<ReplEntry> history = new List<ReplEntry>();
        private readonly ArtiReplSession session = new ArtiReplSession();
        private readonly ArtiEditorIntelligence intelligence = new ArtiEditorIntelligence();
        private Vector2 outputScroll;
        private string input = string.Empty;
        private string analyzedInput;
        private ArtiEditorAnalysis inputAnalysis;
        private int historyIndex;
        private string historyDraft = string.Empty;
        private bool focusEditor = true;
        private bool scrollToBottom;
        private GUIStyle inputStyle;
        private GUIStyle syntaxStyle;
        private float lineAdvance;
        private bool stylesInitialized;

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
                EnsureStyles();

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
                RefreshInputAnalysis();
                DrawInputSyntax(editorRect);
                GUI.SetNextControlName(EditorControlName);
                input = NormalizeLineEndings(GUI.TextArea(editorRect, input, inputStyle));

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
                CalculateTranscriptHeight(Mathf.Max(1f, contentWidth - 8f)) + 8f);
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
                if (history.Count > 0)
                {
                    DrawTranscriptEntries(
                        new Rect(4f, 4f, Mathf.Max(1f, contentWidth - 8f), Mathf.Max(1f, contentHeight - 8f)),
                        Mathf.Max(1f, contentWidth - 8f));
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private float CalculateTranscriptHeight(float width)
        {
            float height = 0f;
            foreach (ReplEntry entry in history)
            {
                height += ArtiEditorText.GetLineCount(entry.Source) * lineAdvance;
                if (!string.IsNullOrEmpty(entry.Result))
                {
                    string[] lines = NormalizeLineEndings(entry.Result).Split(new[] { '\n' }, StringSplitOptions.None);
                    height += Math.Max(1, lines.Length) * lineAdvance;
                }
            }

            return height;
        }

        private void DrawTranscriptEntries(Rect rect, float width)
        {
            float y = rect.y;
            foreach (ReplEntry entry in history)
            {
                y = DrawTranscriptSource(entry, rect.x, y, width);
                if (!string.IsNullOrEmpty(entry.Result))
                {
                    y = DrawResultText(entry.Result, rect.x, y, width);
                }
            }
        }

        private float DrawTranscriptSource(ReplEntry entry, float x, float y, float width)
        {
            string text = NormalizeLineEndings(entry.Source);
            int lineCount = ArtiEditorText.GetLineCount(text);
            int lineStart = 0;
            for (int line = 0; line < lineCount; line++)
            {
                int lineEnd = ArtiEditorText.GetLineEnd(text, lineStart);
                string prompt = line == 0 ? ">>> " : "... ";
                float promptWidth = DrawTextRun(prompt, x, y, AccentColor);
                DrawSyntaxLine(
                    text,
                    entry.Analysis,
                    lineStart,
                    lineEnd,
                    x + promptWidth,
                    y);
                y += lineAdvance;
                lineStart = lineEnd < text.Length ? lineEnd + 1 : text.Length;
            }

            return y;
        }

        private float DrawResultText(string result, float x, float y, float width)
        {
            string[] lines = NormalizeLineEndings(result).Split(new[] { '\n' }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                Color color = line.StartsWith("ART", StringComparison.Ordinal)
                    || line.StartsWith("REPL error", StringComparison.Ordinal)
                    || line.StartsWith("[Advanced", StringComparison.Ordinal)
                        ? InvalidColor
                        : line.StartsWith("warn:", StringComparison.Ordinal)
                            ? AccentColor
                            : IdentifierColor;
                DrawTextRun(line, x, y, color);
                y += lineAdvance;
            }

            return y;
        }

        private void EnsureStyles()
        {
            if (stylesInitialized)
            {
                return;
            }

            inputStyle = new GUIStyle(Text.CurTextAreaStyle);
            inputStyle.alignment = TextAnchor.UpperLeft;
            inputStyle.wordWrap = false;
            inputStyle.richText = false;
            RemoveBackgrounds(inputStyle);
            SetTextColors(inputStyle, new Color(1f, 1f, 1f, 0.01f));

            syntaxStyle = new GUIStyle(inputStyle);
            syntaxStyle.padding = new RectOffset(0, 0, 0, 0);
            syntaxStyle.contentOffset = Vector2.zero;
            SetTextColors(syntaxStyle, Color.white);

            lineAdvance = Mathf.Max(16f, inputStyle.lineHeight);
            stylesInitialized = true;
        }

        private void RefreshInputAnalysis()
        {
            if (inputAnalysis != null
                && string.Equals(analyzedInput, input, StringComparison.Ordinal))
            {
                return;
            }

            inputAnalysis = intelligence.AnalyzeCode(
                input,
                session.GetGlobalNames(),
                true);
            analyzedInput = input;
        }

        private void DrawInputSyntax(Rect rect)
        {
            string text = input ?? string.Empty;
            int lineCount = ArtiEditorText.GetLineCount(text);
            int lineStart = 0;
            for (int line = 0; line < lineCount; line++)
            {
                int lineEnd = ArtiEditorText.GetLineEnd(text, lineStart);
                DrawSyntaxLine(
                    text,
                    inputAnalysis,
                    lineStart,
                    lineEnd,
                    rect.x + inputStyle.padding.left,
                    rect.y + inputStyle.padding.top + line * lineAdvance);
                lineStart = lineEnd < text.Length ? lineEnd + 1 : text.Length;
            }
        }

        private void DrawSyntaxLine(
            string text,
            ArtiEditorAnalysis analysis,
            int lineStart,
            int lineEnd,
            float x,
            float y)
        {
            int position = lineStart;
            if (analysis != null)
            {
                foreach (ArtiHighlightSpan highlight in analysis.Highlights)
                {
                    if (highlight.EndOffset <= lineStart)
                    {
                        continue;
                    }

                    if (highlight.StartOffset >= lineEnd)
                    {
                        break;
                    }

                    int segmentStart = Math.Max(position, Math.Max(lineStart, highlight.StartOffset));
                    int segmentEnd = Math.Min(lineEnd, highlight.EndOffset);
                    if (segmentEnd <= segmentStart)
                    {
                        continue;
                    }

                    if (segmentStart > position)
                    {
                        string plain = text.Substring(position, segmentStart - position);
                        x += DrawTextRun(plain, x, y, IdentifierColor);
                    }

                    string segment = text.Substring(segmentStart, segmentEnd - segmentStart);
                    x += DrawTextRun(segment, x, y, GetSyntaxColor(highlight.Role));
                    position = segmentEnd;
                }
            }

            if (position < lineEnd)
            {
                DrawTextRun(
                    text.Substring(position, lineEnd - position),
                    x,
                    y,
                    IdentifierColor);
            }
        }

        private float DrawTextRun(string text, float x, float y, Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            Vector2 size = syntaxStyle.CalcSize(new GUIContent(text));
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Label(
                new Rect(x, y, Mathf.Max(1f, size.x + 2f), lineAdvance),
                text,
                syntaxStyle);
            GUI.color = previous;
            return size.x;
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
            ArtiEditorAnalysis sourceAnalysis = intelligence.AnalyzeCode(
                source,
                session.GetGlobalNames(),
                true);
            try
            {
                resultText = FormatResult(session.Execute(source));
            }
            catch (Exception exception)
            {
                Log.Error("Advanced RimTalk Arti REPL failed: " + exception);
                resultText = "REPL error: " + exception.Message;
            }

            history.Add(new ReplEntry(source, resultText, sourceAnalysis));
            if (history.Count > MaxHistory)
            {
                history.RemoveAt(0);
            }

            historyIndex = history.Count;

            scrollToBottom = true;
        }

        private void ClearTranscript()
        {
            history.Clear();
            input = string.Empty;
            analyzedInput = null;
            inputAnalysis = null;
            outputScroll = Vector2.zero;
            historyIndex = 0;
            historyDraft = string.Empty;
            session.Reset();
            focusEditor = true;
        }

        private static string FormatResult(ArtiReplExecution replExecution)
        {
            StringBuilder builder = new StringBuilder();
            ArtiExecutionResult execution = replExecution == null ? null : replExecution.Result;
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

            if (replExecution != null && replExecution.Warnings != null)
            {
                foreach (string warning in replExecution.Warnings)
                {
                    if (string.IsNullOrEmpty(warning))
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.Append("warn: ").Append(warning);
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

        private static Color GetSyntaxColor(ArtiSyntaxRole role)
        {
            switch (role)
            {
                case ArtiSyntaxRole.Comment:
                    return CommentColor;
                case ArtiSyntaxRole.Keyword:
                    return KeywordColor;
                case ArtiSyntaxRole.String:
                    return StringColor;
                case ArtiSyntaxRole.Number:
                    return NumberColor;
                case ArtiSyntaxRole.Boolean:
                    return BooleanColor;
                case ArtiSyntaxRole.Builtin:
                    return BuiltinColor;
                case ArtiSyntaxRole.Operator:
                    return OperatorColor;
                case ArtiSyntaxRole.Punctuation:
                    return PunctuationColor;
                case ArtiSyntaxRole.Invalid:
                    return InvalidColor;
                default:
                    return IdentifierColor;
            }
        }

        private static void RemoveBackgrounds(GUIStyle style)
        {
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.onNormal.background = null;
            style.onHover.background = null;
            style.onActive.background = null;
            style.onFocused.background = null;
        }

        private static void SetTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        private sealed class ReplEntry
        {
            public ReplEntry(string source, string result, ArtiEditorAnalysis analysis)
            {
                Source = source ?? string.Empty;
                Result = result ?? string.Empty;
                Analysis = analysis;
            }

            public string Source { get; }
            public string Result { get; }
            public ArtiEditorAnalysis Analysis { get; }
        }
    }

    internal sealed class ArtiReplExecution
    {
        public ArtiReplExecution(ArtiExecutionResult result, IList<string> warnings)
        {
            Result = result;
            Warnings = warnings ?? new List<string>();
        }

        public ArtiExecutionResult Result { get; }
        public IList<string> Warnings { get; }
    }

    internal sealed class ArtiReplSession
    {
        private readonly LiveValueProvider provider = new LiveValueProvider();
        private readonly List<string> warnings = new List<string>();
        private ArtiExecutionContext context;
        private ArtiExecutor executor;
        private Game game;

        public ArtiReplExecution Execute(string source)
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
            warnings.Clear();
            ArtiExecutionResult result = executor.Execute(source, context);
            return new ArtiReplExecution(result, new List<string>(warnings));
        }

        public IEnumerable<string> GetGlobalNames()
        {
            EnsureInitialized();
            return context.Globals.Keys;
        }

        public void Reset()
        {
            if (context != null)
            {
                context.Globals.Clear();
            }

            warnings.Clear();
            executor = new ArtiExecutor();
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
                    AllowGlobalRedeclare = true,
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
                warnings.Add(message ?? string.Empty);
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
