using System;
using System.Collections.Generic;
using RimTalk.API;
using RimTalk.Prompt;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkArtiPawnPromptData
    {
        private const int NearbyDistance = 3;
        private const int NearbyMaxPerKind = 12;
        private const int NearbyMaxCells = 18;
        private const int NearbyMaxThings = 200;
        private const int NearbyMaxItems = 120;

        private enum NearbyThingKind
        {
            Any,
            Building,
            Item,
            Plant,
            Animal,
            Filth
        }

        public static string GetLocation(Pawn pawn)
        {
            return ApplyPawnHook(
                pawn,
                ContextCategories.Pawn.Location,
                PromptContextProvider.GetLocationString(pawn));
        }

        public static string GetBeauty(Pawn pawn)
        {
            return ApplyPawnHook(
                pawn,
                ContextCategories.Pawn.Beauty,
                PromptContextProvider.GetBeautyString(pawn));
        }

        public static string GetCleanliness(Pawn pawn)
        {
            return ApplyPawnHook(
                pawn,
                ContextCategories.Pawn.Cleanliness,
                PromptContextProvider.GetCleanlinessString(pawn));
        }

        public static string GetTerrain(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
            {
                return string.Empty;
            }

            TerrainDef terrain = GridsUtility.GetTerrain(pawn.Position, pawn.Map);
            return ApplyPawnHook(
                pawn,
                ContextCategories.Pawn.Terrain,
                terrain == null ? string.Empty : terrain.LabelCap.RawText);
        }

        public static string GetNearbyThingsText(Pawn pawn)
        {
            return ApplyPawnHook(
                pawn,
                ContextCategories.Pawn.Surroundings,
                ContextHelper.CollectNearbyContextText(
                    pawn,
                    NearbyDistance,
                    NearbyMaxPerKind,
                    NearbyMaxCells,
                    NearbyMaxThings,
                    NearbyMaxItems));
        }

        public static List<Thing> GetNearbyThings(Pawn pawn)
        {
            return CollectNearbyThings(pawn, NearbyThingKind.Any);
        }

        public static List<Thing> GetNearbyItems(Pawn pawn)
        {
            return CollectNearbyThings(pawn, NearbyThingKind.Item);
        }

        public static List<Thing> GetNearbyBuildings(Pawn pawn)
        {
            return CollectNearbyThings(pawn, NearbyThingKind.Building);
        }

        public static List<Thing> GetNearbyPlants(Pawn pawn)
        {
            return CollectNearbyThings(pawn, NearbyThingKind.Plant);
        }

        public static List<Thing> GetNearbyAnimals(Pawn pawn)
        {
            return CollectNearbyThings(pawn, NearbyThingKind.Animal);
        }

        public static List<Thing> GetNearbyFilth(Pawn pawn)
        {
            return CollectNearbyThings(pawn, NearbyThingKind.Filth);
        }

        private static List<Thing> CollectNearbyThings(Pawn pawn, NearbyThingKind filter)
        {
            List<Thing> result = new List<Thing>();
            if (pawn == null || pawn.Map == null || pawn.Position == IntVec3.Invalid)
            {
                return result;
            }

            Map map = pawn.Map;
            Room room = RegionAndRoomQuery.GetRoom(pawn, RegionType.Set_All);
            bool sameRoomOnly = room != null && !room.PsychologicallyOutdoors;
            HashSet<int> seenBuildingIds = new HashSet<int>();
            int cellsScanned = 0;
            int processedTotal = 0;
            int processedItems = 0;
            int buildingCount = 0;
            int itemCount = 0;
            int plantCount = 0;
            int animalCount = 0;
            int filthCount = 0;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, NearbyDistance, true))
            {
                if (!cell.InBounds(map) || (sameRoomOnly && GridsUtility.GetRoom(cell, map) != room))
                {
                    continue;
                }

                if (cellsScanned++ >= NearbyMaxCells)
                {
                    break;
                }

                List<Thing> thingsHere = GridsUtility.GetThingList(cell, map);
                if (thingsHere == null)
                {
                    continue;
                }

                for (int index = 0; index < thingsHere.Count; index++)
                {
                    if (processedTotal >= NearbyMaxThings)
                    {
                        return result;
                    }

                    Thing thing = thingsHere[index];
                    if (thing == null || thing.def == null || thing.DestroyedOrNull()
                        || ContextHelper.IsHiddenForPlayer(thing))
                    {
                        continue;
                    }

                    if (thing.def.category == ThingCategory.Item)
                    {
                        if (StoreUtility.GetSlotGroup(thing.Position, map) != null)
                        {
                            continue;
                        }

                        processedItems++;
                        if (processedItems > NearbyMaxItems)
                        {
                            return result;
                        }

                        if (thing.stackCount >= 1000 && thing.def.stackLimit < 1000)
                        {
                            continue;
                        }
                    }

                    processedTotal++;

                    NearbyThingKind kind;
                    if (thing is Pawn)
                    {
                        Pawn otherPawn = (Pawn)thing;
                        if (otherPawn == pawn || !otherPawn.Spawned || otherPawn.Dead
                            || otherPawn.RaceProps == null || !otherPawn.RaceProps.Animal)
                        {
                            continue;
                        }

                        kind = NearbyThingKind.Animal;
                    }
                    else if (thing.def.category == ThingCategory.Building)
                    {
                        if (!seenBuildingIds.Add(thing.thingIDNumber) || ContextHelper.IsWall(thing))
                        {
                            continue;
                        }

                        kind = NearbyThingKind.Building;
                    }
                    else if (thing.def.category == ThingCategory.Item)
                    {
                        kind = NearbyThingKind.Item;
                    }
                    else if (thing.def.category == ThingCategory.Plant)
                    {
                        kind = NearbyThingKind.Plant;
                    }
                    else if (thing.def.IsFilth)
                    {
                        kind = NearbyThingKind.Filth;
                    }
                    else
                    {
                        continue;
                    }

                    if (filter != NearbyThingKind.Any && filter != kind)
                    {
                        continue;
                    }

                    int kindCount;
                    switch (kind)
                    {
                        case NearbyThingKind.Building:
                            kindCount = buildingCount++;
                            break;
                        case NearbyThingKind.Item:
                            kindCount = itemCount++;
                            break;
                        case NearbyThingKind.Plant:
                            kindCount = plantCount++;
                            break;
                        case NearbyThingKind.Animal:
                            kindCount = animalCount++;
                            break;
                        default:
                            kindCount = filthCount++;
                            break;
                    }

                    if (kindCount < NearbyMaxPerKind)
                    {
                        result.Add(thing);
                    }
                }
            }

            return result;
        }

        private static string ApplyPawnHook(Pawn pawn, ContextCategory category, string value)
        {
            string normalized = value ?? string.Empty;
            return pawn == null
                ? normalized
                : ContextHookRegistry.ApplyPawnHooks(category, pawn, normalized) ?? string.Empty;
        }
    }

    internal static class RimTalkArtiPromptRegistration
    {
        public static void Register()
        {
            try
            {
                RimTalkPromptAPI.RegisterPawnVariable(
                    "advancedrimtalk.prompt",
                    "nearby_things",
                    RimTalkArtiPawnPromptData.GetNearbyThingsText,
                    "Nearby things from RimTalk's surroundings context");
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk could not register RimTalk pawn prompts: " + exception.Message);
            }
        }
    }
}
