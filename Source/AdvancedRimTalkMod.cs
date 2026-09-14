using System.Reflection;
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
        private readonly ArtiCodeEditorPage _artiCodeEditorPage = new ArtiCodeEditorPage();
        private readonly TakeoverPromptPartsPage _takeoverPromptPartsPage = new TakeoverPromptPartsPage();
        private string _artiEditorUndoLimitBuffer;

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
            Settings = GetSettings<AdvancedRimTalkSettings>();
            RimTalkExpandMemoryArtiBridge.Detect();
            RimTalkArtiPromptRegistration.Register();
            IrisMenusSettingsIntegration.TryRegister(this);
            new Harmony("advancedrimtalk.prompt").PatchAll(Assembly.GetExecutingAssembly());
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

            if (Settings.ReplaceRimTalkPromptMechanism)
            {
                listing.Label("AdvancedRimTalk.Settings.TakeoverArtiPromptDocument".Translate());
                string primarySystemDocument = Settings.GetPrimaryTakeoverSystemDocument();
                string updatedSystemDocument = listing.TextEntry(primarySystemDocument, 10);
                if (updatedSystemDocument != primarySystemDocument)
                {
                    Settings.SetPrimaryTakeoverSystemDocument(updatedSystemDocument);
                }

                listing.Label("AdvancedRimTalk.Settings.TakeoverArtiPromptDocumentTooltip".Translate());
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

        internal void DrawArtiEditor(Rect inRect)
        {
            _artiCodeEditorPage.Draw(inRect);
        }

        internal void DrawTakeoverPromptParts(Rect inRect)
        {
            _takeoverPromptPartsPage.Draw(inRect);
        }
    }
}
