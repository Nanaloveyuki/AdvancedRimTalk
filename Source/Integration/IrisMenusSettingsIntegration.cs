using System;
using System.Reflection;
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
                    return;
                }

                Func<string> title = owner.SettingsCategory;
                Action<Listing_Standard> draw = owner.DrawSettingsPage;
                registerListing.Invoke(null, new object[] { owner, "general", title, draw, null });
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk could not register its settings page with IrisMenus: " + exception);
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
    }
}
