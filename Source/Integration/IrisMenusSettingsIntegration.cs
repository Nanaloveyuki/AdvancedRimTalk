using System;
using System.Reflection;
using System.Collections.Generic;
using AdvancedRimTalk.UI;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class IrisMenusSettingsIntegration
    {
        private const string PackageId = "Nanaloveyuki.IrisMenus";
        private const string MenuRegistryTypeName = "IrisMenus.MenuRegistry";
        private static bool attempted;

        public static void TryRegister(AdvancedRimTalkMod owner)
        {
            if (attempted || owner == null)
            {
                return;
            }

            attempted = true;
            if (!ModsConfig.IsActive(PackageId))
            {
                return;
            }

            try
            {
                Type registryType = GenTypes.GetTypeInAnyAssembly(MenuRegistryTypeName);
                MethodInfo registerListing = FindRegisterListing(registryType);
                if (registerListing == null)
                {
                    Log.Warning("Advanced RimTalk found IrisMenus, but its RegisterListing API is unavailable.");
                }
                else
                {
                    Func<string> title = owner.SettingsCategory;
                    Action<Listing_Standard> draw = owner.DrawSettingsPage;
                    registerListing.Invoke(null, new object[] { owner, "general", title, draw, null });
                }

                MethodInfo registerSubItem = FindRegisterSubItem(registryType);
                if (registerSubItem == null)
                {
                    Log.Warning("Advanced RimTalk found IrisMenus, but its RegisterSubItem API is unavailable.");
                }
                else
                {
                    Func<string> replTitle = delegate
                    {
                        return "AdvancedRimTalk.ArtiRepl.Title".Translate().ToString();
                    };
                    RegisterSubItem(
                        registerSubItem,
                        owner,
                        "arti-repl",
                        replTitle,
                        owner.DrawArtiRepl);
                    RegisterSubItem(registerSubItem, owner, "arti-log", () => "AdvancedRimTalk.ArtiLog.Title".Translate().ToString(), owner.DrawArtiLog);
                    RegisterSubItem(registerSubItem, owner, "arti-once", () => "AdvancedRimTalk.Once.Title".Translate().ToString(), owner.DrawArtiOnce);

                    Func<string> editorTitle = delegate
                    {
                        return "AdvancedRimTalk.ArtiEditor.Title".Translate().ToString();
                    };
                    RegisterSubItem(
                        registerSubItem,
                        owner,
                        "arti-editor",
                        editorTitle,
                        owner.DrawArtiEditor);

                    Func<string> promptPartsTitle = delegate
                    {
                        return "AdvancedRimTalk.PromptParts.Title".Translate().ToString();
                    };
                    RegisterSubItem(
                        registerSubItem,
                        owner,
                        "takeover-prompt-parts",
                        promptPartsTitle,
                        owner.DrawTakeoverPromptParts);

                    Func<string> documentationTitle = delegate
                    {
                        return "AdvancedRimTalk.Documentation.Title".Translate().ToString();
                    };
                    RegisterSubItem(registerSubItem, owner, "prompt-preview",
                        () => "AdvancedRimTalk.Preview.Title".Translate().ToString(), owner.DrawPromptPreview);
                    RegisterSubItem(registerSubItem, owner, "response-ignore",
                        () => "AdvancedRimTalk.Response.IgnoreLogic".Translate().ToString(), owner.DrawResponseSettings);
                    RegisterSubItem(
                        registerSubItem,
                        owner,
                        "documentation",
                        documentationTitle,
                        owner.DrawDocumentation);
                    typeof(IrisMenusSettingsIntegration).GetMethod(nameof(RegisterDocumentSearch),
                        BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(
                            registryType.Assembly.GetType("IrisMenus.MenuSearchEntry", true))
                        .Invoke(null, new object[] { registryType, owner });
                }
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk could not register its IrisMenus pages: " + exception);
            }
        }

        private static void RegisterDocumentSearch<T>(Type registryType, AdvancedRimTalkMod owner)
        {
            Func<IEnumerable<T>> provider = () => DocumentSearchEntries<T>(owner);
            Action<string> focus = owner.Documentation.Focus;
            MethodInfo register = registryType.GetMethod("RegisterSearchProvider", new[]
                { typeof(Mod), typeof(string), typeof(Func<IEnumerable<T>>), typeof(Action<string>) });
            if (register == null)
            {
                Log.Warning("[Advanced RimTalk] IrisMenus document search API is unavailable.");
                return;
            }
            register.Invoke(null, new object[] { owner, "documentation", provider, focus });
        }

        private static IEnumerable<T> DocumentSearchEntries<T>(AdvancedRimTalkMod owner)
        {
            foreach (var entry in owner.Documentation.Entries)
            {
                Func<string> title = () => entry.Title;
                Func<string> keywords = () => entry.RelativePath;
                Func<string> context = () => "AdvancedRimTalk.Documentation.Title".Translate().ToString();
                yield return (T)Activator.CreateInstance(typeof(T), entry.RelativePath, title, keywords, context);
            }
        }

        private static MethodInfo FindRegisterListing(Type registryType)
        {
            if (registryType == null)
            {
                return null;
            }

            foreach (MethodInfo method in registryType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!string.Equals(method.Name, "RegisterListing", StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 5
                    || parameters[0].ParameterType != typeof(Mod)
                    || parameters[1].ParameterType != typeof(string)
                    || parameters[2].ParameterType != typeof(Func<string>)
                    || parameters[3].ParameterType != typeof(Action<Listing_Standard>)
                    || parameters[4].ParameterType != typeof(Action))
                {
                    continue;
                }

                return method;
            }

            return null;
        }

        private static void RegisterSubItem(
            MethodInfo registerSubItem,
            AdvancedRimTalkMod owner,
            string pageId,
            Func<string> title,
            Action<Rect> draw)
        {
            try
            {
                registerSubItem.Invoke(
                    null,
                    new object[] { owner, pageId, title, draw, null });
            }
            catch (Exception exception)
            {
                Log.Warning(
                    "Advanced RimTalk could not register IrisMenus SubItem '"
                    + pageId
                    + "': "
                    + exception);
            }
        }

        private static MethodInfo FindRegisterSubItem(Type registryType)
        {
            if (registryType == null)
            {
                return null;
            }

            foreach (MethodInfo method in registryType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!string.Equals(method.Name, "RegisterSubItem", StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 5
                    || parameters[0].ParameterType != typeof(Mod)
                    || parameters[1].ParameterType != typeof(string)
                    || parameters[2].ParameterType != typeof(Func<string>)
                    || parameters[3].ParameterType != typeof(Action<Rect>)
                    || parameters[4].ParameterType != typeof(Action))
                {
                    continue;
                }

                return method;
            }

            return null;
        }
    }
}
