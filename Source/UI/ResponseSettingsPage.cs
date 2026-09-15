using UnityEngine;
using Verse;

namespace AdvancedRimTalk.UI
{
    internal sealed class ResponseSettingsPage
    {
        private Vector2 scroll;
        internal void Draw(Rect rect)
        {
            Rect view = new Rect(0f, 0f, rect.width - 24f, 760f);
            Widgets.BeginScrollView(rect, ref scroll, view);
            Listing_Standard l = new Listing_Standard(); l.Begin(view);
            var s = AdvancedRimTalk.AdvancedRimTalkMod.Settings;
            l.CheckboxLabeled("AdvancedRimTalk.Response.Enable".Translate(), ref s.EnableResponseProcessing);
            l.CheckboxLabeled("AdvancedRimTalk.Response.Json".Translate(), ref s.EnableJsonFormatting);
            l.CheckboxLabeled("AdvancedRimTalk.Response.RegexIgnore".Translate(), ref s.IgnoreByRegex);
            l.CheckboxLabeled("AdvancedRimTalk.Response.Whitelist".Translate(), ref s.RegexWhitelist);
            l.Label("AdvancedRimTalk.Response.WhitelistPatterns".Translate());
            s.ResponseWhitelistRegex = l.TextEntry(s.ResponseWhitelistRegex, 5);
            l.CheckboxLabeled("AdvancedRimTalk.Response.Blacklist".Translate(), ref s.RegexBlacklist);
            l.Label("AdvancedRimTalk.Response.BlacklistPatterns".Translate());
            s.ResponseBlacklistRegex = l.TextEntry(s.ResponseBlacklistRegex, 5);
            l.CheckboxLabeled("AdvancedRimTalk.Response.IntervalIgnore".Translate(), ref s.IgnoreByInterval);
            l.Label("AdvancedRimTalk.Response.IntervalSeconds".Translate());
            string interval = s.IgnoreIntervalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            interval = l.TextEntry(interval, 1);
            float.TryParse(interval, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out s.IgnoreIntervalSeconds);
            l.CheckboxLabeled("AdvancedRimTalk.Response.ModelIgnore".Translate(), ref s.IgnoreByModel);
            l.Label("AdvancedRimTalk.Response.ModelIds".Translate());
            s.ResponseModelIds = l.TextEntry(s.ResponseModelIds, 5);
            l.End(); Widgets.EndScrollView();
        }
    }
}
