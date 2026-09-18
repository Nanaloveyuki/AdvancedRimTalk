using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Settings;
using AdvancedRimTalk.Prompt;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ArtiCodeEditorPage
    {
        private const string EditorControlName = "AdvancedRimTalk.ArtiEditor.Editor";
        private const float ToolbarHeight = 30f;
        private const float PanelGap = 6f;

        private static readonly Color EditorPanelColor = new Color(0.035f, 0.045f, 0.06f, 0.98f);
        private static readonly Color DiagnosticsPanelColor = new Color(0.05f, 0.055f, 0.065f, 0.98f);
        private static readonly Color CurrentLineColor = new Color(0.16f, 0.19f, 0.25f, 0.72f);
        private static readonly Color CompletionPanelColor = new Color(0.07f, 0.08f, 0.11f, 0.99f);
        private static readonly Color CompletionSelectedColor = new Color(0.18f, 0.25f, 0.36f, 0.98f);

        private static readonly Color MarkerColor = new Color(0.95f, 0.68f, 0.3f);
        private static readonly Color CommentColor = new Color(0.42f, 0.55f, 0.47f);
        private static readonly Color NumberColor = new Color(0.48f, 0.78f, 0.91f);
        private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f);

        private readonly ArtiEditorIntelligence intelligence = new ArtiEditorIntelligence();
        private readonly ArtiPromptAnalysisContext promptAnalysisContext = new ArtiPromptAnalysisContext();
        private readonly List<ArtiCompletionItem> completions = new List<ArtiCompletionItem>();
        private readonly List<EditorSnapshot> undoStack = new List<EditorSnapshot>();
        private readonly List<EditorSnapshot> redoStack = new List<EditorSnapshot>();

        private Vector2 editorScroll;
        private Vector2 diagnosticsScroll;
        private string source = string.Empty;
        private string lastBoundSource = string.Empty;
        private string analyzedSource;
        private ArtiEditorAnalysis analysis;
        private bool initialized;
        private bool focusEditor = true;
        private bool pendingCaret;
        private int editorControlId;
        private int pendingCursor;
        private int pendingSelect;
        private int completionStart = -1;
        private int completionSelected;
        private string completionPrefix = string.Empty;

        private GUIStyle inputStyle;
        private GUIStyle syntaxStyle;
        private GUIStyle lineNumberStyle;
        private GUIStyle diagnosticStyle;
        private float lineAdvance;
        private bool stylesInitialized;
        private Vector2 editorViewportSize;
        private string measuredSource;
        private float measuredMaxLineWidth;
        private readonly ArtiPromptPart boundPart;

        public ArtiCodeEditorPage(ArtiPromptPart part = null)
        {
            boundPart = part;
        }

        public void Draw(Rect inRect)
        {
            if (inRect.width <= 1f || inRect.height <= 1f)
            {
                return;
            }

            AdvancedRimTalkSettings settings = AdvancedRimTalk.AdvancedRimTalkMod.Settings;
            if (settings == null)
            {
                return;
            }

            GameFont previousFont = Text.Font;
            bool previousWordWrap = Text.WordWrap;
            Color previousColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.WordWrap = false;
                SyncFromSettings(settings);
                EnsureStyles();
                RefreshAnalysis();

                float diagnosticsHeight = Mathf.Clamp(inRect.height * 0.27f, 78f, 180f);
                float editorHeight = Mathf.Max(
                    84f,
                    inRect.height - ToolbarHeight - diagnosticsHeight - PanelGap * 2f);
                if (editorHeight + diagnosticsHeight + ToolbarHeight + PanelGap * 2f > inRect.height)
                {
                    diagnosticsHeight = Mathf.Max(
                        52f,
                        inRect.height - ToolbarHeight - editorHeight - PanelGap * 2f);
                }

                Rect toolbar = new Rect(inRect.x, inRect.y, inRect.width, ToolbarHeight);
                DrawToolbar(toolbar);

                Rect editorPanel = new Rect(
                    inRect.x,
                    toolbar.yMax + PanelGap,
                    inRect.width,
                    editorHeight);
                DrawEditorPanel(editorPanel);

                Rect diagnosticsPanel = new Rect(
                    inRect.x,
                    editorPanel.yMax + PanelGap,
                    inRect.width,
                    diagnosticsHeight);
                DrawDiagnosticsPanel(diagnosticsPanel);
            }
            finally
            {
                Text.Font = previousFont;
                Text.WordWrap = previousWordWrap;
                GUI.color = previousColor;
            }
        }

        private void DrawToolbar(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, DiagnosticsPanelColor);
            string status;
            if (analysis == null || (analysis.ErrorCount == 0 && analysis.WarningCount == 0))
            {
                status = "AdvancedRimTalk.ArtiEditor.NoDiagnostics".Translate().ToString();
            }
            else
            {
                status = "AdvancedRimTalk.ArtiEditor.DiagnosticSummary".Translate(
                    analysis.ErrorCount,
                    analysis.WarningCount).ToString();
            }

            GUI.color = analysis != null && analysis.ErrorCount > 0
                ? InvalidColor
                : analysis != null && analysis.WarningCount > 0
                    ? MarkerColor
                    : CommentColor;
            GUI.Label(
                new Rect(rect.x + 8f, rect.y + 4f, Mathf.Max(1f, rect.width - 96f), 22f),
                status,
                diagnosticStyle);
            GUI.color = Color.white;

            if (Widgets.ButtonText(
                new Rect(rect.xMax - 82f, rect.y + 2f, 78f, 26f),
                "AdvancedRimTalk.ArtiEditor.Reset".Translate().ToString()))
            {
                ResetDocument();
            }
        }

        private void DrawEditorPanel(Rect panel)
        {
            Widgets.DrawBoxSolid(panel, EditorPanelColor);
            Rect viewport = new Rect(
                panel.x + 2f,
                panel.y + 2f,
                Mathf.Max(1f, panel.width - 4f),
                Mathf.Max(1f, panel.height - 4f));
            editorViewportSize = new Vector2(viewport.width, viewport.height);

            int lineCount = ArtiEditorText.GetLineCount(source);
            float gutterWidth = Mathf.Max(
                32f,
                Text.CalcSize(lineCount.ToString()).x + 18f);
            float maxLineWidth = GetMaxLineWidth();
            float minimumCodeWidth = Mathf.Max(80f, viewport.width - gutterWidth - 18f);
            float contentWidth = Mathf.Max(
                viewport.width,
                gutterWidth + maxLineWidth + inputStyle.padding.left + inputStyle.padding.right + 28f);
            float contentHeight = Mathf.Max(
                viewport.height,
                lineCount * lineAdvance + inputStyle.padding.top + inputStyle.padding.bottom + 8f);
            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
            bool pointerInViewport = Event.current != null && viewport.Contains(Event.current.mousePosition);

            Widgets.BeginScrollView(viewport, ref editorScroll, viewRect);
            try
            {
                DrawEditorContent(viewRect, gutterWidth, minimumCodeWidth, pointerInViewport);
            }
            finally
            {
                Widgets.EndScrollView();
            }

            if (focusEditor && Event.current != null && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl(EditorControlName);
                editorControlId = GUIUtility.keyboardControl;
                focusEditor = false;
            }
        }

        private void DrawEditorContent(Rect viewRect, float gutterWidth, float minimumCodeWidth, bool pointerInViewport)
        {
            ProcessEditorShortcuts();
            // The overlay must consume pointer events before TextArea moves the caret.
            if (pointerInViewport) DrawCompletionPopup(gutterWidth, viewRect.width, true);
            TextEditor editor = GetEditor();
            if (editor != null
                && HasEditorFocus())
            {
                editor.text = source;
                ApplyPendingCaret(editor);
            }

            int cursor = editor == null ? source.Length : Clamp(editor.cursorIndex, 0, source.Length);
            int cursorLine = ArtiEditorText.GetLineIndex(source, cursor);
            Widgets.DrawBoxSolid(
                new Rect(
                    gutterWidth,
                    LineY(cursorLine),
                    Mathf.Max(minimumCodeWidth, viewRect.width - gutterWidth),
                    lineAdvance),
                CurrentLineColor);

            DrawLineNumbers(gutterWidth);
            DrawSyntax(gutterWidth);

            Rect codeRect = new Rect(
                gutterWidth,
                0f,
                Mathf.Max(minimumCodeWidth, viewRect.width - gutterWidth),
                viewRect.height);
            string before = source;
            TextEditor beforeEditor = GetEditor();
            int beforeCursor = beforeEditor == null
                ? source.Length
                : Clamp(beforeEditor.cursorIndex, 0, source.Length);
            int beforeSelect = beforeEditor == null
                ? beforeCursor
                : Clamp(beforeEditor.selectIndex, 0, source.Length);

            GUI.SetNextControlName(EditorControlName);
            string edited = GUI.TextArea(codeRect, source, inputStyle);
            if (string.Equals(GUI.GetNameOfFocusedControl(), EditorControlName, StringComparison.Ordinal))
                editorControlId = GUIUtility.keyboardControl;
            HandleTextAreaResult(
                before,
                ArtiEditorText.NormalizeLineEndings(edited),
                beforeCursor,
                beforeSelect);

            DrawDiagnosticDecorations(gutterWidth);
            DrawCompletionPopup(gutterWidth, viewRect.width, false);
        }

        private void DrawLineNumbers(float gutterWidth)
        {
            int lineCount = ArtiEditorText.GetLineCount(source);
            ArtiSyntaxRendering.GetVisibleLineRange(
                inputStyle.padding.top,
                lineAdvance,
                lineCount,
                VisibleEditorRect(),
                out int firstLine,
                out int lastExclusive);
            for (int line = firstLine; line < lastExclusive; line++)
            {
                Rect lineRect = new Rect(
                    4f,
                    line * lineAdvance + inputStyle.padding.top,
                    Mathf.Max(1f, gutterWidth - 10f),
                    lineAdvance);
                GUI.color = new Color(0.42f, 0.47f, 0.55f, 0.9f);
                GUI.Label(
                    lineRect,
                    (line + 1).ToString(),
                    lineNumberStyle);
            }

            GUI.color = Color.white;
        }

        private void DrawSyntax(float gutterWidth)
        {
            ArtiSyntaxRendering.DrawSyntax(
                new Rect(
                    gutterWidth + inputStyle.padding.left,
                    inputStyle.padding.top,
                    1f,
                    1f),
                source,
                analysis,
                syntaxStyle,
                lineAdvance,
                VisibleEditorRect());
        }

        private void DrawDiagnosticDecorations(float gutterWidth)
        {
            if (analysis == null)
            {
                return;
            }

            ArtiSyntaxRendering.GetVisibleLineRange(
                inputStyle.padding.top,
                lineAdvance,
                ArtiEditorText.GetLineCount(source),
                VisibleEditorRect(),
                out int firstLine,
                out int lastExclusive);
            foreach (ArtiDiagnostic diagnostic in analysis.Diagnostics)
            {
                if (diagnostic == null)
                {
                    continue;
                }

                int start = Math.Max(0, Math.Min(diagnostic.Span.StartOffset, source.Length));
                int line = ArtiEditorText.GetLineIndex(source, start);
                if (line < firstLine || line >= lastExclusive)
                {
                    continue;
                }
                int end = Math.Max(start, Math.Min(diagnostic.Span.EndOffset, source.Length));
                int lineStart = ArtiEditorText.GetLineStart(source, start);
                int lineEnd = ArtiEditorText.GetLineEnd(source, start);
                int underlineStart = Math.Max(lineStart, Math.Min(start, lineEnd));
                int underlineEnd = Math.Min(
                    lineEnd,
                    Math.Max(
                        underlineStart,
                        end == start ? underlineStart + 1 : end));
                float x = gutterWidth
                    + inputStyle.padding.left
                    + MeasureText(source.Substring(lineStart, underlineStart - lineStart));
                float width = underlineEnd > underlineStart
                    ? Math.Max(
                        3f,
                        MeasureText(source.Substring(underlineStart, underlineEnd - underlineStart)))
                    : 3f;
                Color color = GetDiagnosticColor(diagnostic.Severity);
                Widgets.DrawBoxSolid(
                    new Rect(
                        x,
                        LineY(ArtiEditorText.GetLineIndex(source, start)) + lineAdvance - 2f,
                        width,
                        2f),
                    color);
                Widgets.DrawBoxSolid(
                    new Rect(
                        Math.Max(1f, gutterWidth - 5f),
                        LineY(ArtiEditorText.GetLineIndex(source, start)) + 5f,
                        3f,
                        3f),
                    color);
            }
        }

        private void DrawCompletionPopup(float gutterWidth, float contentWidth, bool inputOnly)
        {
            ValidateCompletion(GetEditor());
            if (completions.Count == 0 || completionStart < 0)
            {
                return;
            }

            TextEditor editor = GetEditor();
            int cursor = editor == null ? source.Length : Clamp(editor.cursorIndex, 0, source.Length);
            int lineStart = ArtiEditorText.GetLineStart(source, cursor);
            int line = ArtiEditorText.GetLineIndex(source, cursor);
            float x = gutterWidth
                + inputStyle.padding.left
                + MeasureText(source.Substring(lineStart, cursor - lineStart));
            float y = line * lineAdvance + inputStyle.padding.top + lineAdvance;
            float width = Mathf.Min(260f, Mathf.Max(170f, contentWidth - x - 8f));
            float rowHeight = 21f;
            float height = completions.Count * rowHeight + 4f;
            x = Mathf.Max(gutterWidth, Mathf.Min(x, contentWidth - width - 4f));
            Rect popup = new Rect(x, y, width, height);
            Event current = Event.current;
            if (inputOnly)
            {
                if (current != null && popup.Contains(current.mousePosition))
                {
                    int hovered = Clamp((int)((current.mousePosition.y - y - 2f) / rowHeight), 0, completions.Count - 1);
                    if (current.type == EventType.MouseMove) completionSelected = hovered;
                    if (current.type == EventType.MouseDown && current.button == 0)
                    {
                        ApplyCompletion(hovered);
                        current.Use();
                    }
                }
                return;
            }
            Widgets.DrawBoxSolid(popup, CompletionPanelColor);
            for (int index = 0; index < completions.Count; index++)
            {
                ArtiCompletionItem item = completions[index];
                Rect row = new Rect(x + 2f, y + 2f + index * rowHeight, width - 4f, rowHeight);
                if (index == completionSelected)
                {
                    Widgets.DrawBoxSolid(row, CompletionSelectedColor);
                }

                GUI.color = Color.white;
                GUI.Label(
                    new Rect(row.x + 5f, row.y + 1f, row.width * 0.62f, row.height),
                    item.Label,
                    syntaxStyle);
                GUI.color = CommentColor;
                GUI.Label(
                    new Rect(row.x + row.width * 0.62f, row.y + 1f, row.width * 0.36f, row.height),
                    item.Detail,
                    lineNumberStyle);
            }

            GUI.color = Color.white;
        }

        private void DrawDiagnosticsPanel(Rect panel)
        {
            Widgets.DrawBoxSolid(panel, DiagnosticsPanelColor);
            int count = analysis == null ? 0 : analysis.Diagnostics.Count;
            if (count == 0)
            {
                GUI.color = CommentColor;
                GUI.Label(
                    new Rect(panel.x + 8f, panel.y + 6f, panel.width - 16f, 22f),
                    "AdvancedRimTalk.ArtiEditor.NoDiagnostics".Translate().ToString(),
                    diagnosticStyle);
                GUI.color = Color.white;
                return;
            }

            float rowHeight = 22f;
            float contentHeight = Mathf.Max(panel.height, count * rowHeight + 4f);
            Rect viewport = new Rect(
                panel.x + 2f,
                panel.y + 2f,
                Mathf.Max(1f, panel.width - 4f),
                Mathf.Max(1f, panel.height - 4f));
            Widgets.BeginScrollView(
                viewport,
                ref diagnosticsScroll,
                new Rect(0f, 0f, Mathf.Max(1f, viewport.width - 18f), contentHeight));
            try
            {
                for (int index = 0; index < count; index++)
                {
                    ArtiDiagnostic diagnostic = analysis.Diagnostics[index];
                    if (diagnostic == null)
                    {
                        continue;
                    }

                    Rect row = new Rect(
                        4f,
                        2f + index * rowHeight,
                        Mathf.Max(1f, viewport.width - 26f),
                        rowHeight);
                    if (Widgets.ButtonInvisible(row, false))
                    {
                        MoveCaretTo(diagnostic.Span.StartOffset);
                    }

                    GUI.color = GetDiagnosticColor(diagnostic.Severity);
                    GUI.Label(row, FormatDiagnostic(diagnostic), diagnosticStyle);
                }
            }
            finally
            {
                GUI.color = Color.white;
                Widgets.EndScrollView();
            }
        }

        private void SyncFromSettings(AdvancedRimTalkSettings settings)
        {
            string configured = ArtiEditorText.NormalizeLineEndings(
                boundPart == null ? settings.GetPrimaryTakeoverSystemDocument() : boundPart.Content);
            if (!initialized)
            {
                source = configured;
                lastBoundSource = configured;
                initialized = true;
                return;
            }

            if (!string.Equals(configured, lastBoundSource, StringComparison.Ordinal)
                && !string.Equals(configured, source, StringComparison.Ordinal))
            {
                source = configured;
                lastBoundSource = configured;
                analyzedSource = null;
                analysis = null;
                completions.Clear();
                completionStart = -1;
                undoStack.Clear();
                redoStack.Clear();
                focusEditor = true;
            }
            else if (string.Equals(configured, source, StringComparison.Ordinal))
            {
                lastBoundSource = configured;
            }
        }

        private void RefreshAnalysis()
        {
            var settings = AdvancedRimTalkMod.Settings;
            var parts = settings == null ? null : settings.TakeoverPromptParts;
            var current = boundPart ?? parts?.Find(part => part != null && part.Role == RimTalk.Prompt.PromptRole.System);
            bool contextChanged = promptAnalysisContext.Refresh(parts, current);
            if (!contextChanged && analysis != null
                && string.Equals(analyzedSource, source, StringComparison.Ordinal))
            {
                return;
            }

            analysis = intelligence.AnalyzeDocument(source, promptAnalysisContext.Names,
                externalConstants: promptAnalysisContext.Constants, externalFunctions: promptAnalysisContext.Functions);
            if (contextChanged) ClearCompletion();
            analyzedSource = source;
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

            lineNumberStyle = new GUIStyle(syntaxStyle);
            lineNumberStyle.alignment = TextAnchor.UpperRight;

            diagnosticStyle = new GUIStyle(syntaxStyle);
            diagnosticStyle.alignment = TextAnchor.MiddleLeft;

            lineAdvance = Mathf.Max(16f, inputStyle.lineHeight);
            stylesInitialized = true;
        }

        private float LineY(int line)
        {
            return inputStyle.padding.top + Math.Max(0, line) * lineAdvance;
        }

        private Rect VisibleEditorRect()
        {
            return new Rect(
                editorScroll.x,
                editorScroll.y,
                Mathf.Max(1f, editorViewportSize.x),
                Mathf.Max(1f, editorViewportSize.y));
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

        private float GetMaxLineWidth()
        {
            if (measuredSource != null
                && string.Equals(measuredSource, source, StringComparison.Ordinal))
            {
                return measuredMaxLineWidth;
            }

            float width = 0f;
            int lineStart = 0;
            while (lineStart <= source.Length)
            {
                int lineEnd = ArtiEditorText.GetLineEnd(source, lineStart);
                width = Mathf.Max(
                    width,
                    MeasureText(source.Substring(lineStart, lineEnd - lineStart)));
                if (lineEnd >= source.Length)
                {
                    break;
                }

                lineStart = lineEnd + 1;
            }

            measuredSource = source;
            measuredMaxLineWidth = width;
            return width;
        }

        private float MeasureText(string text)
        {
            return string.IsNullOrEmpty(text)
                ? 0f
                : syntaxStyle.CalcSize(new GUIContent(text)).x;
        }

        private static Color GetDiagnosticColor(ArtiDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case ArtiDiagnosticSeverity.Error:
                    return InvalidColor;
                case ArtiDiagnosticSeverity.Warning:
                    return MarkerColor;
                default:
                    return NumberColor;
            }
        }

        private static string FormatDiagnostic(ArtiDiagnostic diagnostic)
        {
            ArtiDiagnosticLanguage language = IsChineseLanguage()
                ? ArtiDiagnosticLanguage.ChineseSimplified
                : ArtiDiagnosticLanguage.English;
            return diagnostic.Code
                + "  "
                + diagnostic.Span.Line
                + ":"
                + diagnostic.Span.Column
                + "  "
                + diagnostic.GetMessage(language);
        }

        private static bool IsChineseLanguage()
        {
            string language = Prefs.LangFolderName;
            return !string.IsNullOrEmpty(language)
                && language.IndexOf("Chinese", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(value, maximum));
        }

        private TextEditor GetEditor()
        {
            int control = editorControlId != 0 ? editorControlId : GUIUtility.keyboardControl;
            if (control == 0)
            {
                return null;
            }

            return GUIUtility.GetStateObject(typeof(TextEditor), control) as TextEditor;
        }

        private bool HasEditorFocus()
        {
            return editorControlId != 0 && GUIUtility.keyboardControl == editorControlId
                || string.Equals(GUI.GetNameOfFocusedControl(), EditorControlName, StringComparison.Ordinal);
        }

        private void ApplyPendingCaret(TextEditor editor)
        {
            if (!pendingCaret)
            {
                return;
            }

            editor.cursorIndex = Clamp(pendingCursor, 0, source.Length);
            editor.selectIndex = Clamp(pendingSelect, 0, source.Length);
            pendingCaret = false;
        }

        private void RequestCaret(TextEditor editor, int cursor, int select)
        {
            cursor = Clamp(cursor, 0, source.Length);
            select = Clamp(select, 0, source.Length);
            if (editor != null)
            {
                editor.text = source;
                editor.cursorIndex = cursor;
                editor.selectIndex = select;
                pendingCaret = false;
            }
            else
            {
                pendingCursor = cursor;
                pendingSelect = select;
                pendingCaret = true;
            }
        }

        private void CommitSource(string value, int cursor, int select, bool recordUndo = true)
        {
            value = ArtiEditorText.NormalizeLineEndings(value);
            if (recordUndo && !string.Equals(source, value, StringComparison.Ordinal))
            {
                PushUndoSnapshot();
                redoStack.Clear();
            }

            source = value;
            if (boundPart == null)
                AdvancedRimTalk.AdvancedRimTalkMod.Settings.SetPrimaryTakeoverSystemDocument(source);
            else
                boundPart.Content = source;
            lastBoundSource = source;
            analyzedSource = null;
            analysis = null;
            RefreshAnalysis();
            RequestCaret(GetEditor(), cursor, select);
        }

        private void PushUndoSnapshot()
        {
            int limit = GetUndoLimit();
            if (limit <= 0)
            {
                undoStack.Clear();
                return;
            }

            TextEditor editor = GetEditor();
            int cursor = editor == null ? source.Length : Clamp(editor.cursorIndex, 0, source.Length);
            int select = editor == null ? cursor : Clamp(editor.selectIndex, 0, source.Length);
            undoStack.Add(new EditorSnapshot(source, cursor, select));
            while (undoStack.Count > limit)
            {
                undoStack.RemoveAt(0);
            }
        }

        private void Undo()
        {
            if (undoStack.Count == 0)
            {
                return;
            }

            PushRedoSnapshot();
            EditorSnapshot snapshot = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            ApplySnapshot(snapshot);
        }

        private void Redo()
        {
            if (redoStack.Count == 0)
            {
                return;
            }

            PushUndoSnapshot();
            EditorSnapshot snapshot = redoStack[redoStack.Count - 1];
            redoStack.RemoveAt(redoStack.Count - 1);
            ApplySnapshot(snapshot);
        }

        private void PushRedoSnapshot()
        {
            TextEditor editor = GetEditor();
            int cursor = editor == null ? source.Length : Clamp(editor.cursorIndex, 0, source.Length);
            int select = editor == null ? cursor : Clamp(editor.selectIndex, 0, source.Length);
            redoStack.Add(new EditorSnapshot(source, cursor, select));
            int limit = GetUndoLimit();
            while (redoStack.Count > limit && redoStack.Count > 0)
            {
                redoStack.RemoveAt(0);
            }
        }

        private void ApplySnapshot(EditorSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            CommitSource(
                snapshot.Source,
                snapshot.Cursor,
                snapshot.Select,
                false);
            ClearCompletion();
            focusEditor = true;
            RevealCaret(snapshot.Cursor);
        }

        private static int GetUndoLimit()
        {
            AdvancedRimTalkSettings settings = AdvancedRimTalk.AdvancedRimTalkMod.Settings;
            if (settings == null)
            {
                return 100;
            }

            return Clamp(settings.ArtiEditorUndoLimit, 0, 500);
        }

        private string GetCurrentLineText(TextEditor editor)
        {
            int cursor = editor == null ? source.Length : Clamp(editor.cursorIndex, 0, source.Length);
            int lineStart = ArtiEditorText.GetLineStart(source, cursor);
            int lineEnd = ArtiEditorText.GetLineEnd(source, cursor);
            return source.Substring(lineStart, lineEnd - lineStart);
        }

        private void ResetDocument()
        {
            CommitSource(
                AdvancedRimTalkSettings.DefaultTakeoverArtiPromptDocument,
                0,
                0);
            completions.Clear();
            completionStart = -1;
            completionPrefix = string.Empty;
            editorScroll = Vector2.zero;
            diagnosticsScroll = Vector2.zero;
            focusEditor = true;
        }

        private void MoveCaretTo(int position)
        {
            TextEditor editor = GetEditor();
            int cursor = Clamp(position, 0, source.Length);
            RequestCaret(editor, cursor, cursor);
            focusEditor = true;
            RevealCaret(cursor);
        }

        private void RevealCaret(int cursor)
        {
            int line = ArtiEditorText.GetLineIndex(source, cursor);
            float top = LineY(line);
            float bottom = top + lineAdvance;
            if (top < editorScroll.y)
            {
                editorScroll.y = top;
            }
            else if (bottom > editorScroll.y + 80f)
            {
                editorScroll.y = Math.Max(0f, bottom - 80f);
            }
        }

        private void ProcessEditorShortcuts()
        {
            Event current = Event.current;
            if (current == null
                || current.type != EventType.KeyDown
                || !HasEditorFocus())
            {
                return;
            }

            TextEditor editor = GetEditor();
            if (editor == null)
            {
                return;
            }

            editor.text = source;
            ApplyPendingCaret(editor);

            ValidateCompletion(editor);
            bool command = current.control || current.command;
            if (command)
            {
                if (current.keyCode == KeyCode.Z)
                {
                    if (current.shift)
                    {
                        Redo();
                    }
                    else
                    {
                        Undo();
                    }

                    current.Use();
                    return;
                }

                if (current.keyCode == KeyCode.Y && !current.shift)
                {
                    Redo();
                    current.Use();
                    return;
                }

                if (current.keyCode == KeyCode.A && !current.shift)
                {
                    editor.SelectAll();
                    current.Use();
                    return;
                }

                if (current.keyCode == KeyCode.C)
                {
                    if (current.shift)
                    {
                        GUIUtility.systemCopyBuffer = source;
                    }
                    else
                    {
                        GUIUtility.systemCopyBuffer = editor.hasSelection
                            ? editor.SelectedText
                            : GetCurrentLineText(editor);
                    }

                    current.Use();
                    return;
                }

                if (current.keyCode == KeyCode.X)
                {
                    if (editor.hasSelection)
                    {
                        GUIUtility.systemCopyBuffer = editor.SelectedText;
                        editor.DeleteSelection();
                        CommitSource(editor.text, editor.cursorIndex, editor.selectIndex);
                    }

                    current.Use();
                    return;
                }

                if (current.keyCode == KeyCode.V)
                {
                    if (editor.Paste())
                    {
                        CommitSource(editor.text, editor.cursorIndex, editor.selectIndex);
                    }

                    current.Use();
                    return;
                }
            }

            if (completions.Count > 0)
            {
                if (current.keyCode == KeyCode.Escape)
                {
                    ClearCompletion();
                    current.Use();
                    return;
                }

                if (current.keyCode == KeyCode.UpArrow)
                {
                    completionSelected = Math.Max(0, completionSelected - 1);
                    current.Use();
                    return;
                }

                if (current.keyCode == KeyCode.DownArrow)
                {
                    completionSelected = Math.Min(
                        completions.Count - 1,
                        completionSelected + 1);
                    current.Use();
                    return;
                }

                if (!current.shift && !command && !current.alt
                    && (current.keyCode == KeyCode.Return
                        || current.keyCode == KeyCode.KeypadEnter))
                {
                    ApplyCompletion(completionSelected);
                    current.Use();
                    return;
                }
            }

            if (command && current.keyCode == KeyCode.Space)
            {
                ShowCompletion(editor);
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.Tab)
            {
                if (current.shift)
                {
                    UnindentCurrentLine(editor);
                }
                else
                {
                    HandleTab(editor);
                }

                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.Escape && completions.Count > 0)
            {
                ClearCompletion();
                current.Use();
            }
        }

        private void HandleTab(TextEditor editor)
        {
            if (completions.Count > 0)
            {
                ApplyCompletion(completionSelected);
                return;
            }

            if (CanUseEditorIntelligence(editor.cursorIndex))
            {
                int prefixStart;
                string prefix = ArtiEditorIntelligence.GetCompletionPrefix(
                    source,
                    editor.cursorIndex,
                    out prefixStart);
                IList<ArtiCompletionItem> candidates = intelligence.GetCompletionCandidates(
                    source,
                    editor.cursorIndex,
                    analysis,
                    promptAnalysisContext.Names,
                    GetCompletionLimit());
                if (candidates.Count == 1 && (prefix.Length > 0 || IsMemberContext(editor.cursorIndex)))
                {
                    completions.Clear();
                    completions.Add(candidates[0]);
                    completionStart = prefixStart;
                    completionPrefix = prefix;
                    ApplyCompletion(0);
                    return;
                }

                if (candidates.Count > 1 && (prefix.Length > 0 || IsMemberContext(editor.cursorIndex)))
                {
                    SetCompletions(candidates, prefixStart, prefix);
                    return;
                }
            }

            InsertIndent(editor);
        }

        private void ShowCompletion(TextEditor editor)
        {
            if (editor == null || !CanUseEditorIntelligence(editor.cursorIndex))
            {
                ClearCompletion();
                return;
            }

            int prefixStart;
            string prefix = ArtiEditorIntelligence.GetCompletionPrefix(
                source,
                editor.cursorIndex,
                out prefixStart);
            IList<ArtiCompletionItem> candidates = intelligence.GetCompletionCandidates(
                source,
                editor.cursorIndex,
                analysis,
                promptAnalysisContext.Names,
                GetCompletionLimit());
            if (candidates.Count == 0)
            {
                ClearCompletion();
                return;
            }

            SetCompletions(candidates, prefixStart, prefix);
        }

        private void SetCompletions(
            IList<ArtiCompletionItem> values,
            int start,
            string prefix)
        {
            completions.Clear();
            foreach (ArtiCompletionItem item in values)
            {
                if (item != null && !string.Equals(item.Label, prefix, StringComparison.Ordinal))
                {
                    completions.Add(item);
                }
            }

            completionStart = start;
            completionPrefix = prefix ?? string.Empty;
            completionSelected = 0;
        }

        private void ClearCompletion()
        {
            completions.Clear();
            completionStart = -1;
            completionSelected = 0;
            completionPrefix = string.Empty;
        }

        private void ValidateCompletion(TextEditor editor)
        {
            if (completions.Count == 0) return;
            if (editor == null || !HasEditorFocus()
                || !ArtiEditorText.IsCompletionCurrent(source, editor.cursorIndex, editor.selectIndex,
                    completionStart, completionPrefix)) ClearCompletion();
        }

        private void ApplyCompletion(int index)
        {
            if (index < 0 || index >= completions.Count)
            {
                ClearCompletion();
                return;
            }

            TextEditor editor = GetEditor();
            int cursor = editor == null ? source.Length : Clamp(editor.cursorIndex, 0, source.Length);
            if (completionStart < 0
                || completionStart > cursor
                || !string.Equals(
                    source.Substring(completionStart, cursor - completionStart),
                    completionPrefix,
                    StringComparison.Ordinal))
            {
                ClearCompletion();
                return;
            }

            ArtiCompletionItem item = completions[index];
            string completed = source.Substring(0, completionStart)
                + item.Label
                + source.Substring(cursor);
            int newCursor = completionStart + item.Label.Length;
            ClearCompletion();
            CommitSource(completed, newCursor, newCursor);
            focusEditor = true;
        }

        private void InsertIndent(TextEditor editor)
        {
            if (editor == null)
            {
                return;
            }

            editor.ReplaceSelection(ArtiEditorText.IndentUnit);
            CommitSource(editor.text, editor.cursorIndex, editor.selectIndex);
            ClearCompletion();
        }

        private void UnindentCurrentLine(TextEditor editor)
        {
            if (editor == null)
            {
                return;
            }

            int cursor = Clamp(editor.cursorIndex, 0, source.Length);
            int lineStart = ArtiEditorText.GetLineStart(source, cursor);
            int remove = 0;
            if (source.Substring(lineStart).StartsWith(
                ArtiEditorText.IndentUnit,
                StringComparison.Ordinal))
            {
                remove = ArtiEditorText.IndentUnit.Length;
            }
            else if (lineStart < source.Length && source[lineStart] == '\t')
            {
                remove = 1;
            }
            else
            {
                while (remove < 4
                    && lineStart + remove < source.Length
                    && source[lineStart + remove] == ' ')
                {
                    remove++;
                }
            }

            if (remove == 0)
            {
                return;
            }

            string value = source.Remove(lineStart, remove);
            int newCursor = cursor > lineStart
                ? Math.Max(lineStart, cursor - remove)
                : cursor;
            CommitSource(value, newCursor, newCursor);
            ClearCompletion();
        }

        private void HandleTextAreaResult(
            string before,
            string edited,
            int beforeCursor,
            int beforeSelect)
        {
            TextEditor editor = GetEditor();
            if (string.Equals(before, edited, StringComparison.Ordinal))
            {
                return;
            }

            int cursor = editor == null
                ? edited.Length
                : Clamp(editor.cursorIndex, 0, edited.Length);
            int select = editor == null
                ? cursor
                : Clamp(editor.selectIndex, 0, edited.Length);
            string result = edited;
            int resultCursor = cursor;
            int resultSelect = select;
            int insertionIndex = -1;
            char inserted = '\0';
            bool singleInsertion = beforeCursor == beforeSelect
                && ArtiEditorText.TryFindSingleInsertion(
                    before,
                    edited,
                    out insertionIndex,
                    out inserted,
                    beforeCursor);

            if (singleInsertion
                && inserted == '\n'
                && CanUseEditorIntelligence(insertionIndex))
            {
                result = ArtiEditorText.ApplyAutoIndentAfterNewline(edited, insertionIndex);
                int indentStart = insertionIndex + 1;
                int indentEnd = indentStart;
                while (indentEnd < result.Length
                    && (result[indentEnd] == ' ' || result[indentEnd] == '\t'))
                {
                    indentEnd++;
                }

                resultCursor = indentEnd;
                resultSelect = resultCursor;
            }
            else if (singleInsertion
                && beforeCursor == beforeSelect
                && ArtiEditorText.IsClosingPair(inserted)
                && insertionIndex < before.Length
                && before[insertionIndex] == inserted
                && CanEditAt(insertionIndex))
            {
                result = before;
                resultCursor = insertionIndex + 1;
                resultSelect = resultCursor;
            }
            else if (singleInsertion
                && beforeCursor == beforeSelect
                && ArtiEditorText.IsOpeningPair(inserted)
                && CanInsertPairAt(insertionIndex, before))
            {
                char closing = ArtiEditorText.MatchingClose(inserted);
                result = edited.Insert(insertionIndex + 1, closing.ToString());
                resultCursor = insertionIndex + 1;
                resultSelect = resultCursor;
            }
            else
            {
                int deletionIndex;
                char deleted;
                if (beforeCursor == beforeSelect
                    && ArtiEditorText.TryFindSingleDeletion(
                        before,
                        edited,
                        out deletionIndex,
                        out deleted)
                    && deletionIndex + 1 < before.Length
                    && ArtiEditorText.IsOpeningPair(deleted)
                    && ArtiEditorText.IsMatchingPair(
                        deleted,
                        before[deletionIndex + 1])
                    && beforeCursor == deletionIndex + 1
                    && CanEditAt(deletionIndex))
                {
                    result = edited.Remove(deletionIndex, 1);
                    resultCursor = deletionIndex;
                    resultSelect = resultCursor;
                }
            }

            CommitSource(result, resultCursor, resultSelect);
            ClearCompletion();
            TextEditor resultEditor = GetEditor();
            if (singleInsertion
                && ArtiEditorText.IsIdentifierPart(inserted)
                && resultEditor != null
                && CanUseEditorIntelligence(resultEditor.cursorIndex))
            {
                int prefixStart;
                string prefix = ArtiEditorIntelligence.GetCompletionPrefix(
                    source,
                    resultEditor.cursorIndex,
                    out prefixStart);
                if (prefix.Length >= 2)
                {
                    IList<ArtiCompletionItem> candidates = intelligence.GetCompletionCandidates(
                        source,
                        resultEditor.cursorIndex,
                        analysis,
                        promptAnalysisContext.Names,
                        GetCompletionLimit());
                    if (candidates.Count > 0)
                    {
                        SetCompletions(candidates, prefixStart, prefix);
                    }
                }
            }
            else
            {
                ClearCompletion();
            }
        }

        private bool CanInsertPairAt(int position, string value)
        {
            return CanEditAt(position)
                && !ArtiEditorText.IsInsideStringOrComment(
                    value,
                    position,
                    GetCodeRangeStart(position));
        }

        private static int GetCompletionLimit()
        {
            return AdvancedRimTalkMod.Settings == null
                ? 5
                : Math.Max(1, Math.Min(9, AdvancedRimTalkMod.Settings.ArtiEditorCompletionLimit));
        }

        private bool CanEditAt(int position)
        {
            if (analysis == null)
            {
                return false;
            }

            foreach (ArtiCodeRange range in analysis.CodeRanges)
            {
                if (range.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanUseEditorIntelligence(int position)
        {
            return CanEditAt(position)
                && !ArtiEditorText.IsInsideStringOrComment(
                    source,
                    position,
                    GetCodeRangeStart(position));
        }

        private int GetCodeRangeStart(int position)
        {
            if (analysis != null)
            {
                foreach (ArtiCodeRange range in analysis.CodeRanges)
                {
                    if (range.Contains(position))
                    {
                        return range.StartOffset;
                    }
                }
            }

            return 0;
        }

        private bool IsMemberContext(int position)
        {
            return ArtiEditorIntelligence.IsMemberAccessContext(source, position);
        }

        private sealed class EditorSnapshot
        {
            public EditorSnapshot(string source, int cursor, int select)
            {
                Source = source ?? string.Empty;
                Cursor = cursor;
                Select = select;
            }

            public string Source { get; }
            public int Cursor { get; }
            public int Select { get; }
        }
    }
}
