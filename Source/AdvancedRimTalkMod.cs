using System.Reflection;
using AdvancedRimTalk.Integration;
using AdvancedRimTalk.Settings;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk
{
    public sealed class AdvancedRimTalkMod : Mod
    {
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

            if (Settings.ReplaceRimTalkPromptMechanism)
            {
                listing.Label("AdvancedRimTalk.Settings.TakeoverArtiPromptDocument".Translate());
                Settings.TakeoverArtiPromptDocument = listing.TextEntry(
                    Settings.TakeoverArtiPromptDocument,
                    10);
                listing.Label("AdvancedRimTalk.Settings.TakeoverArtiPromptDocumentTooltip".Translate());
            }
        }
    }
}
