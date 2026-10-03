using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimTalk.Prompt;
using RimTalk.Service;
using RimTalk.Util;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkRaceCompatibility
    {
        // HAR remains optional; no AlienRace assembly reference is required.
        private static readonly Type AlienRaceDefType = AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");

        internal static bool IsAlienRace(Pawn pawn)
        {
            ThingDef definition = pawn?.def;
            // HAR also converts vanilla Human and CreepJoiner definitions to ThingDef_AlienRace.
            return definition != null && definition.race != null && definition.race.Humanlike
                && definition.defName != "Human" && definition.defName != "CreepJoiner"
                && AlienRaceDefType != null && AlienRaceDefType.IsInstanceOfType(definition);
        }

        internal static string GetLabel(Pawn pawn)
        {
            if (pawn == null) return string.Empty;
            if (!IsAlienRace(pawn) && ModsConfig.BiotechActive
                && pawn.genes != null && pawn.genes.Xenotype != null)
                return pawn.genes.XenotypeLabel ?? string.Empty;

            return GetDefLabel(pawn.def);
        }

        internal static string GetDefLabel(ThingDef definition)
        {
            if (definition == null) return string.Empty;
            return string.IsNullOrEmpty(definition.label)
                ? definition.defName ?? string.Empty
                : definition.LabelCap.RawText ?? definition.label;
        }

        internal static string GetGeneRaceLabel(RimWorld.Pawn_GeneTracker genes)
        {
            return IsAlienRace(genes.pawn) ? GetDefLabel(genes.pawn.def) : genes.XenotypeLabel;
        }
    }

    [HarmonyPatch(typeof(ContextBuilder), nameof(ContextBuilder.GetRaceContext))]
    internal static class RimTalkAlienRaceContextPatch
    {
        private static bool Prefix(Pawn pawn, ref string __result)
        {
            if (!RimTalkRaceCompatibility.IsAlienRace(pawn)) return true;
            __result = RimTalk.Settings.Get().Context.IncludeRace
                ? "Race: " + RimTalkRaceCompatibility.GetDefLabel(pawn.def) : null;
            return false;
        }
    }

    [HarmonyPatch(typeof(ScribanParser), "GetMagicPawnValue")]
    internal static class RimTalkAlienRaceScribanPatch
    {
        private static bool Prefix(Pawn pawn, string member, ref string __result)
        {
            if (!string.Equals(member, "race", StringComparison.OrdinalIgnoreCase)
                || !RimTalkRaceCompatibility.IsAlienRace(pawn)) return true;
            __result = RimTalkRaceCompatibility.GetDefLabel(pawn.def);
            return false;
        }
    }

    // Replace only the race label read, preserving RimTalk's name, role, age and threat formatting.
    [HarmonyPatch]
    internal static class RimTalkAlienRaceNamePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ContextHelper), nameof(ContextHelper.GetDecoratedName));
            yield return AccessTools.Method(typeof(PawnUtil), "GetThreatLabel");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo original = AccessTools.PropertyGetter(typeof(RimWorld.Pawn_GeneTracker), "XenotypeLabel");
            MethodInfo replacement = AccessTools.Method(typeof(RimTalkRaceCompatibility),
                nameof(RimTalkRaceCompatibility.GetGeneRaceLabel));
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                }
                yield return instruction;
            }
        }
    }
}
