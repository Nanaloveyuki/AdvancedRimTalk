using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using AdvancedRimTalk.Arti;
using RimWorld;
using RimWorld.Planet;
using RimTalk.Prompt;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkArtiNames
    {
        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int length = 0;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character == '_' || character == '-' || char.IsWhiteSpace(character))
                {
                    continue;
                }

                buffer[length++] = char.ToLowerInvariant(character);
            }

            return new string(buffer, 0, length);
        }
    }

    internal class RimTalkArtiLazyNamespace
    {
        private sealed class Entry
        {
            public Entry(Func<object> factory, bool cache)
            {
                Factory = factory;
                Cache = cache;
            }

            public readonly Func<object> Factory;
            public readonly bool Cache;
            public bool HasValue;
            public object Value;
        }

        private readonly Dictionary<string, Entry> _entries =
            new Dictionary<string, Entry>(StringComparer.Ordinal);

        public void Set(string name, Func<object> factory, bool cache = true)
        {
            if (string.IsNullOrEmpty(name) || factory == null)
            {
                return;
            }

            _entries[RimTalkArtiNames.Normalize(name)] = new Entry(factory, cache);
        }

        public void Set(string name, object value)
        {
            Set(name, () => value);
        }

        public bool TryGetMember(string member, out object value)
        {
            Entry entry;
            if (!_entries.TryGetValue(RimTalkArtiNames.Normalize(member), out entry))
            {
                value = null;
                return false;
            }

            if (!entry.Cache || !entry.HasValue)
            {
                entry.Value = entry.Factory();
                entry.HasValue = true;
            }

            value = entry.Value;
            return true;
        }

        public List<string> GetMemberNames()
        {
            List<string> names = new List<string>(_entries.Keys);
            names.Sort(StringComparer.Ordinal);
            return names;
        }
    }

    internal sealed class RimTalkArtiCallable : IArtiCallable
    {
        private readonly Func<IList<object>, IDictionary<string, object>, object> _callback;

        public RimTalkArtiCallable(Func<IList<object>, IDictionary<string, object>, object> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public object Invoke(IList<object> positionalArguments, IDictionary<string, object> namedArguments)
        {
            return _callback(positionalArguments, namedArguments);
        }
    }

    internal sealed class RimTalkArtiCallableNamespace : RimTalkArtiLazyNamespace, IArtiCallable
    {
        private readonly Func<IList<object>, IDictionary<string, object>, object> _callback;

        public RimTalkArtiCallableNamespace(
            Func<IList<object>, IDictionary<string, object>, object> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public object Invoke(IList<object> positionalArguments, IDictionary<string, object> namedArguments)
        {
            return _callback(positionalArguments, namedArguments);
        }
    }

    internal sealed class RimTalkArtiReferenceComparer : IEqualityComparer<object>
    {
        public new bool Equals(object left, object right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(object value)
        {
            return value == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }
    }

    internal static class RimTalkArtiPublicValueReader
    {
        private sealed class MemberLookup
        {
            public readonly PropertyInfo Property;
            public readonly FieldInfo Field;

            public MemberLookup(PropertyInfo property)
            {
                Property = property;
            }

            public MemberLookup(FieldInfo field)
            {
                Field = field;
            }
        }

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, MemberLookup> MemberCache =
            new Dictionary<string, MemberLookup>(StringComparer.Ordinal);
        private static readonly Dictionary<string, MemberLookup> StaticMemberCache =
            new Dictionary<string, MemberLookup>(StringComparer.Ordinal);

        public static bool TryRead(object target, string member, out object value)
        {
            return TryReadInternal(target, target == null ? null : target.GetType(), member, false, out value);
        }

        public static bool TryReadStatic(Type targetType, string member, out object value)
        {
            return TryReadInternal(null, targetType, member, true, out value);
        }

        public static bool TryReadIndex(object target, int index, out object value)
        {
            value = null;
            if (target == null || index < 0 || IsBlockedValue(target))
            {
                return false;
            }

            IList list = target as IList;
            if (list != null)
            {
                if (index >= list.Count)
                {
                    return false;
                }

                value = list[index];
                return true;
            }

            Array array = target as Array;
            if (array != null && array.Rank == 1 && index < array.Length)
            {
                value = array.GetValue(index);
                return true;
            }

            Type type = target.GetType();
            PropertyInfo itemProperty = type.GetProperty(
                "Item",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                null,
                new[] { typeof(int) },
                null);
            if (itemProperty != null && itemProperty.CanRead)
            {
                try
                {
                    value = itemProperty.GetValue(target, new object[] { index });
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            IEnumerable enumerable = target as IEnumerable;
            if (enumerable == null || target is string)
            {
                return false;
            }

            int current = 0;
            try
            {
                foreach (object item in enumerable)
                {
                    if (current++ == index)
                    {
                        value = item;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        public static List<string> GetPublicMemberNames(object target)
        {
            return GetPublicMemberNames(target == null ? null : target.GetType(), false);
        }

        public static List<string> GetPublicStaticMemberNames(Type targetType)
        {
            return GetPublicMemberNames(targetType, true);
        }

        private static bool TryReadInternal(
            object target,
            Type targetType,
            string member,
            bool isStatic,
            out object value)
        {
            value = null;
            if (targetType == null || string.IsNullOrEmpty(member) || IsBlockedValue(target))
            {
                return false;
            }

            string normalized = RimTalkArtiNames.Normalize(member);
            if (normalized.Length == 0)
            {
                return false;
            }

            MemberLookup lookup = GetLookup(targetType, normalized, isStatic);
            if (lookup == null)
            {
                return false;
            }

            try
            {
                if (lookup.Property != null)
                {
                    value = lookup.Property.GetValue(target, null);
                }
                else
                {
                    value = lookup.Field.GetValue(target);
                }

                return !IsBlockedValue(value);
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        private static MemberLookup GetLookup(Type type, string normalized, bool isStatic)
        {
            Dictionary<string, MemberLookup> cache = isStatic ? StaticMemberCache : MemberCache;
            string cacheKey = type.AssemblyQualifiedName + "|" + normalized;
            lock (SyncRoot)
            {
                MemberLookup cached;
                if (cache.TryGetValue(cacheKey, out cached))
                {
                    return cached;
                }
            }

            BindingFlags flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            MemberLookup lookup = null;
            try
            {
                foreach (PropertyInfo property in type.GetProperties(flags))
                {
                    if (!property.CanRead
                        || property.GetIndexParameters().Length != 0
                        || RimTalkArtiNames.Normalize(property.Name) != normalized)
                    {
                        continue;
                    }

                    lookup = new MemberLookup(property);
                    break;
                }

                if (lookup == null)
                {
                    foreach (FieldInfo field in type.GetFields(flags))
                    {
                        if (RimTalkArtiNames.Normalize(field.Name) != normalized)
                        {
                            continue;
                        }

                        lookup = new MemberLookup(field);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                lookup = null;
            }

            lock (SyncRoot)
            {
                cache[cacheKey] = lookup;
            }

            return lookup;
        }

        private static List<string> GetPublicMemberNames(Type targetType, bool isStatic)
        {
            List<string> result = new List<string>();
            if (targetType == null)
            {
                return result;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            BindingFlags flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            try
            {
                foreach (PropertyInfo property in targetType.GetProperties(flags))
                {
                    if (property.CanRead
                        && property.GetIndexParameters().Length == 0
                        && seen.Add(property.Name))
                    {
                        result.Add(property.Name);
                    }
                }

                foreach (FieldInfo field in targetType.GetFields(flags))
                {
                    if (seen.Add(field.Name))
                    {
                        result.Add(field.Name);
                    }
                }
            }
            catch (Exception)
            {
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static bool IsBlockedValue(object value)
        {
            if (value == null)
            {
                return false;
            }

            return value is Type
                || value is MemberInfo
                || value is Assembly
                || value is MethodBase
                || value is Delegate
                || value is Stream
                || value is FileSystemInfo
                || value is UnityEngine.Object;
        }
    }

    internal sealed class RimTalkArtiCachedMember
    {
        public RimTalkArtiCachedMember(bool found, object value)
        {
            Found = found;
            Value = value;
        }

        public readonly bool Found;
        public readonly object Value;
    }

    internal static class RimTalkArtiDefDatabase
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, Type> TypeCache =
            new Dictionary<string, Type>(StringComparer.Ordinal);
        private static readonly Dictionary<string, List<Def>> DefCache =
            new Dictionary<string, List<Def>>(StringComparer.Ordinal);

        public static List<Def> All(string typeName)
        {
            string key = NormalizeTypeName(typeName);
            lock (SyncRoot)
            {
                List<Def> cached;
                if (DefCache.TryGetValue(key, out cached))
                {
                    return cached;
                }
            }

            if (key == "all" || key == "alldefs" || key == "alldefinitions")
            {
                return AllConcreteDefs();
            }

            Type defType = ResolveDefType(typeName, key);
            List<Def> result = ReadDatabase(defType);

            lock (SyncRoot)
            {
                DefCache[key] = result;
            }

            return result;
        }

        private static List<Def> AllConcreteDefs()
        {
            List<Def> result = new List<Def>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type == typeof(Def) || type.IsAbstract || !typeof(Def).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    foreach (Def def in ReadDatabase(type))
                    {
                        if (def == null)
                        {
                            continue;
                        }

                        string identity = type.FullName + "|" + def.defName;
                        if (seen.Add(identity))
                        {
                            result.Add(def);
                        }
                    }
                }
            }

            lock (SyncRoot)
            {
                DefCache["all"] = result;
            }

            return result;
        }

        private static List<Def> ReadDatabase(Type defType)
        {
            List<Def> result = new List<Def>();
            if (defType == null)
            {
                return result;
            }

            try
            {
                Type databaseType = typeof(DefDatabase<>).MakeGenericType(defType);
                PropertyInfo property = databaseType.GetProperty(
                    "AllDefsListForReading",
                    BindingFlags.Public | BindingFlags.Static);
                IEnumerable values = property == null ? null : property.GetValue(null, null) as IEnumerable;
                if (values != null)
                {
                    foreach (object value in values)
                    {
                        Def def = value as Def;
                        if (def != null)
                        {
                            result.Add(def);
                        }
                    }
                }
            }
            catch (Exception)
            {
                result.Clear();
            }

            return result;
        }

        public static Def Find(string typeName, object name)
        {
            string requested = Convert.ToString(name, CultureInfo.InvariantCulture) ?? string.Empty;
            if (requested.Length == 0)
            {
                return null;
            }

            foreach (Def def in All(typeName))
            {
                if (string.Equals(def.defName, requested, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(def.label, requested, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(def.LabelCap.RawText, requested, StringComparison.OrdinalIgnoreCase))
                {
                    return def;
                }
            }

            return null;
        }

        private static Type ResolveDefType(string originalTypeName, string typeName)
        {
            string normalized = NormalizeTypeName(typeName);
            if (normalized.Length == 0)
            {
                return null;
            }

            lock (SyncRoot)
            {
                Type cached;
                if (TypeCache.TryGetValue(normalized, out cached))
                {
                    return cached;
                }
            }

            string originalName = (originalTypeName ?? string.Empty).Trim();
            string canonicalName = CanonicalTypeName(normalized);
            Type result = null;
            try
            {
                if (originalName.Length > 0)
                {
                    result = GenTypes.GetTypeInAnyAssembly(originalName);
                }

                if (result == null)
                {
                    result = GenTypes.GetTypeInAnyAssembly(canonicalName);
                }
            }
            catch (Exception)
            {
            }

            if (result == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type candidate in assembly.GetTypes())
                        {
                            if (candidate.Name.Equals(canonicalName, StringComparison.OrdinalIgnoreCase)
                                || (candidate.FullName != null
                                    && candidate.FullName.Equals(canonicalName, StringComparison.OrdinalIgnoreCase)))
                            {
                                result = candidate;
                                break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                    if (result != null)
                    {
                        break;
                    }
                }
            }

            if (result == null || !typeof(Def).IsAssignableFrom(result) || result.IsAbstract)
            {
                result = null;
            }

            lock (SyncRoot)
            {
                TypeCache[normalized] = result;
            }

            return result;
        }

        private static string NormalizeTypeName(string typeName)
        {
            return RimTalkArtiNames.Normalize(typeName);
        }

        private static string CanonicalTypeName(string normalized)
        {
            switch (normalized)
            {
                case "thing":
                case "things":
                case "thingdef":
                case "thingdefs":
                    return "ThingDef";
                case "pawn":
                case "pawns":
                case "pawnkind":
                case "pawnkinds":
                case "pawnkinddef":
                    return "PawnKindDef";
                case "faction":
                case "factions":
                case "factiondef":
                    return "FactionDef";
                case "biome":
                case "biomes":
                case "biomedef":
                    return "BiomeDef";
                case "terrain":
                case "terrains":
                case "terraindef":
                    return "TerrainDef";
                case "hediff":
                case "hediffs":
                case "hediffdef":
                    return "HediffDef";
                case "trait":
                case "traits":
                case "traitdef":
                    return "TraitDef";
                case "skill":
                case "skills":
                case "skilldef":
                    return "SkillDef";
                case "work":
                case "worktype":
                case "worktypes":
                case "worktypedef":
                    return "WorkTypeDef";
                case "research":
                case "researchproject":
                case "researchprojects":
                case "researchprojectdef":
                    return "ResearchProjectDef";
                case "incident":
                case "incidents":
                case "incidentdef":
                    return "IncidentDef";
                case "recipe":
                case "recipes":
                case "recipedef":
                    return "RecipeDef";
                case "job":
                case "jobs":
                case "jobdef":
                    return "JobDef";
                case "interaction":
                case "interactions":
                case "interactiondef":
                    return "InteractionDef";
                case "thought":
                case "thoughts":
                case "thoughtdef":
                    return "ThoughtDef";
                case "gene":
                case "genes":
                case "genedef":
                    return "GeneDef";
                case "ability":
                case "abilities":
                case "abilitydef":
                    return "AbilityDef";
                case "worldobject":
                case "worldobjects":
                case "worldobjectdef":
                    return "WorldObjectDef";
                case "mapgenerator":
                case "mapgeneratordef":
                    return "MapGeneratorDef";
                case "gamecondition":
                case "gameconditiondef":
                    return "GameConditionDef";
                case "thingcategory":
                case "thingcategorydef":
                    return "ThingCategoryDef";
                case "royaltitle":
                case "royaltitledef":
                    return "RoyalTitleDef";
                case "ideology":
                case "ideo":
                case "ideodef":
                    return "IdeoDef";
                default:
                    return typeNameToPascal(normalized);
            }
        }

        private static string typeNameToPascal(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }

    internal sealed class RimTalkArtiCoreModule
    {
        private readonly PromptContext _context;
        private readonly RimTalkArtiLazyNamespace _root = new RimTalkArtiLazyNamespace();
        private readonly Lazy<List<Map>> _maps;
        private readonly Lazy<List<Pawn>> _allPawns;
        private readonly Lazy<List<Faction>> _factions;
        private readonly Lazy<List<WorldObject>> _worldObjects;
        private readonly Lazy<List<ModMetaData>> _installedMods;
        private readonly Lazy<List<ModMetaData>> _activeMods;
        private readonly Lazy<List<ModContentPack>> _runningMods;
        private readonly Dictionary<string, List<Def>> _defs =
            new Dictionary<string, List<Def>>(StringComparer.Ordinal);

        public RimTalkArtiCoreModule(PromptContext context)
        {
            _context = context ?? new PromptContext();
            _maps = new Lazy<List<Map>>(LoadMaps);
            _allPawns = new Lazy<List<Pawn>>(LoadAllPawns);
            _factions = new Lazy<List<Faction>>(LoadFactions);
            _worldObjects = new Lazy<List<WorldObject>>(LoadWorldObjects);
            _installedMods = new Lazy<List<ModMetaData>>(LoadInstalledMods);
            _activeMods = new Lazy<List<ModMetaData>>(LoadActiveMods);
            _runningMods = new Lazy<List<ModContentPack>>(LoadRunningMods);

            _root.Set("game", () => CreateGame());
            _root.Set("world", () => CreateWorld());
            _root.Set("find", () => CreateFind());
            _root.Set("engine", () => CreateFind());
            _root.Set("current", () => CreateCurrent());
            _root.Set("maps", () => CreateMaps());
            _root.Set("map", () => CreateMapSelector());
            _root.Set("pawns", () => CreatePawns());
            _root.Set("pawn", () => CreatePawnSelector());
            _root.Set("factions", () => CreateFactions());
            _root.Set("faction", () => CreateFactionSelector());
            _root.Set("settlements", () => CreateSettlements());
            _root.Set("sites", () => CreateSites());
            _root.Set("caravans", () => CreateCaravans());
            _root.Set("world_objects", () => CreateWorldObjects());
            _root.Set("mods", () => CreateMods());
            _root.Set("defs", () => CreateDefs());
            _root.Set("def", () => CreateDefSelector());
            _root.Set("thing", () => CreateThingSelector());
            _root.Set("things", () => CreateThingsSelector());
            _root.Set("cell", () => CreateCellSelector());
            _root.Set("query", () => CreateQuery());
            _root.Set("data", () => CreateQuery());
            _root.Set("read", new RimTalkArtiCallable(ReadMember));
            _root.Set("has", new RimTalkArtiCallable(HasMember));
            _root.Set("keys", new RimTalkArtiCallable(GetMemberNames));
        }

        public bool TryGetCoreMember(string member, out object value)
        {
            return _root.TryGetMember(member, out value);
        }

        internal Faction FindFactionValue(object value)
        {
            return ResolveFaction(
                new List<object> { value },
                null) as Faction;
        }

        private RimTalkArtiLazyNamespace CreateGame()
        {
            RimTalkArtiLazyNamespace game = new RimTalkArtiLazyNamespace();
            game.Set("raw", () => GetGame());
            game.Set("world", () => GetWorld());
            game.Set("current_map", () => GetCurrentMap(), false);
            game.Set("current_map_index", () => GetCurrentMapIndex(), false);
            game.Set("current_map_id", () => GetCurrentMapId(), false);
            game.Set("maps", () => _maps.Value);
            game.Set("map_count", () => _maps.Value.Count, false);
            game.Set("player_home_maps", () => GetPlayerHomeMaps());
            game.Set("any_player_home_map", () => GetAnyPlayerHomeMap(), false);
            game.Set("player_has_control", () => GetPlayerHasControl(), false);
            game.Set("info", () => GetMember(GetGame(), "Info"));
            game.Set("rules", () => GetMember(GetGame(), "Rules"));
            game.Set("components", () => GetMember(GetGame(), "components"));
            game.Set("current_pawn", () => _context.CurrentPawn, false);
            game.Set("recipient", () => _context.TalkRequest == null ? null : _context.TalkRequest.Recipient, false);
            return game;
        }

        private RimTalkArtiLazyNamespace CreateCurrent()
        {
            RimTalkArtiLazyNamespace current = new RimTalkArtiLazyNamespace();
            current.Set("game", () => GetGame(), false);
            current.Set("world", () => GetWorld(), false);
            current.Set("map", () => GetCurrentMap(), false);
            current.Set("current_map", () => GetCurrentMap(), false);
            current.Set("map_index", () => GetCurrentMapIndex(), false);
            current.Set("pawn", () => _context.CurrentPawn, false);
            current.Set("recipient", () => _context.TalkRequest == null ? null : _context.TalkRequest.Recipient, false);
            current.Set("creating_world", () => ReadStaticMember(typeof(Current), "CreatingWorld"), false);
            current.Set("program_state", () => ReadStaticMember(typeof(Current), "ProgramState"), false);
            current.Set("root", () => ReadStaticMember(typeof(Current), "Root"), false);
            return current;
        }

        private RimTalkArtiLazyNamespace CreateWorld()
        {
            RimTalkArtiLazyNamespace world = new RimTalkArtiLazyNamespace();
            world.Set("raw", () => GetWorld());
            world.Set("info", () => GetMember(GetWorld(), "info"));
            world.Set("grid", () => GetMember(GetWorld(), "grid"));
            world.Set("faction_manager", () => GetFactionManager());
            world.Set("world_pawns", () => GetMember(GetWorld(), "worldPawns"));
            world.Set("world_objects", () => GetWorldObjectsHolder());
            world.Set("objects", () => _worldObjects.Value);
            world.Set("settlements", () => GetWorldObjectsList("Settlements"));
            world.Set("settlement_bases", () => GetWorldObjectsList("SettlementBases"));
            world.Set("destroyed_settlements", () => GetWorldObjectsList("DestroyedSettlements"));
            world.Set("sites", () => GetWorldObjectsList("Sites"));
            world.Set("caravans", () => GetWorldObjectsList("Caravans"));
            world.Set("travelling_transporters", () => GetWorldObjectsList("TravellingTransporters"));
            world.Set("peace_talks", () => GetWorldObjectsList("PeaceTalks"));
            world.Set("route_planner_waypoints", () => GetWorldObjectsList("RoutePlannerWaypoints"));
            world.Set("map_parents", () => GetWorldObjectsList("MapParents"));
            world.Set("factions", () => _factions.Value);
            world.Set("pawns", () => _allPawns.Value);
            world.Set("player_pawns", () => GetMember(GetWorld(), "PlayerPawnsForStoryteller"));
            world.Set("components", () => GetMember(GetWorld(), "components"));
            world.Set("pocket_maps", () => GetMember(GetWorld(), "pocketMaps"));
            world.Set("tile_count", () => GetWorldTileCount());
            world.Set("tile", () => GetMember(GetWorld(), "Tile"));
            return world;
        }

        private RimTalkArtiLazyNamespace CreateFind()
        {
            RimTalkArtiLazyNamespace find = new RimTalkArtiLazyNamespace();
            string[] names =
            {
                "Root", "World", "Maps", "CurrentMap", "WorldObjects", "WorldPawns", "WorldGrid",
                "FactionManager", "TickManager", "QuestManager", "ResearchManager", "Storyteller",
                "History", "TaleManager", "PlayLog", "BattleLog", "LetterStack", "Archive",
                "PlaySettings", "IdeoManager", "Anomaly", "SignalManager", "UniqueIDsManager",
                "GameInfo", "Scenario", "StoryWatcher", "MapUI", "Selector", "WindowStack",
                "ColonistBar", "RelationshipRecords", "TransportShipManager", "BossgroupManager",
                "StudyManager", "CustomXenogermDatabase", "PsychicRitualManager", "EntityCodex",
                "CurrentGravship", "WorldFeatures", "WorldPathGrid", "WorldReachability"
            };

            foreach (string name in names)
            {
                string capturedName = name;
                bool dynamic = string.Equals(name, "CurrentMap", StringComparison.Ordinal)
                    || string.Equals(name, "Maps", StringComparison.Ordinal)
                    || string.Equals(name, "WorldObjects", StringComparison.Ordinal);
                find.Set(name, () => ReadFindMember(capturedName), !dynamic);
            }

            find.Set("game", () => GetGame());
            find.Set("current_map_index", () => GetCurrentMapIndex(), false);
            find.Set("map_count", () => _maps.Value.Count, false);
            find.Set("get", new RimTalkArtiCallable(ReadFindMemberByName));
            find.Set("read", new RimTalkArtiCallable(ReadFindMemberByName));
            find.Set("keys", new RimTalkArtiCallable(GetFindMemberNames));
            return find;
        }

        private RimTalkArtiLazyNamespace CreateMaps()
        {
            RimTalkArtiLazyNamespace maps = new RimTalkArtiLazyNamespace();
            maps.Set("all", () => _maps.Value);
            maps.Set("count", () => _maps.Value.Count, false);
            maps.Set("current", () => GetCurrentMap(), false);
            maps.Set("home", () => GetPlayerHomeMaps());
            maps.Set("at", new RimTalkArtiCallable(ResolveMapByIndex));
            maps.Set("by_index", new RimTalkArtiCallable(ResolveMapByIndex));
            maps.Set("by_id", new RimTalkArtiCallable(ResolveMapById));
            maps.Set("find", new RimTalkArtiCallable(ResolveMap));
            return maps;
        }

        private RimTalkArtiCallableNamespace CreateMapSelector()
        {
            RimTalkArtiCallableNamespace selector = new RimTalkArtiCallableNamespace(ResolveMap);
            selector.Set("current", () => GetCurrentMap(), false);
            selector.Set("all", () => _maps.Value);
            selector.Set("count", () => _maps.Value.Count, false);
            selector.Set("at", new RimTalkArtiCallable(ResolveMapByIndex));
            selector.Set("by_index", new RimTalkArtiCallable(ResolveMapByIndex));
            selector.Set("by_id", new RimTalkArtiCallable(ResolveMapById));
            selector.Set("find", new RimTalkArtiCallable(ResolveMap));
            return selector;
        }

        private RimTalkArtiLazyNamespace CreatePawns()
        {
            RimTalkArtiLazyNamespace pawns = new RimTalkArtiLazyNamespace();
            pawns.Set("all", () => _allPawns.Value);
            pawns.Set("alive", () => FilterPawns(delegate(Pawn pawn) { return pawn != null && !pawn.Dead; }));
            pawns.Set("dead", () => FilterPawns(delegate(Pawn pawn) { return pawn != null && pawn.Dead; }));
            pawns.Set("colonists", () => FilterPawns(delegate(Pawn pawn) { return pawn != null && pawn.IsFreeColonist; }));
            pawns.Set("humanlike", () => FilterPawns(IsHumanlike));
            pawns.Set("animals", () => FilterPawns(delegate(Pawn pawn) { return pawn != null && pawn.IsAnimal; }));
            pawns.Set("prisoners", () => FilterPawns(delegate(Pawn pawn) { return pawn != null && pawn.IsPrisoner; }));
            pawns.Set("slaves", () => FilterPawns(delegate(Pawn pawn) { return pawn != null && pawn.IsSlave; }));
            pawns.Set("mechs", () => FilterPawns(delegate(Pawn pawn) { return pawn != null && pawn.IsColonyMech; }));
            pawns.Set("map", () => GetMapPawns(GetCurrentMap()), false);
            pawns.Set("world", () => GetWorldPawns());
            pawns.Set("context", () => _context.AllPawns ?? new List<Pawn>(), false);
            pawns.Set("count", () => _allPawns.Value.Count, false);
            pawns.Set("find", new RimTalkArtiCallable(ResolvePawn));
            pawns.Set("by_id", new RimTalkArtiCallable(ResolvePawn));
            pawns.Set("by_name", new RimTalkArtiCallable(ResolvePawn));
            return pawns;
        }

        private RimTalkArtiCallableNamespace CreatePawnSelector()
        {
            RimTalkArtiCallableNamespace selector = new RimTalkArtiCallableNamespace(ResolvePawn);
            selector.Set("current", () => _context.CurrentPawn, false);
            selector.Set("recipient", () => _context.TalkRequest == null ? null : _context.TalkRequest.Recipient, false);
            selector.Set("all", () => _allPawns.Value);
            selector.Set("find", new RimTalkArtiCallable(ResolvePawn));
            selector.Set("by_id", new RimTalkArtiCallable(ResolvePawn));
            selector.Set("by_name", new RimTalkArtiCallable(ResolvePawn));
            selector.Set("info", () => RimTalkArtiPawnInfo.Create(_context.CurrentPawn), false);
            selector.Set("prompt", () => CreatePawnPromptNamespace(_context.CurrentPawn), false);
            selector.Set("location", () => RimTalkArtiPawnPromptData.GetLocation(_context.CurrentPawn), false);
            selector.Set("terrain", () => RimTalkArtiPawnPromptData.GetTerrain(_context.CurrentPawn), false);
            selector.Set("beauty", () => RimTalkArtiPawnPromptData.GetBeauty(_context.CurrentPawn), false);
            selector.Set("cleanliness", () => RimTalkArtiPawnPromptData.GetCleanliness(_context.CurrentPawn), false);
            selector.Set("surroundings", () => RimTalkArtiPawnPromptData.GetNearbyThingsText(_context.CurrentPawn), false);
            selector.Set("nearby_things", () => RimTalkArtiPawnPromptData.GetNearbyThingsText(_context.CurrentPawn), false);
            selector.Set("nearby_things_text", () => RimTalkArtiPawnPromptData.GetNearbyThingsText(_context.CurrentPawn), false);
            selector.Set("nearby_things_raw", () => RimTalkArtiPawnPromptData.GetNearbyThings(_context.CurrentPawn), false);
            selector.Set("nearby_items", () => RimTalkArtiPawnPromptData.GetNearbyItems(_context.CurrentPawn), false);
            selector.Set("nearby_buildings", () => RimTalkArtiPawnPromptData.GetNearbyBuildings(_context.CurrentPawn), false);
            selector.Set("nearby_plants", () => RimTalkArtiPawnPromptData.GetNearbyPlants(_context.CurrentPawn), false);
            selector.Set("nearby_animals", () => RimTalkArtiPawnPromptData.GetNearbyAnimals(_context.CurrentPawn), false);
            selector.Set("nearby_filth", () => RimTalkArtiPawnPromptData.GetNearbyFilth(_context.CurrentPawn), false);
            return selector;
        }

        private RimTalkArtiLazyNamespace CreatePawnPromptNamespace(Pawn pawn)
        {
            RimTalkArtiLazyNamespace prompt = new RimTalkArtiLazyNamespace();
            prompt.Set("location", () => RimTalkArtiPawnPromptData.GetLocation(pawn), false);
            prompt.Set("terrain", () => RimTalkArtiPawnPromptData.GetTerrain(pawn), false);
            prompt.Set("beauty", () => RimTalkArtiPawnPromptData.GetBeauty(pawn), false);
            prompt.Set("cleanliness", () => RimTalkArtiPawnPromptData.GetCleanliness(pawn), false);
            prompt.Set("surroundings", () => RimTalkArtiPawnPromptData.GetNearbyThingsText(pawn), false);
            prompt.Set("nearby_things", () => RimTalkArtiPawnPromptData.GetNearbyThingsText(pawn), false);
            prompt.Set("nearby_things_text", () => RimTalkArtiPawnPromptData.GetNearbyThingsText(pawn), false);
            prompt.Set("nearby_things_raw", () => RimTalkArtiPawnPromptData.GetNearbyThings(pawn), false);
            prompt.Set("nearby_items", () => RimTalkArtiPawnPromptData.GetNearbyItems(pawn), false);
            prompt.Set("nearby_buildings", () => RimTalkArtiPawnPromptData.GetNearbyBuildings(pawn), false);
            prompt.Set("nearby_plants", () => RimTalkArtiPawnPromptData.GetNearbyPlants(pawn), false);
            prompt.Set("nearby_animals", () => RimTalkArtiPawnPromptData.GetNearbyAnimals(pawn), false);
            prompt.Set("nearby_filth", () => RimTalkArtiPawnPromptData.GetNearbyFilth(pawn), false);
            return prompt;
        }

        private RimTalkArtiLazyNamespace CreateFactions()
        {
            RimTalkArtiLazyNamespace factions = new RimTalkArtiLazyNamespace();
            factions.Set("all", () => _factions.Value);
            factions.Set("player", () => GetPlayerFaction(), false);
            factions.Set("hostile", () => FilterFactions(IsHostileToPlayer));
            factions.Set("non_hostile", () => FilterFactions(delegate(Faction faction)
            {
                return faction != null && !IsHostileToPlayer(faction);
            }));
            factions.Set("friendly", () => FilterFactions(delegate(Faction faction)
            {
                return faction != null && faction != Faction.OfPlayer && !IsHostileToPlayer(faction);
            }));
            factions.Set("humanlike", () => FilterFactions(IsHumanlikeFaction));
            factions.Set("hidden", () => FilterFactions(delegate(Faction faction)
            {
                return faction != null && faction.Hidden;
            }));
            factions.Set("count", () => _factions.Value.Count, false);
            factions.Set("find", new RimTalkArtiCallable(ResolveFaction));
            factions.Set("by_id", new RimTalkArtiCallable(ResolveFaction));
            factions.Set("by_name", new RimTalkArtiCallable(ResolveFaction));
            return factions;
        }

        private RimTalkArtiCallableNamespace CreateFactionSelector()
        {
            RimTalkArtiCallableNamespace selector = new RimTalkArtiCallableNamespace(ResolveFaction);
            selector.Set("player", () => GetPlayerFaction(), false);
            selector.Set("all", () => _factions.Value);
            selector.Set("hostile", () => FilterFactions(IsHostileToPlayer));
            selector.Set("find", new RimTalkArtiCallable(ResolveFaction));
            selector.Set("by_id", new RimTalkArtiCallable(ResolveFaction));
            selector.Set("by_name", new RimTalkArtiCallable(ResolveFaction));
            return selector;
        }

        private RimTalkArtiLazyNamespace CreateSettlements()
        {
            RimTalkArtiLazyNamespace settlements = new RimTalkArtiLazyNamespace();
            settlements.Set("all", () => GetWorldObjectsList("Settlements"));
            settlements.Set("player", () => FilterWorldObjects(GetWorldObjectsList("Settlements"), delegate(WorldObject item)
            {
                return item != null && item.Faction == Faction.OfPlayer;
            }));
            settlements.Set("hostile", () => FilterWorldObjects(GetWorldObjectsList("Settlements"), IsHostileWorldObject));
            settlements.Set("count", () => GetWorldObjectsList("Settlements").Count, false);
            settlements.Set("find", new RimTalkArtiCallable(ResolveWorldObject));
            return settlements;
        }

        private RimTalkArtiLazyNamespace CreateSites()
        {
            RimTalkArtiLazyNamespace sites = new RimTalkArtiLazyNamespace();
            sites.Set("all", () => GetWorldObjectsList("Sites"));
            sites.Set("hostile", () => FilterWorldObjects(GetWorldObjectsList("Sites"), IsHostileWorldObject));
            sites.Set("count", () => GetWorldObjectsList("Sites").Count, false);
            sites.Set("find", new RimTalkArtiCallable(ResolveWorldObject));
            return sites;
        }

        private RimTalkArtiLazyNamespace CreateCaravans()
        {
            RimTalkArtiLazyNamespace caravans = new RimTalkArtiLazyNamespace();
            caravans.Set("all", () => GetWorldObjectsList("Caravans"));
            caravans.Set("player", () => FilterWorldObjects(GetWorldObjectsList("Caravans"), delegate(WorldObject item)
            {
                Caravan caravan = item as Caravan;
                return caravan != null && caravan.IsPlayerControlled;
            }));
            caravans.Set("count", () => GetWorldObjectsList("Caravans").Count, false);
            caravans.Set("find", new RimTalkArtiCallable(ResolveWorldObject));
            return caravans;
        }

        private RimTalkArtiLazyNamespace CreateWorldObjects()
        {
            RimTalkArtiLazyNamespace objects = new RimTalkArtiLazyNamespace();
            objects.Set("all", () => _worldObjects.Value);
            objects.Set("settlements", () => GetWorldObjectsList("Settlements"));
            objects.Set("settlement_bases", () => GetWorldObjectsList("SettlementBases"));
            objects.Set("destroyed_settlements", () => GetWorldObjectsList("DestroyedSettlements"));
            objects.Set("sites", () => GetWorldObjectsList("Sites"));
            objects.Set("caravans", () => GetWorldObjectsList("Caravans"));
            objects.Set("travelling_transporters", () => GetWorldObjectsList("TravellingTransporters"));
            objects.Set("peace_talks", () => GetWorldObjectsList("PeaceTalks"));
            objects.Set("route_planner_waypoints", () => GetWorldObjectsList("RoutePlannerWaypoints"));
            objects.Set("map_parents", () => GetWorldObjectsList("MapParents"));
            objects.Set("count", () => _worldObjects.Value.Count, false);
            objects.Set("at", new RimTalkArtiCallable(ResolveWorldObjectsAt));
            objects.Set("find", new RimTalkArtiCallable(ResolveWorldObject));
            return objects;
        }

        private RimTalkArtiLazyNamespace CreateMods()
        {
            RimTalkArtiLazyNamespace mods = new RimTalkArtiLazyNamespace();
            mods.Set("installed", () => _installedMods.Value);
            mods.Set("active", () => _activeMods.Value);
            mods.Set("running", () => _runningMods.Value);
            mods.Set("all", () => _installedMods.Value);
            mods.Set("installed_count", () => _installedMods.Value.Count, false);
            mods.Set("active_count", () => _activeMods.Value.Count, false);
            mods.Set("running_count", () => _runningMods.Value.Count, false);
            mods.Set("count", () => _installedMods.Value.Count, false);
            mods.Set("dlc", () => CreateDlc());
            mods.Set("find", new RimTalkArtiCallable(ResolveMod));
            return mods;
        }

        private RimTalkArtiLazyNamespace CreateDlc()
        {
            RimTalkArtiLazyNamespace dlc = new RimTalkArtiLazyNamespace();
            dlc.Set("royalty_installed", () => ModLister.RoyaltyInstalled);
            dlc.Set("ideology_installed", () => ModLister.IdeologyInstalled);
            dlc.Set("biotech_installed", () => ModLister.BiotechInstalled);
            dlc.Set("anomaly_installed", () => ModLister.AnomalyInstalled);
            dlc.Set("odyssey_installed", () => ModLister.OdysseyInstalled);
            dlc.Set("royalty_active", () => ModsConfig.RoyaltyActive);
            dlc.Set("ideology_active", () => ModsConfig.IdeologyActive);
            dlc.Set("biotech_active", () => ModsConfig.BiotechActive);
            dlc.Set("anomaly_active", () => ModsConfig.AnomalyActive);
            dlc.Set("odyssey_active", () => ModsConfig.OdysseyActive);
            return dlc;
        }

        private RimTalkArtiLazyNamespace CreateDefs()
        {
            RimTalkArtiLazyNamespace defs = new RimTalkArtiLazyNamespace();
            defs.Set("all", new RimTalkArtiCallable(GetDefs));
            defs.Set("all_defs", () => GetDefsByName("all"));
            defs.Set("find", new RimTalkArtiCallable(FindDef));
            defs.Set("count", new RimTalkArtiCallable(CountDefs));
            defs.Set("thing", () => GetDefsByName("ThingDef"));
            defs.Set("things", () => GetDefsByName("ThingDef"));
            defs.Set("pawn_kind", () => GetDefsByName("PawnKindDef"));
            defs.Set("pawn_kinds", () => GetDefsByName("PawnKindDef"));
            defs.Set("faction", () => GetDefsByName("FactionDef"));
            defs.Set("factions", () => GetDefsByName("FactionDef"));
            defs.Set("biome", () => GetDefsByName("BiomeDef"));
            defs.Set("biomes", () => GetDefsByName("BiomeDef"));
            defs.Set("terrain", () => GetDefsByName("TerrainDef"));
            defs.Set("terrains", () => GetDefsByName("TerrainDef"));
            defs.Set("hediff", () => GetDefsByName("HediffDef"));
            defs.Set("hediffs", () => GetDefsByName("HediffDef"));
            defs.Set("trait", () => GetDefsByName("TraitDef"));
            defs.Set("traits", () => GetDefsByName("TraitDef"));
            defs.Set("skill", () => GetDefsByName("SkillDef"));
            defs.Set("skills", () => GetDefsByName("SkillDef"));
            defs.Set("work_type", () => GetDefsByName("WorkTypeDef"));
            defs.Set("research", () => GetDefsByName("ResearchProjectDef"));
            defs.Set("incident", () => GetDefsByName("IncidentDef"));
            defs.Set("recipe", () => GetDefsByName("RecipeDef"));
            defs.Set("job", () => GetDefsByName("JobDef"));
            defs.Set("interaction", () => GetDefsByName("InteractionDef"));
            defs.Set("thought", () => GetDefsByName("ThoughtDef"));
            defs.Set("gene", () => GetDefsByName("GeneDef"));
            defs.Set("ability", () => GetDefsByName("AbilityDef"));
            defs.Set("world_object", () => GetDefsByName("WorldObjectDef"));
            defs.Set("map_generator", () => GetDefsByName("MapGeneratorDef"));
            defs.Set("game_condition", () => GetDefsByName("GameConditionDef"));
            return defs;
        }

        private RimTalkArtiCallableNamespace CreateDefSelector()
        {
            RimTalkArtiCallableNamespace selector = new RimTalkArtiCallableNamespace(FindDef);
            selector.Set("all", new RimTalkArtiCallable(GetDefs));
            selector.Set("find", new RimTalkArtiCallable(FindDef));
            selector.Set("count", new RimTalkArtiCallable(CountDefs));
            return selector;
        }

        private RimTalkArtiCallableNamespace CreateThingSelector()
        {
            RimTalkArtiCallableNamespace selector = new RimTalkArtiCallableNamespace(ResolveThing);
            selector.Set("find", new RimTalkArtiCallable(ResolveThing));
            selector.Set("at", new RimTalkArtiCallable(ResolveThingsAt));
            selector.Set("all", () => GetThings(null, null));
            selector.Set("count", () => ((List<Thing>)GetThings(null, null)).Count, false);
            return selector;
        }

        private RimTalkArtiCallableNamespace CreateThingsSelector()
        {
            RimTalkArtiCallableNamespace selector = new RimTalkArtiCallableNamespace(GetThings);
            selector.Set("all", () => GetThings(null, null));
            selector.Set("find", new RimTalkArtiCallable(ResolveThing));
            selector.Set("at", new RimTalkArtiCallable(ResolveThingsAt));
            selector.Set("count", () => ((List<Thing>)GetThings(null, null)).Count, false);
            return selector;
        }

        private RimTalkArtiCallableNamespace CreateCellSelector()
        {
            RimTalkArtiCallableNamespace selector = new RimTalkArtiCallableNamespace(CreateCell);
            selector.Set("at", new RimTalkArtiCallable(CreateCell));
            selector.Set("things", new RimTalkArtiCallable(ResolveThingsAt));
            selector.Set("pawns", new RimTalkArtiCallable(ResolvePawnsAt));
            return selector;
        }

        private RimTalkArtiLazyNamespace CreateQuery()
        {
            RimTalkArtiLazyNamespace query = new RimTalkArtiLazyNamespace();
            query.Set("maps", () => _maps.Value);
            query.Set("pawns", new RimTalkArtiCallable(QueryMapPawns));
            query.Set("things", new RimTalkArtiCallable(GetThings));
            query.Set("buildings", new RimTalkArtiCallable(GetMapBuildings));
            query.Set("plants", new RimTalkArtiCallable(GetMapPlants));
            query.Set("items", new RimTalkArtiCallable(GetMapItems));
            query.Set("things_at", new RimTalkArtiCallable(ResolveThingsAt));
            query.Set("pawns_at", new RimTalkArtiCallable(ResolvePawnsAt));
            query.Set("objects_at", new RimTalkArtiCallable(ResolveWorldObjectsAt));
            query.Set("factions_on_map", new RimTalkArtiCallable(GetFactionsOnMap));
            query.Set("all_pawns", () => _allPawns.Value);
            query.Set("all_factions", () => _factions.Value);
            query.Set("all_mods", () => _installedMods.Value);
            return query;
        }

        private object GetGame()
        {
            return Current.Game;
        }

        private object GetWorld()
        {
            Game game = Current.Game;
            if (game != null && game.World != null)
            {
                return game.World;
            }

            return ReadFindMember("World");
        }

        private Map GetCurrentMap()
        {
            Game game = Current.Game;
            if (game != null)
            {
                return game.CurrentMap;
            }

            return ReadFindMember("CurrentMap") as Map;
        }

        private int GetCurrentMapIndex()
        {
            Game game = Current.Game;
            return game == null ? -1 : game.currentMapIndex;
        }

        private int GetCurrentMapId()
        {
            Map map = GetCurrentMap();
            return map == null ? 0 : map.uniqueID;
        }

        private List<Map> LoadMaps()
        {
            try
            {
                Game game = Current.Game;
                if (game != null && game.Maps != null)
                {
                    return new List<Map>(game.Maps);
                }

                List<Map> maps = ReadFindMember("Maps") as List<Map>;
                return maps == null ? new List<Map>() : new List<Map>(maps);
            }
            catch (Exception)
            {
                return new List<Map>();
            }
        }

        private object GetPlayerHomeMaps()
        {
            Game game = Current.Game;
            object value = GetMember(game, "PlayerHomeMaps");
            return value ?? new List<Map>();
        }

        private object GetAnyPlayerHomeMap()
        {
            Game game = Current.Game;
            return GetMember(game, "AnyPlayerHomeMap");
        }

        private bool GetPlayerHasControl()
        {
            Game game = Current.Game;
            object value = GetMember(game, "PlayerHasControl");
            return value is bool && (bool)value;
        }

        private int GetWorldTileCount()
        {
            object grid = GetMember(GetWorld(), "grid");
            object value = GetMember(grid, "TilesCount");
            return ToInt(value, 0);
        }

        private FactionManager GetFactionManager()
        {
            World world = GetWorld() as World;
            if (world != null && world.factionManager != null)
            {
                return world.factionManager;
            }

            return ReadFindMember("FactionManager") as FactionManager;
        }

        private WorldObjectsHolder GetWorldObjectsHolder()
        {
            World world = GetWorld() as World;
            if (world != null && world.worldObjects != null)
            {
                return world.worldObjects;
            }

            return ReadFindMember("WorldObjects") as WorldObjectsHolder;
        }

        private List<WorldObject> LoadWorldObjects()
        {
            WorldObjectsHolder holder = GetWorldObjectsHolder();
            return holder == null || holder.AllWorldObjects == null
                ? new List<WorldObject>()
                : new List<WorldObject>(holder.AllWorldObjects);
        }

        private List<WorldObject> GetWorldObjectsList(string property)
        {
            WorldObjectsHolder holder = GetWorldObjectsHolder();
            object value = GetMember(holder, property);
            IEnumerable enumerable = value as IEnumerable;
            List<WorldObject> result = new List<WorldObject>();
            if (enumerable != null)
            {
                foreach (object item in enumerable)
                {
                    WorldObject worldObject = item as WorldObject;
                    if (worldObject != null)
                    {
                        result.Add(worldObject);
                    }
                }
            }

            return result;
        }

        private List<Pawn> LoadAllPawns()
        {
            List<Pawn> result = new List<Pawn>();
            HashSet<Pawn> seen = new HashSet<Pawn>();
            AddPawns(result, seen, _context.AllPawns);

            foreach (Map map in _maps.Value)
            {
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                AddPawns(result, seen, map.mapPawns.AllPawns);
                AddPawns(result, seen, map.mapPawns.AllPawnsUnspawned);
            }

            World world = GetWorld() as World;
            if (world != null && world.worldPawns != null)
            {
                AddPawns(result, seen, world.worldPawns.AllPawnsAliveOrDead);
            }
            else
            {
                WorldPawns worldPawns = ReadFindMember("WorldPawns") as WorldPawns;
                if (worldPawns != null)
                {
                    AddPawns(result, seen, worldPawns.AllPawnsAliveOrDead);
                }
            }

            return result;
        }

        private static void AddPawns(List<Pawn> target, HashSet<Pawn> seen, IEnumerable<Pawn> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (Pawn pawn in source)
            {
                if (pawn != null && seen.Add(pawn))
                {
                    target.Add(pawn);
                }
            }
        }

        private List<Pawn> GetWorldPawns()
        {
            World world = GetWorld() as World;
            WorldPawns worldPawns = world == null ? null : world.worldPawns;
            if (worldPawns == null)
            {
                worldPawns = ReadFindMember("WorldPawns") as WorldPawns;
            }

            return worldPawns == null || worldPawns.AllPawnsAliveOrDead == null
                ? new List<Pawn>()
                : new List<Pawn>(worldPawns.AllPawnsAliveOrDead);
        }

        private List<Pawn> GetMapPawns(Map map)
        {
            if (map == null || map.mapPawns == null || map.mapPawns.AllPawns == null)
            {
                return new List<Pawn>();
            }

            return new List<Pawn>(map.mapPawns.AllPawns);
        }

        private List<Faction> LoadFactions()
        {
            FactionManager manager = GetFactionManager();
            return manager == null || manager.AllFactionsListForReading == null
                ? new List<Faction>()
                : new List<Faction>(manager.AllFactionsListForReading);
        }

        private List<ModMetaData> LoadInstalledMods()
        {
            List<ModMetaData> result = new List<ModMetaData>();
            try
            {
                IEnumerable<ModMetaData> mods = ModLister.AllInstalledMods;
                if (mods != null)
                {
                    foreach (ModMetaData mod in mods)
                    {
                        if (mod != null)
                        {
                            result.Add(mod);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        private List<ModMetaData> LoadActiveMods()
        {
            List<ModMetaData> result = new List<ModMetaData>();
            try
            {
                IEnumerable<ModMetaData> mods = ModsConfig.ActiveModsInLoadOrder;
                if (mods != null)
                {
                    foreach (ModMetaData mod in mods)
                    {
                        if (mod != null)
                        {
                            result.Add(mod);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        private List<ModContentPack> LoadRunningMods()
        {
            try
            {
                return LoadedModManager.RunningModsListForReading == null
                    ? new List<ModContentPack>()
                    : new List<ModContentPack>(LoadedModManager.RunningModsListForReading);
            }
            catch (Exception)
            {
                return new List<ModContentPack>();
            }
        }

        private object ResolveMap(IList<object> positional, IDictionary<string, object> named)
        {
            object argument = GetArgument(positional, named, 0, "map", null);
            return ResolveMapValue(argument, false);
        }

        private object ResolveMapByIndex(IList<object> positional, IDictionary<string, object> named)
        {
            object argument = GetArgument(positional, named, 0, "index", null);
            int index;
            return TryGetInt(argument, out index) && index >= 0 && index < _maps.Value.Count
                ? _maps.Value[index]
                : null;
        }

        private object ResolveMapById(IList<object> positional, IDictionary<string, object> named)
        {
            object argument = GetArgument(positional, named, 0, "id", null);
            int id;
            if (TryGetInt(argument, out id))
            {
                foreach (Map map in _maps.Value)
                {
                    if (map != null && map.uniqueID == id)
                    {
                        return map;
                    }
                }
            }

            return ResolveMapValue(argument, false);
        }

        private Map ResolveMapValue(object argument, bool preferIndex)
        {
            if (argument == null)
            {
                return GetCurrentMap();
            }

            Map direct = argument as Map;
            if (direct != null)
            {
                return direct;
            }

            PlanetTile tile;
            if (argument is PlanetTile)
            {
                tile = (PlanetTile)argument;
                foreach (Map map in _maps.Value)
                {
                    if (map != null && map.Tile == tile)
                    {
                        return map;
                    }
                }
            }

            int number;
            if (TryGetInt(argument, out number))
            {
                if (preferIndex && number >= 0 && number < _maps.Value.Count)
                {
                    return _maps.Value[number];
                }

                foreach (Map map in _maps.Value)
                {
                    if (map != null && map.uniqueID == number)
                    {
                        return map;
                    }
                }

                if (number >= 0 && number < _maps.Value.Count)
                {
                    return _maps.Value[number];
                }
            }

            string text = Convert.ToString(argument, CultureInfo.InvariantCulture) ?? string.Empty;
            foreach (Map map in _maps.Value)
            {
                if (map == null)
                {
                    continue;
                }

                string label = Convert.ToString(GetMember(map, "Label"), CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(label) && map.Parent != null)
                {
                    label = map.Parent.Label ?? string.Empty;
                }
                if (string.Equals(label, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(map.uniqueID.ToString(CultureInfo.InvariantCulture), text, StringComparison.OrdinalIgnoreCase))
                {
                    return map;
                }
            }

            return null;
        }

        private object ResolvePawn(IList<object> positional, IDictionary<string, object> named)
        {
            object argument = GetArgument(positional, named, 0, "id", null);
            if (argument == null)
            {
                return _context.CurrentPawn;
            }

            Pawn direct = argument as Pawn;
            if (direct != null)
            {
                return direct;
            }

            int number;
            if (TryGetInt(argument, out number))
            {
                foreach (Pawn pawn in _allPawns.Value)
                {
                    if (pawn != null && pawn.thingIDNumber == number)
                    {
                        return pawn;
                    }
                }
            }

            string text = Convert.ToString(argument, CultureInfo.InvariantCulture) ?? string.Empty;
            foreach (Pawn pawn in _allPawns.Value)
            {
                if (pawn == null)
                {
                    continue;
                }

                if (string.Equals(pawn.ThingID, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(pawn.LabelShort, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(pawn.Name == null ? string.Empty : pawn.Name.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    return pawn;
                }
            }

            return null;
        }

        private object ResolveFaction(IList<object> positional, IDictionary<string, object> named)
        {
            object argument = GetArgument(positional, named, 0, "id", null);
            if (argument == null)
            {
                return GetPlayerFaction();
            }

            Faction direct = argument as Faction;
            if (direct != null)
            {
                return direct;
            }

            int number;
            if (TryGetInt(argument, out number))
            {
                foreach (Faction faction in _factions.Value)
                {
                    if (faction != null && faction.loadID == number)
                    {
                        return faction;
                    }
                }
            }

            string text = Convert.ToString(argument, CultureInfo.InvariantCulture) ?? string.Empty;
            foreach (Faction faction in _factions.Value)
            {
                if (faction == null)
                {
                    continue;
                }

                string defName = faction.def == null ? string.Empty : faction.def.defName;
                if (string.Equals(faction.Name, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(defName, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(faction.loadID.ToString(CultureInfo.InvariantCulture), text, StringComparison.OrdinalIgnoreCase))
                {
                    return faction;
                }
            }

            return null;
        }

        private object ResolveWorldObject(IList<object> positional, IDictionary<string, object> named)
        {
            object argument = GetArgument(positional, named, 0, "id", null);
            if (argument == null)
            {
                return null;
            }

            WorldObject direct = argument as WorldObject;
            if (direct != null)
            {
                return direct;
            }

            int number;
            if (TryGetInt(argument, out number))
            {
                foreach (WorldObject worldObject in _worldObjects.Value)
                {
                    if (worldObject != null && worldObject.ID == number)
                    {
                        return worldObject;
                    }
                }
            }

            string text = Convert.ToString(argument, CultureInfo.InvariantCulture) ?? string.Empty;
            foreach (WorldObject worldObject in _worldObjects.Value)
            {
                if (worldObject == null)
                {
                    continue;
                }

                if (string.Equals(worldObject.Label, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(worldObject.LabelCap, text, StringComparison.OrdinalIgnoreCase))
                {
                    return worldObject;
                }
            }

            return null;
        }

        private object ResolveWorldObjectsAt(IList<object> positional, IDictionary<string, object> named)
        {
            object argument = GetArgument(positional, named, 0, "tile", null);
            int tileId;
            if (!TryGetInt(argument, out tileId))
            {
                return new List<WorldObject>();
            }

            WorldObjectsHolder holder = GetWorldObjectsHolder();
            if (holder == null)
            {
                return new List<WorldObject>();
            }

            List<WorldObject> result = new List<WorldObject>();
            foreach (WorldObject worldObject in holder.ObjectsAt(new PlanetTile(tileId)))
            {
                if (worldObject != null)
                {
                    result.Add(worldObject);
                }
            }

            return result;
        }

        private object ResolveMod(IList<object> positional, IDictionary<string, object> named)
        {
            object argument = GetArgument(positional, named, 0, "id", null);
            ModMetaData direct = argument as ModMetaData;
            if (direct != null)
            {
                return direct;
            }

            string text = Convert.ToString(argument, CultureInfo.InvariantCulture) ?? string.Empty;
            foreach (ModMetaData mod in _installedMods.Value)
            {
                if (mod == null)
                {
                    continue;
                }

                if (string.Equals(mod.PackageId, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(mod.FolderName, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(mod.Name, text, StringComparison.OrdinalIgnoreCase))
                {
                    return mod;
                }
            }

            return null;
        }

        private object GetDefs(IList<object> positional, IDictionary<string, object> named)
        {
            object typeName = GetArgument(positional, named, 0, "type", "ThingDef");
            return GetDefsByName(Convert.ToString(typeName, CultureInfo.InvariantCulture));
        }

        private object FindDef(IList<object> positional, IDictionary<string, object> named)
        {
            object typeName = GetArgument(positional, named, 0, "type", "ThingDef");
            object defName = GetArgument(positional, named, 1, "name", null);
            return RimTalkArtiDefDatabase.Find(
                Convert.ToString(typeName, CultureInfo.InvariantCulture),
                defName);
        }

        private object CountDefs(IList<object> positional, IDictionary<string, object> named)
        {
            object typeName = GetArgument(positional, named, 0, "type", "ThingDef");
            return GetDefsByName(Convert.ToString(typeName, CultureInfo.InvariantCulture)).Count;
        }

        private List<Def> GetDefsByName(string typeName)
        {
            string key = RimTalkArtiNames.Normalize(typeName);
            List<Def> result;
            if (!_defs.TryGetValue(key, out result))
            {
                result = RimTalkArtiDefDatabase.All(typeName);
                _defs[key] = result;
            }

            return result;
        }

        private object ResolveThing(IList<object> positional, IDictionary<string, object> named)
        {
            object identifier = GetArgument(positional, named, 0, "id", null);
            Map map = ResolveMapValue(GetArgument(positional, named, 1, "map", null), false);
            if (map == null)
            {
                map = GetCurrentMap();
            }

            Thing direct = identifier as Thing;
            if (direct != null)
            {
                return direct;
            }

            IEnumerable<Thing> things = GetThingsForMap(map);
            int number;
            if (TryGetInt(identifier, out number))
            {
                foreach (Thing thing in things)
                {
                    if (thing != null && thing.thingIDNumber == number)
                    {
                        return thing;
                    }
                }
            }

            string text = Convert.ToString(identifier, CultureInfo.InvariantCulture) ?? string.Empty;
            foreach (Thing thing in things)
            {
                if (thing == null)
                {
                    continue;
                }

                string defName = thing.def == null ? string.Empty : thing.def.defName;
                if (string.Equals(thing.ThingID, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(thing.Label, text, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(defName, text, StringComparison.OrdinalIgnoreCase))
                {
                    return thing;
                }
            }

            return null;
        }

        private object GetThings(IList<object> positional, IDictionary<string, object> named)
        {
            Map map = ResolveMapValue(GetArgument(positional, named, 0, "map", null), false);
            return new List<Thing>(GetThingsForMap(map));
        }

        private object QueryMapPawns(IList<object> positional, IDictionary<string, object> named)
        {
            Map map = ResolveMapValue(GetArgument(positional, named, 0, "map", null), false);
            return GetMapPawns(map);
        }

        private object GetMapBuildings(IList<object> positional, IDictionary<string, object> named)
        {
            List<Thing> result = new List<Thing>();
            foreach (Thing thing in GetThingsForMap(ResolveMapValue(GetArgument(positional, named, 0, "map", null), false)))
            {
                if (thing is Building)
                {
                    result.Add(thing);
                }
            }

            return result;
        }

        private object GetMapPlants(IList<object> positional, IDictionary<string, object> named)
        {
            List<Thing> result = new List<Thing>();
            foreach (Thing thing in GetThingsForMap(ResolveMapValue(GetArgument(positional, named, 0, "map", null), false)))
            {
                if (thing is Plant)
                {
                    result.Add(thing);
                }
            }

            return result;
        }

        private object GetMapItems(IList<object> positional, IDictionary<string, object> named)
        {
            List<Thing> result = new List<Thing>();
            foreach (Thing thing in GetThingsForMap(ResolveMapValue(GetArgument(positional, named, 0, "map", null), false)))
            {
                if (thing != null && thing.def != null && thing.def.category == ThingCategory.Item)
                {
                    result.Add(thing);
                }
            }

            return result;
        }

        private object ResolveThingsAt(IList<object> positional, IDictionary<string, object> named)
        {
            Map map = ResolveMapValue(GetArgument(positional, named, 0, "map", null), false);
            int x;
            int z;
            if (!TryGetInt(GetArgument(positional, named, 1, "x", null), out x)
                || !TryGetInt(GetArgument(positional, named, 2, "z", null), out z)
                || map == null
                || map.thingGrid == null)
            {
                return new List<Thing>();
            }

            return map.thingGrid.ThingsListAt(new IntVec3(x, 0, z));
        }

        private object ResolvePawnsAt(IList<object> positional, IDictionary<string, object> named)
        {
            Map map = ResolveMapValue(GetArgument(positional, named, 0, "map", null), false);
            int x;
            int z;
            if (!TryGetInt(GetArgument(positional, named, 1, "x", null), out x)
                || !TryGetInt(GetArgument(positional, named, 2, "z", null), out z)
                || map == null)
            {
                return new List<Pawn>();
            }

            IntVec3 cell = new IntVec3(x, 0, z);
            List<Pawn> result = new List<Pawn>();
            foreach (Pawn pawn in GetMapPawns(map))
            {
                if (pawn != null && pawn.Position == cell)
                {
                    result.Add(pawn);
                }
            }

            return result;
        }

        private object CreateCell(IList<object> positional, IDictionary<string, object> named)
        {
            Map map = ResolveMapValue(GetArgument(positional, named, 0, "map", null), false);
            int x;
            int z;
            if (!TryGetInt(GetArgument(positional, named, 1, "x", null), out x)
                || !TryGetInt(GetArgument(positional, named, 2, "z", null), out z))
            {
                return new IntVec3(0, 0, 0);
            }

            IntVec3 result = new IntVec3(x, 0, z);
            return map == null || result.InBounds(map) ? result : new IntVec3(0, 0, 0);
        }

        private object GetFactionsOnMap(IList<object> positional, IDictionary<string, object> named)
        {
            Map map = ResolveMapValue(GetArgument(positional, named, 0, "map", null), false);
            List<Faction> result = new List<Faction>();
            HashSet<Faction> seen = new HashSet<Faction>();
            foreach (Pawn pawn in GetMapPawns(map))
            {
                if (pawn != null && pawn.Faction != null && seen.Add(pawn.Faction))
                {
                    result.Add(pawn.Faction);
                }
            }

            return result;
        }

        private object ReadMember(IList<object> positional, IDictionary<string, object> named)
        {
            object target = GetArgument(positional, named, 0, "target", null);
            string member = Convert.ToString(
                GetArgument(positional, named, 1, "member", string.Empty),
                CultureInfo.InvariantCulture);
            if (target is RimTalkArtiLazyNamespace)
            {
                object lazyValue;
                return ((RimTalkArtiLazyNamespace)target).TryGetMember(member, out lazyValue)
                    ? lazyValue
                    : null;
            }

            object value;
            return RimTalkArtiPublicValueReader.TryRead(target, member, out value) ? value : null;
        }

        private object HasMember(IList<object> positional, IDictionary<string, object> named)
        {
            object target = GetArgument(positional, named, 0, "target", null);
            string member = Convert.ToString(
                GetArgument(positional, named, 1, "member", string.Empty),
                CultureInfo.InvariantCulture);
            if (target is RimTalkArtiLazyNamespace)
            {
                object lazyValue;
                return ((RimTalkArtiLazyNamespace)target).TryGetMember(member, out lazyValue);
            }

            object value;
            return RimTalkArtiPublicValueReader.TryRead(target, member, out value);
        }

        private object GetMemberNames(IList<object> positional, IDictionary<string, object> named)
        {
            object target = GetArgument(positional, named, 0, "target", null);
            if (target is RimTalkArtiLazyNamespace)
            {
                return ((RimTalkArtiLazyNamespace)target).GetMemberNames();
            }

            return RimTalkArtiPublicValueReader.GetPublicMemberNames(target);
        }

        private object ReadFindMemberByName(IList<object> positional, IDictionary<string, object> named)
        {
            string member = Convert.ToString(
                GetArgument(positional, named, 0, "member", string.Empty),
                CultureInfo.InvariantCulture);
            return ReadFindMember(member);
        }

        private object GetFindMemberNames(IList<object> positional, IDictionary<string, object> named)
        {
            return RimTalkArtiPublicValueReader.GetPublicStaticMemberNames(typeof(Find));
        }

        private IEnumerable<Thing> GetThingsForMap(Map map)
        {
            if (map == null || map.listerThings == null || map.listerThings.AllThings == null)
            {
                return new List<Thing>();
            }

            return map.listerThings.AllThings;
        }

        private List<Pawn> FilterPawns(Func<Pawn, bool> predicate)
        {
            List<Pawn> result = new List<Pawn>();
            foreach (Pawn pawn in _allPawns.Value)
            {
                if (predicate(pawn))
                {
                    result.Add(pawn);
                }
            }

            return result;
        }

        private List<Faction> FilterFactions(Func<Faction, bool> predicate)
        {
            List<Faction> result = new List<Faction>();
            foreach (Faction faction in _factions.Value)
            {
                if (predicate(faction))
                {
                    result.Add(faction);
                }
            }

            return result;
        }

        private static List<WorldObject> FilterWorldObjects(
            List<WorldObject> source,
            Func<WorldObject, bool> predicate)
        {
            List<WorldObject> result = new List<WorldObject>();
            foreach (WorldObject item in source)
            {
                if (predicate(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private static bool IsHumanlike(Pawn pawn)
        {
            return pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike;
        }

        private static bool IsHumanlikeFaction(Faction faction)
        {
            return faction != null && faction.def != null && faction.def.humanlikeFaction;
        }

        private static bool IsHostileToPlayer(Faction faction)
        {
            return faction != null && Faction.OfPlayer != null && faction.HostileTo(Faction.OfPlayer);
        }

        private static bool IsHostileWorldObject(WorldObject worldObject)
        {
            return worldObject != null && IsHostileToPlayer(worldObject.Faction);
        }

        private static Faction GetPlayerFaction()
        {
            return Faction.OfPlayer;
        }

        private static object GetMember(object target, string member)
        {
            object value;
            return target != null && RimTalkArtiPublicValueReader.TryRead(target, member, out value)
                ? value
                : null;
        }

        private static object ReadFindMember(string member)
        {
            object value;
            return RimTalkArtiPublicValueReader.TryReadStatic(typeof(Find), member, out value)
                ? value
                : null;
        }

        private static object ReadStaticMember(Type type, string member)
        {
            object value;
            return RimTalkArtiPublicValueReader.TryReadStatic(type, member, out value)
                ? value
                : null;
        }

        private static object GetArgument(
            IList<object> positional,
            IDictionary<string, object> named,
            int index,
            string name,
            object defaultValue)
        {
            object value;
            if (named != null && !string.IsNullOrEmpty(name) && named.TryGetValue(name, out value))
            {
                return value;
            }

            if (positional != null && index >= 0 && index < positional.Count)
            {
                return positional[index];
            }

            return defaultValue;
        }

        private static bool TryGetInt(object value, out int result)
        {
            if (value is PlanetTile)
            {
                object tileId = GetMember(value, "tileId");
                if (tileId is int)
                {
                    result = (int)tileId;
                    return true;
                }
            }

            if (value is byte || value is sbyte || value is short || value is ushort
                || value is int || value is uint || value is long || value is ulong)
            {
                try
                {
                    result = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    return true;
                }
                catch (Exception)
                {
                }
            }

            string text = value as string;
            if (text != null && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }

            result = 0;
            return false;
        }

        private static int ToInt(object value, int defaultValue)
        {
            int result;
            return TryGetInt(value, out result) ? result : defaultValue;
        }
    }
}
