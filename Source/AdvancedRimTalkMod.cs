using System.Reflection;
using System;
using AdvancedRimTalk.Integration;
using AdvancedRimTalk.Settings;
using AdvancedRimTalk.UI;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk
{
    public sealed class AdvancedRimTalkMod : Mod
    {
        private readonly ArtiReplPage _artiReplPage = new ArtiReplPage();
        private readonly ArtiLogPage _artiLogPage = new ArtiLogPage();
        private readonly ArtiOncePage _artiOncePage = new ArtiOncePage();
        internal void DrawArtiLog(Rect rect) { _artiLogPage.Draw(rect); }
        internal void DrawArtiOnce(Rect rect) { _artiOncePage.Draw(rect); }
        private readonly PromptPreviewPage _promptPreviewPage = new PromptPreviewPage();
        private readonly ArtiCodeEditorPage _artiCodeEditorPage = new ArtiCodeEditorPage();
        private readonly TakeoverPromptPartsPage _takeoverPromptPartsPage = new TakeoverPromptPartsPage();
        private readonly DocumentationPage _documentationPage;
        private readonly ResponseSettingsPage _responseSettingsPage = new ResponseSettingsPage();
        private string _artiEditorUndoLimitBuffer;
        private string _artiEditorCompletionLimitBuffer;
        private string _takeoverPawnLimitBuffer;
        private string _takeoverHistoryLimitBuffer;
        private string _takeoverBudgetBuffer;
        private string _initializationError;

        internal static AdvancedRimTalkSettings Settings { get; private set; }

        internal static bool IsPlaceholderLayerEnabled
        {
            get { return Settings == null || Settings.EnablePlaceholderLayer; }
        }

        internal static bool ShouldReplaceRimTalkPromptMechanism
        {
            get { return Settings != null && Settings.ReplaceRimTalkPromptMechanism; }
        }

        internal static bool IsArtiPromptEmbeddingEnabled
        {
            get { return !ShouldReplaceRimTalkPromptMechanism; }
        }

        public AdvancedRimTalkMod(ModContentPack content)
            : base(content)
        {
            _documentationPage = new DocumentationPage(content.RootDir);
            Settings = GetSettings<AdvancedRimTalkSettings>();
            var harmony = new Harmony("advancedrimtalk.prompt");
            try
            {
                RimTalkCompatibility.Validate();
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception exception)
            {
                _initializationError = exception.GetBaseException().Message;
                Log.Error("Advanced RimTalk integration disabled: " + exception);
                // Roll back only our patches, including a partially completed PatchAll.
                harmony.UnpatchAll(harmony.Id);
                return;
            }
            RimTalkExpandMemoryArtiBridge.Detect();
            ArtiOnceBinding.Initialize();
            RimTalkArtiPromptRegistration.Register();
            IrisMenusSettingsIntegration.TryRegister(this);
            Log.Message("Advanced RimTalk Prompt initialized.");
        }

        public override string SettingsCategory()
        {
            return "Advanced RimTalk";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            try
            {
                DrawSettingsPage(listing);
            }
            finally
            {
                listing.End();
            }
        }

        internal void DrawSettingsPage(Listing_Standard listing)
        {
            if (_initializationError != null)
            {
                listing.Label("AdvancedRimTalk.Settings.Incompatible".Translate(_initializationError));
                return;
            }
            listing.Label("AdvancedRimTalk.Settings.PromptIntegrationMode".Translate());
            if (listing.RadioButton(
                "AdvancedRimTalk.Settings.EmbedArtiIntoRimTalkPrompt".Translate(),
                !Settings.ReplaceRimTalkPromptMechanism,
                12f,
                "AdvancedRimTalk.Settings.EmbedArtiIntoRimTalkPromptTooltip".Translate(),
                null))
            {
                Settings.ReplaceRimTalkPromptMechanism = false;
            }

            if (listing.RadioButton(
                "AdvancedRimTalk.Settings.TakeoverRimTalkPrompt".Translate(),
                Settings.ReplaceRimTalkPromptMechanism,
                12f,
                "AdvancedRimTalk.Settings.TakeoverRimTalkPromptTooltip".Translate(),
                null))
            {
                Settings.ReplaceRimTalkPromptMechanism = true;
            }

            listing.Gap();
            listing.CheckboxLabeled(
                "AdvancedRimTalk.Settings.EnablePlaceholderLayer".Translate(),
                ref Settings.EnablePlaceholderLayer,
                "AdvancedRimTalk.Settings.EnablePlaceholderLayerTooltip".Translate());

            listing.Gap();
            if (_artiEditorUndoLimitBuffer == null)
            {
                _artiEditorUndoLimitBuffer = Settings.ArtiEditorUndoLimit.ToString();
            }

            DrawIntegerSetting(
                listing,
                "AdvancedRimTalk.Settings.ArtiEditorUndoLimit".Translate(),
                ref Settings.ArtiEditorUndoLimit,
                ref _artiEditorUndoLimitBuffer,
                0,
                500);

            DrawIntegerSetting(
                listing,
                "AdvancedRimTalk.Settings.ArtiEditorCompletionLimit".Translate(),
                ref Settings.ArtiEditorCompletionLimit,
                ref _artiEditorCompletionLimitBuffer,
                1,
                9);

            DrawIntegerSetting(listing, "AdvancedRimTalk.Settings.PawnLimit".Translate(), ref Settings.TakeoverMaxPawnContextCount, ref _takeoverPawnLimitBuffer, 1, 256);
            DrawIntegerSetting(listing, "AdvancedRimTalk.Settings.HistoryLimit".Translate(), ref Settings.TakeoverConversationHistoryCount, ref _takeoverHistoryLimitBuffer, 0, 500);
            DrawIntegerSetting(listing, "AdvancedRimTalk.Settings.CharacterBudget".Translate(), ref Settings.TakeoverPromptCharacterBudget, ref _takeoverBudgetBuffer, 4000, 200000);

            listing.Gap();
            if (listing.ButtonText("AdvancedRimTalk.Once.Open".Translate()))
            {
                Find.WindowStack.Add(new ArtiOnceWindow());
            }
        }

        private static void DrawIntegerSetting(
            Listing_Standard listing,
            string label,
            ref int value,
            ref string buffer,
            int minimum,
            int maximum)
        {
            if (buffer == null) buffer = value.ToString();
            float labelWidth = listing.ColumnWidth * 0.4f;
            float height = Mathf.Max(30f, Text.CalcHeight(label, labelWidth));
            Rect row = listing.GetRect(height);
            Widgets.Label(
                new Rect(row.x, row.y, labelWidth, row.height),
                label);
            listing.Gap(4f);
            Widgets.TextFieldNumeric(
                new Rect(
                    row.x + labelWidth + 8f,
                    row.y,
                    Mathf.Max(1f, row.width - labelWidth - 8f),
                    30f),
                ref value,
                ref buffer,
                minimum,
                maximum);
        }

        internal void DrawArtiRepl(Rect inRect)
        {
            _artiReplPage.Draw(inRect);
        }

        internal void DrawPromptPreview(Rect inRect)
        {
            _promptPreviewPage.Draw(inRect);
        }

        internal void DrawArtiEditor(Rect inRect)
        {
            _artiCodeEditorPage.Draw(inRect);
        }

        internal void DrawTakeoverPromptParts(Rect inRect)
        {
            _takeoverPromptPartsPage.Draw(inRect);
        }

        internal void DrawDocumentation(Rect inRect)
        {
            _documentationPage.Draw(inRect);
        }

        internal DocumentationPage Documentation => _documentationPage;

        internal void DrawResponseSettings(Rect inRect)
        {
            _responseSettingsPage.Draw(inRect);
        }
    }
}
