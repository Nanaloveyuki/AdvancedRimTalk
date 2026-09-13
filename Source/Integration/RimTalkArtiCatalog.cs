using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;
using RimTalk.API;
using RimTalk.Prompt;
using Verse;

namespace AdvancedRimTalk.Integration
{
    public static class RimTalkArtiCatalog
    {
        public static ArtiAnalyzer CreateAnalyzer()
        {
            return new ArtiAnalyzer(CreateModuleCatalog(), CreateSymbolCatalog());
        }

        public static IArtiModuleCatalog CreateModuleCatalog()
        {
            return new RimTalkArtiModuleCatalog();
        }

        public static ArtiSymbolCatalog CreateSymbolCatalog()
        {
            ArtiSymbolCatalog catalog = new ArtiSymbolCatalog();
            HashSet<string> customPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (ValueTuple<string, string, string, string> variable in RimTalkPromptAPI.GetRegisteredCustomVariables())
                {
                    string name = variable.Item1 ?? string.Empty;
                    if (name.Length == 0)
                    {
                        continue;
                    }

                    string displayedPath = string.Equals(variable.Item4, "Environment", StringComparison.OrdinalIgnoreCase)
                        ? "map." + name
                        : name;
                    customPaths.Add(displayedPath);

                    string runtimePath = string.Equals(variable.Item4, "Pawn", StringComparison.OrdinalIgnoreCase)
                        ? "pawn." + name
                        : string.Equals(variable.Item4, "Environment", StringComparison.OrdinalIgnoreCase)
                            ? "map." + name
                            : name;
                    catalog.AddPath(runtimePath);
                }
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk could not read RimTalk custom variables: " + exception.Message);
            }

            try
            {
                Dictionary<string, List<ValueTuple<string, string>>> variables =
                    VariableDefinitions.GetScribanVariables();
                foreach (List<ValueTuple<string, string>> category in variables.Values)
                {
                    foreach (ValueTuple<string, string> variable in category)
                    {
                        if (!customPaths.Contains(variable.Item1))
                        {
                            catalog.AddPath(variable.Item1);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk could not read RimTalk variable definitions: " + exception.Message);
            }

            return catalog;
        }
    }

    internal sealed class RimTalkArtiModuleCatalog : IArtiModuleCatalog
    {
        public bool TryGetModule(string packageId, out ArtiModuleInfo module)
        {
            module = null;
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return false;
            }

            try
            {
                ModMetaData installed = ModLister.GetModWithIdentifier(packageId, false);
                if (installed == null)
                {
                    return false;
                }

                ModMetaData active = ModLister.GetActiveModWithIdentifier(packageId, false);
                bool apiAvailable = string.Equals(packageId, "cj.rimtalk", StringComparison.OrdinalIgnoreCase)
                    && typeof(RimTalkPromptAPI).GetMethod("RegisterContextVariable") != null;
                module = new ArtiModuleInfo(
                    packageId,
                    true,
                    active != null,
                    apiAvailable,
                    installed.ModVersion ?? string.Empty);
                return true;
            }
            catch (Exception)
            {
                module = null;
                return false;
            }
        }
    }
}
