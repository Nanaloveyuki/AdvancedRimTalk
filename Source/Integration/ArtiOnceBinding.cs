using System;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Settings;
using RimWorld.Planet;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class ArtiOnceBinding
    {
        internal static void Initialize()
        {
            ArtiOnceStores.Current = new ArtiOnceSettingsStore();
            ArtiOnceStores.ScopeKey = CurrentScopeKey;
            ArtiOnceStores.ScopeLabel = CurrentScopeLabel;
        }

        internal static void Apply(ArtiExecutionContext context, bool preview, string owner)
        {
            if (context == null)
            {
                return;
            }

            context.OnceStore = ArtiOnceStores.Current;
            context.OncePreview = preview;
            context.OnceOwner = owner ?? string.Empty;
            context.OnceScope = ArtiOnceStores.CurrentScopeKey();
            context.OnceScopeLabel = ArtiOnceStores.CurrentScopeLabel();
        }

        private static string CurrentScopeKey()
        {
            WorldInfo info = TryWorldInfo();
            if (info == null)
            {
                return "global";
            }

            return "world:" + (info.seedString ?? string.Empty) + ":" + (info.name ?? string.Empty);
        }

        private static string CurrentScopeLabel()
        {
            WorldInfo info = TryWorldInfo();
            return info == null ? "global" : (string.IsNullOrWhiteSpace(info.name) ? info.seedString : info.name);
        }

        private static WorldInfo TryWorldInfo()
        {
            try
            {
                if (Current.Game == null || Find.World == null)
                {
                    return null;
                }

                return Find.World.info;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
