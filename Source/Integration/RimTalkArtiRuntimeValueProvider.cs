using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using AdvancedRimTalk.Arti;
using AdvancedRimTalk.Prompt;
using RimTalk;
using RimTalk.API;
using RimTalk.Data;
using RimTalk.Prompt;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;
using RimTalkCache = RimTalk.Data.Cache;
using RimTalkSettingsProvider = RimTalk.Settings;

namespace AdvancedRimTalk.Integration
{
    internal sealed class RimTalkArtiRuntimeValueProvider : IArtiRuntimeValueProvider, IArtiCoreModuleProvider
    {
        private readonly PromptContext _context;
        private readonly PromptSnapshot _snapshot;
        private readonly RimTalkArtiCoreModule _coreModule;
        private readonly Dictionary<object, Dictionary<string, RimTalkArtiCachedMember>> _memberCache =
            new Dictionary<object, Dictionary<string, RimTalkArtiCachedMember>>(
                new RimTalkArtiReferenceComparer());

        public RimTalkArtiRuntimeValueProvider(PromptContext context)
        {
            _context = context ?? new PromptContext();
            _snapshot = PromptContextSnapshotFactory.From(_context);
            _coreModule = new RimTalkArtiCoreModule(_context);
        }

        public bool TryGetCoreMember(string member, out object value)
        {
            return _coreModule.TryGetCoreMember(member, out value);
        }

        public bool TryGetGlobal(string name, out object value)
        {
            string key = Normalize(name);
            switch (key)
            {
                case "ctx":
                    value = _context;
                    return true;
                case "pawn":
                    value = _context.CurrentPawn;
                    return true;
                case "recipient":
                    value = _context.TalkRequest == null ? null : _context.TalkRequest.Recipient;
                    return true;
                case "pawns":
                    value = _context.AllPawns ?? new List<Pawn>();
                    return true;
                case "map":
                    value = _context.Map;
                    return true;
                case "settings":
                    value = GetRimTalkSettings();
                    return value != null;
                case "game":
                    value = new RimTalkArtiGameValue(_context.Map, _snapshot);
                    return true;
                case "current":
                case "world":
                case "maps":
                case "factions":
                case "settlements":
                case "sites":
                case "caravans":
                case "world_objects":
                case "mods":
                case "defs":
                case "thing":
                case "things":
                case "cell":
                case "query":
                case "find":
                    if (_coreModule.TryGetCoreMember(key, out value))
                    {
                        return true;
                    }

                    break;
                case "pawnsfinder":
                    if (_coreModule.TryGetCoreMember("pawns", out value))
                    {
                        return true;
                    }

                    break;
                case "chat":
                    value = new RimTalkArtiChatValue(_context);
                    return true;
                case "json":
                    value = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        {
                            "format",
                            Constant.GetJsonInstruction(GetRimTalkSettings() != null && GetRimTalkSettings().ApplyMoodAndSocialEffects)
                        }
                    };
                    return true;
                case "lang":
                    value = Constant.Lang;
                    return true;
                case "prompt":
                    value = _context.DialoguePrompt ?? string.Empty;
                    return true;
                case "context":
                case "pawn_context":
                    value = _context.PawnContext ?? string.Empty;
                    return true;
                case "raw_prompt":
                    value = _context.TalkRequest == null ? string.Empty : _context.TalkRequest.RawPrompt ?? string.Empty;
                    return true;
                case "time":
                    value = GetMapTime(_context.Map);
                    return true;
                case "hour":
                    value = GetHourOfDay(_context.Map);
                    return true;
                case "date":
                    value = GetMapDate(_context.Map);
                    return true;
                case "season":
                    value = GetMapSeason(_context.Map);
                    return true;
                case "weather":
                    value = GetMapWeather(_context.Map);
                    return true;
                case "temperature":
                    value = GetMapTemperature(_context.Map);
                    return true;
                case "wealth":
                    value = GetMapWealth(_context.Map);
                    return true;
                case "events":
                    value = GetMapEvents(_context.Map);
                    return true;
                case "day":
                    value = GetDayOfQuadrum(_context.Map);
                    return true;
                case "quadrum":
                    value = GetQuadrum(_context.Map);
                    return true;
                case "year":
                    value = GetYear(_context.Map);
                    return true;
            }

            string contextValue;
            if (ContextHookRegistry.TryGetContextVariable(name, _context, out contextValue))
            {
                value = contextValue;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetMember(object target, string member, out object value)
        {
            try
            {
                RimTalkArtiLazyNamespace lazyNamespace = target as RimTalkArtiLazyNamespace;
                if (lazyNamespace != null && lazyNamespace.TryGetMember(member, out value))
                {
                    return true;
                }

                PromptContext promptContext = target as PromptContext;
                if (promptContext != null)
                {
                    if (TryGetContextMember(promptContext, member, out value))
                    {
                        return true;
                    }
                }

                Pawn pawn = target as Pawn;
                if (pawn != null)
                {
                    if (TryGetPawnMember(pawn, member, out value))
                    {
                        return true;
                    }
                }

                Thing thing = target as Thing;
                if (thing != null && TryGetThingMember(thing, member, out value))
                {
                    return true;
                }

                Map map = target as Map;
                if (map != null)
                {
                    if (TryGetMapMember(map, member, out value))
                    {
                        return true;
                    }
                }

                RimTalkArtiGameValue game = target as RimTalkArtiGameValue;
                if (game != null)
                {
                    if (TryGetGameMember(game, member, out value))
                    {
                        return true;
                    }
                }

                RimTalkArtiChatValue chat = target as RimTalkArtiChatValue;
                if (chat != null)
                {
                    if (TryGetChatMember(chat, member, out value))
                    {
                        return true;
                    }
                }

                RimTalkSettings settings = target as RimTalkSettings;
                if (settings != null)
                {
                    if (TryGetSettingsMember(settings, member, out value))
                    {
                        return true;
                    }
                }

                Faction faction = target as Faction;
                if (faction != null && TryGetFactionMember(faction, member, out value))
                {
                    return true;
                }

                if (TryGetEnumerableMember(target, member, out value))
                {
                    return true;
                }

                if (TryGetReflectedMember(target, member, out value))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Log.Warning("Advanced RimTalk Arti value provider failed for '" + member + "': " + exception.Message);
            }

            value = null;
            return false;
        }

        public bool TryGetIndex(object target, object index, out object value)
        {
            RimTalkArtiLazyNamespace lazyNamespace = target as RimTalkArtiLazyNamespace;
            string key = index as string;
            if (lazyNamespace != null && key != null && lazyNamespace.TryGetMember(key, out value))
            {
                return true;
            }

            int numericIndex;
            if (TryGetInt(index, out numericIndex))
            {
                if (RimTalkArtiPublicValueReader.TryReadIndex(target, numericIndex, out value))
                {
                    return true;
                }

                if (lazyNamespace != null
                    && lazyNamespace.TryGetMember("all", out object all)
                    && RimTalkArtiPublicValueReader.TryReadIndex(all, numericIndex, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        private bool TryGetFactionMember(Faction faction, string member, out object value)
        {
            switch (RimTalkArtiNames.Normalize(member))
            {
                case "id":
                case "loadid":
                    value = faction.loadID;
                    return true;
                case "name":
                case "label":
                    value = faction.Name ?? string.Empty;
                    return true;
                case "def":
                    value = faction.def;
                    return true;
                case "isplayer":
                    value = faction.IsPlayer;
                    return true;
                case "hidden":
                    value = faction.Hidden;
                    return true;
                case "defeated":
                    value = faction.defeated;
                    return true;
                case "temporary":
                    value = faction.temporary;
                    return true;
                case "humanlike":
                    value = faction.def != null && faction.def.humanlikeFaction;
                    return true;
                case "hostile":
                case "hostiletoplayer":
                    value = Faction.OfPlayer != null && faction.HostileTo(Faction.OfPlayer);
                    return true;
                case "goodwill":
                case "goodwilltoplayer":
                    value = faction.PlayerGoodwill;
                    return true;
                case "relation":
                case "relationtoplayer":
                    value = faction.PlayerRelationKind.ToString();
                    return true;
                case "leader":
                    value = faction.leader;
                    return true;
                case "hasgoodwill":
                    value = faction.HasGoodwill;
                    return true;
                case "hostileto":
                    value = new RimTalkArtiCallable(delegate(IList<object> positional, IDictionary<string, object> named)
                    {
                        Faction other = _coreModule.FindFactionValue(GetCallableArgument(positional, named));
                        return other != null && faction.HostileTo(other);
                    });
                    return true;
                case "goodwillwith":
                case "goodwillto":
                    value = new RimTalkArtiCallable(delegate(IList<object> positional, IDictionary<string, object> named)
                    {
                        Faction other = _coreModule.FindFactionValue(GetCallableArgument(positional, named));
                        return other == null ? 0 : faction.GoodwillWith(other);
                    });
                    return true;
                case "relationkindwith":
                case "relationwith":
                    value = new RimTalkArtiCallable(delegate(IList<object> positional, IDictionary<string, object> named)
                    {
                        Faction other = _coreModule.FindFactionValue(GetCallableArgument(positional, named));
                        return other == null ? string.Empty : faction.RelationKindWith(other).ToString();
                    });
                    return true;
            }

            value = null;
            return false;
        }

        private bool TryGetThingMember(Thing thing, string member, out object value)
        {
            switch (RimTalkArtiNames.Normalize(member))
            {
                case "inspectstring":
                    value = new RimTalkArtiCallable(delegate
                    {
                        return thing.GetInspectString();
                    });
                    return true;
                case "hostileto":
                    value = new RimTalkArtiCallable(delegate(IList<object> positional, IDictionary<string, object> named)
                    {
                        object other = GetCallableArgument(positional, named);
                        Thing otherThing = other as Thing;
                        if (otherThing != null)
                        {
                            return thing.HostileTo(otherThing);
                        }

                        Faction otherFaction = _coreModule.FindFactionValue(other);
                        return otherFaction != null && thing.HostileTo(otherFaction);
                    });
                    return true;
            }

            value = null;
            return false;
        }

        private bool TryGetEnumerableMember(object target, string member, out object value)
        {
            value = null;
            if (target == null || target is string || !(target is IEnumerable))
            {
                return false;
            }

            string key = RimTalkArtiNames.Normalize(member);
            if (key != "count" && key != "size" && key != "length"
                && key != "first" && key != "last")
            {
                return false;
            }

            if (ShouldCacheMember(target, key))
            {
                Dictionary<string, RimTalkArtiCachedMember> cache;
                RimTalkArtiCachedMember cached;
                if (_memberCache.TryGetValue(target, out cache)
                    && cache.TryGetValue(key, out cached))
                {
                    value = cached.Value;
                    return cached.Found;
                }

                bool found = ReadEnumerableMember(target, key, out value);
                if (cache == null)
                {
                    cache = new Dictionary<string, RimTalkArtiCachedMember>(StringComparer.Ordinal);
                    _memberCache[target] = cache;
                }

                cache[key] = new RimTalkArtiCachedMember(found, value);
                return found;
            }

            return ReadEnumerableMember(target, key, out value);
        }

        private static bool ReadEnumerableMember(object target, string key, out object value)
        {
            value = null;
            ICollection collection = target as ICollection;
            if (key == "count" || key == "size" || key == "length")
            {
                if (collection != null)
                {
                    value = collection.Count;
                    return true;
                }

                int count = 0;
                try
                {
                    foreach (object unused in (IEnumerable)target)
                    {
                        count++;
                    }
                }
                catch (Exception)
                {
                    return false;
                }

                value = count;
                return true;
            }

            object first = null;
            object last = null;
            bool hasValue = false;
            try
            {
                foreach (object item in (IEnumerable)target)
                {
                    if (!hasValue)
                    {
                        first = item;
                        hasValue = true;
                    }

                    last = item;
                }
            }
            catch (Exception)
            {
                return false;
            }

            value = key == "first" ? first : last;
            return true;
        }

        private bool TryGetReflectedMember(object target, string member, out object value)
        {
            value = null;
            if (target == null)
            {
                return false;
            }

            string key = RimTalkArtiNames.Normalize(member);
            if (ShouldCacheMember(target, key))
            {
                Dictionary<string, RimTalkArtiCachedMember> cache;
                RimTalkArtiCachedMember cached;
                if (_memberCache.TryGetValue(target, out cache)
                    && cache.TryGetValue(key, out cached))
                {
                    value = cached.Value;
                    return cached.Found;
                }

                bool found = RimTalkArtiPublicValueReader.TryRead(target, key, out value);
                if (found)
                {
                    value = SnapshotEnumerable(value);
                }
                if (cache == null)
                {
                    cache = new Dictionary<string, RimTalkArtiCachedMember>(StringComparer.Ordinal);
                    _memberCache[target] = cache;
                }

                cache[key] = new RimTalkArtiCachedMember(found, value);
                return found;
            }

            bool foundWithoutCache = RimTalkArtiPublicValueReader.TryRead(target, key, out value);
            if (foundWithoutCache)
            {
                value = SnapshotEnumerable(value);
            }

            return foundWithoutCache;
        }

        private static object SnapshotEnumerable(object value)
        {
            if (value == null || value is string || value is IDictionary || value is IList || value is Array)
            {
                return value;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                return value;
            }

            List<object> snapshot = new List<object>();
            try
            {
                foreach (object item in enumerable)
                {
                    snapshot.Add(item);
                }

                return snapshot;
            }
            catch (Exception)
            {
                return value;
            }
        }

        private static bool ShouldCacheMember(object target, string key)
        {
            if (target == null)
            {
                return false;
            }

            string typeName = target.GetType().Name;
            if (typeName.IndexOf("Tracker", StringComparison.OrdinalIgnoreCase) >= 0
                || typeName.StartsWith("Need", StringComparison.OrdinalIgnoreCase)
                || (typeName.StartsWith("Hediff", StringComparison.OrdinalIgnoreCase)
                    && !typeName.EndsWith("Def", StringComparison.OrdinalIgnoreCase))
                || string.Equals(typeName, "HediffSet", StringComparison.Ordinal)
                || string.Equals(typeName, "MapTemperature", StringComparison.Ordinal)
                || string.Equals(typeName, "WeatherManager", StringComparison.Ordinal)
                || string.Equals(typeName, "WealthWatcher", StringComparison.Ordinal)
                || string.Equals(typeName, "TickManager", StringComparison.Ordinal))
            {
                return false;
            }

            return key != "tick"
                && key != "ticks"
                && key != "ticksabs"
                && key != "ticksgame"
                && key != "temperature"
                && key != "outdoortemp"
                && key != "maptemperature"
                && key != "weather"
                && key != "position"
                && key != "positionheld"
                && key != "map"
                && key != "faction"
                && key != "dead"
                && key != "downed"
                && key != "drafted"
                && key != "spawned"
                && key != "destroyed"
                && key != "hitpoints"
                && key != "stackcount"
                && key != "curjob"
                && key != "currentjob"
                && key != "mood"
                && key != "age"
                && key != "needs"
                && key != "playergoodwill"
                && key != "playerrelationkind"
                && key != "wealth"
                && key != "currentmap"
                && key != "currentmapindex";
        }

        private static bool TryGetInt(object value, out int result)
        {
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

        private static object GetCallableArgument(
            IList<object> positional,
            IDictionary<string, object> named)
        {
            if (named != null)
            {
                foreach (KeyValuePair<string, object> pair in named)
                {
                    string key = RimTalkArtiNames.Normalize(pair.Key);
                    if (key == "other" || key == "faction" || key == "target" || key == "id")
                    {
                        return pair.Value;
                    }
                }
            }

            return positional == null || positional.Count == 0 ? null : positional[0];
        }

        private bool TryGetContextMember(PromptContext context, string member, out object value)
        {
            switch (Normalize(member))
            {
                case "pawn":
                case "current_pawn":
                    value = context.CurrentPawn;
                    return true;
                case "recipient":
                    value = context.TalkRequest == null ? null : context.TalkRequest.Recipient;
                    return true;
                case "pawns":
                case "all_pawns":
                    value = context.AllPawns ?? new List<Pawn>();
                    return true;
                case "map":
                    value = context.Map;
                    return true;
                case "talk_type":
                case "talktype":
                    value = context.TalkType.ToString();
                    return true;
                case "dialogue_type":
                case "dialoguetype":
                    value = context.DialogueType ?? string.Empty;
                    return true;
                case "intent":
                    value = context.Intent ?? string.Empty;
                    return true;
                case "topic":
                case "conversation_topic":
                case "conversationtopic":
                    value = context.ConversationTopic ?? string.Empty;
                    return true;
                case "status":
                case "dialogue_status":
                case "dialoguestatus":
                    value = context.DialogueStatus ?? string.Empty;
                    return true;
                case "prompt":
                case "dialogue_prompt":
                case "dialogueprompt":
                    value = context.DialoguePrompt ?? string.Empty;
                    return true;
                case "user_prompt":
                case "userprompt":
                    value = context.UserPrompt ?? string.Empty;
                    return true;
                case "context":
                case "pawn_context":
                case "pawncontext":
                    value = context.PawnContext ?? string.Empty;
                    return true;
                case "chat":
                case "chat_history":
                case "chathistory":
                case "history":
                    value = new RimTalkArtiChatValue(context);
                    return true;
                case "is_monologue":
                case "ismonologue":
                    value = context.IsMonologue;
                    return true;
                case "pawn_count":
                case "pawncount":
                    value = context.AllPawns == null ? 0 : context.AllPawns.Count;
                    return true;
                case "map_id":
                case "mapid":
                    value = context.Map == null ? 0 : context.Map.uniqueID;
                    return true;
            }

            string contextValue;
            if (ContextHookRegistry.TryGetContextVariable(member, context, out contextValue))
            {
                value = contextValue;
                return true;
            }

            value = null;
            return false;
        }

        private bool TryGetPawnMember(Pawn pawn, string member, out object value)
        {
            string customValue;
            if (ContextHookRegistry.TryGetPawnVariable(member, pawn, out customValue))
            {
                value = customValue;
                return true;
            }

            switch (Normalize(member))
            {
                case "name":
                case "label":
                case "label_short":
                case "labelshort":
                    value = pawn.LabelShort ?? string.Empty;
                    return true;
                case "name_raw":
                    value = pawn.Name;
                    return true;
                case "faction":
                    value = pawn.Faction == null ? string.Empty : pawn.Faction.Name ?? string.Empty;
                    return true;
                case "faction_object":
                case "faction_raw":
                    value = pawn.Faction;
                    return true;
                case "id":
                case "thing_id_number":
                    value = pawn.thingIDNumber;
                    return true;
                case "thing_id":
                    value = pawn.ThingID;
                    return true;
                case "info":
                    value = RimTalkArtiPawnInfo.Create(pawn);
                    return true;
                case "def":
                case "race_def":
                    value = pawn.def;
                    return true;
                case "kind_def":
                    value = pawn.kindDef;
                    return true;
                case "map":
                    value = pawn.Map;
                    return true;
                case "position":
                    value = pawn.Position;
                    return true;
                case "position_held":
                    value = pawn.PositionHeld;
                    return true;
                case "health_tracker":
                case "health_raw":
                    value = pawn.health;
                    return true;
                case "needs_tracker":
                    value = pawn.needs;
                    return true;
                case "story_tracker":
                    value = pawn.story;
                    return true;
                case "relations_tracker":
                    value = pawn.relations;
                    return true;
                case "interaction_tracker":
                    value = pawn.interactions;
                    return true;
                case "equipment_tracker":
                    value = pawn.equipment;
                    return true;
                case "inventory_tracker":
                    value = pawn.inventory;
                    return true;
                case "ideology_tracker":
                case "ideo_tracker":
                    value = pawn.ideo;
                    return true;
                case "gene_tracker":
                case "genes_raw":
                    value = pawn.genes;
                    return true;
                case "race":
                    if (ModsConfig.BiotechActive && pawn.genes != null && pawn.genes.Xenotype != null)
                    {
                        value = pawn.genes.XenotypeLabel ?? string.Empty;
                    }
                    else
                    {
                        value = pawn.def == null ? string.Empty : pawn.def.LabelCap.RawText;
                    }

                    return true;
                case "gender":
                    value = pawn.gender.ToString();
                    return true;
                case "age":
                    value = pawn.ageTracker == null ? 0 : pawn.ageTracker.AgeBiologicalYears;
                    return true;
                case "title":
                    value = pawn.GetTitle() ?? string.Empty;
                    return true;
                case "job":
                case "activity":
                    value = GetPawnActivity(pawn);
                    return true;
                case "mood":
                    value = pawn.needs == null || pawn.needs.mood == null
                        ? string.Empty
                        : pawn.needs.mood.MoodString ?? string.Empty;
                    return true;
                case "health":
                    value = ContextBuilder.GetHealthContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "traits":
                    value = ContextBuilder.GetTraitsContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "skills":
                    value = ContextBuilder.GetSkillsContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "thoughts":
                case "fullthought":
                    value = ContextBuilder.GetAllThoughtsContext(pawn) ?? string.Empty;
                    return true;
                case "relations":
                    value = ContextBuilder.GetRelationsContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "social":
                    value = RelationsService.GetRelationsString(pawn) ?? string.Empty;
                    return true;
                case "fullsocial":
                    value = RelationsService.GetAllSocialString(pawn) ?? string.Empty;
                    return true;
                case "fullrelation":
                    value = RelationsService.GetAllRelationsString(pawn) ?? string.Empty;
                    return true;
                case "fullinteraction":
                    value = RelationsService.GetAllInteractionString(pawn) ?? string.Empty;
                    return true;
                case "equipment":
                    value = ContextBuilder.GetEquipmentContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "ideology":
                    value = ContextBuilder.GetIdeologyContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "genes":
                case "notable_genes":
                    value = ContextBuilder.GetNotableGenesContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "backstory":
                    value = ContextBuilder.GetBackstoryContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "profile":
                case "context":
                    value = GetPawnStateContext(pawn);
                    return true;
                case "captive_status":
                    value = ContextBuilder.GetPrisonerSlaveContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty;
                    return true;
                case "location":
                    value = RimTalkArtiPawnPromptData.GetLocation(pawn);
                    return true;
                case "beauty":
                    value = RimTalkArtiPawnPromptData.GetBeauty(pawn);
                    return true;
                case "cleanliness":
                    value = RimTalkArtiPawnPromptData.GetCleanliness(pawn);
                    return true;
                case "terrain":
                    value = RimTalkArtiPawnPromptData.GetTerrain(pawn);
                    return true;
                case "surroundings":
                    value = RimTalkArtiPawnPromptData.GetNearbyThingsText(pawn);
                    return true;
                case "nearby_things":
                case "nearby_things_text":
                    value = RimTalkArtiPawnPromptData.GetNearbyThingsText(pawn);
                    return true;
                case "nearby_things_raw":
                    value = RimTalkArtiPawnPromptData.GetNearbyThings(pawn);
                    return true;
                case "nearby_items":
                    value = RimTalkArtiPawnPromptData.GetNearbyItems(pawn);
                    return true;
                case "nearby_buildings":
                    value = RimTalkArtiPawnPromptData.GetNearbyBuildings(pawn);
                    return true;
                case "nearby_plants":
                    value = RimTalkArtiPawnPromptData.GetNearbyPlants(pawn);
                    return true;
                case "nearby_animals":
                    value = RimTalkArtiPawnPromptData.GetNearbyAnimals(pawn);
                    return true;
                case "nearby_filth":
                    value = RimTalkArtiPawnPromptData.GetNearbyFilth(pawn);
                    return true;
            }

            value = null;
            return false;
        }

        private bool TryGetMapMember(Map map, string member, out object value)
        {
            string customValue;
            if (ContextHookRegistry.TryGetEnvironmentVariable(member, map, out customValue))
            {
                value = customValue;
                return true;
            }

            switch (Normalize(member))
            {
                case "time":
                    value = GetMapTime(map);
                    return true;
                case "hour":
                    value = GetHourOfDay(map);
                    return true;
                case "date":
                    value = GetMapDate(map);
                    return true;
                case "day":
                    value = GetDayOfQuadrum(map);
                    return true;
                case "quadrum":
                    value = GetQuadrum(map);
                    return true;
                case "year":
                    value = GetYear(map);
                    return true;
                case "season":
                    value = GetMapSeason(map);
                    return true;
                case "weather":
                    value = GetMapWeather(map);
                    return true;
                case "temperature":
                    value = GetMapTemperature(map);
                    return true;
                case "wealth":
                    value = GetMapWealth(map);
                    return true;
                case "events":
                    value = GetMapEvents(map);
                    return true;
                case "events_raw":
                    value = map == null ? null : map.events;
                    return true;
                case "map_id":
                case "mapid":
                case "unique_id":
                case "uniqueid":
                case "id":
                    value = map == null ? 0 : map.uniqueID;
                    return true;
                case "index":
                    value = map == null ? -1 : map.Index;
                    return true;
                case "tile":
                    value = map == null ? 0 : map.Tile;
                    return true;
                case "tile_id":
                    value = map == null ? 0 : map.Tile.tileId;
                    return true;
                case "name":
                    value = map == null || map.Parent == null ? string.Empty : map.Parent.Label ?? string.Empty;
                    return true;
                case "pawns":
                    value = map == null || map.mapPawns == null
                        ? new List<Pawn>()
                        : map.mapPawns.AllPawns;
                    return true;
                case "things":
                    value = map == null || map.listerThings == null
                        ? new List<Thing>()
                        : map.listerThings.AllThings;
                    return true;
                case "buildings":
                    value = GetAllBuildings(map);
                    return true;
                case "raw_events":
                    value = map == null ? null : map.events;
                    return true;
            }

            value = null;
            return false;
        }

        private static List<Building> GetAllBuildings(Map map)
        {
            List<Building> result = new List<Building>();
            if (map == null || map.listerThings == null || map.listerThings.AllThings == null)
            {
                return result;
            }

            foreach (Thing thing in map.listerThings.AllThings)
            {
                Building building = thing as Building;
                if (building != null)
                {
                    result.Add(building);
                }
            }

            return result;
        }

        private bool TryGetGameMember(RimTalkArtiGameValue game, string member, out object value)
        {
            switch (Normalize(member))
            {
                case "map":
                    value = game.Map;
                    return true;
                case "raw":
                    value = Current.Game;
                    return true;
                case "snapshot":
                    value = game.Snapshot;
                    return true;
            }

            return TryGetMapMember(game.Map, member, out value);
        }

        private bool TryGetChatMember(RimTalkArtiChatValue chat, string member, out object value)
        {
            string key = Normalize(member);
            if (key == "history" || key == "chat_history" || key == "chathistory")
            {
                value = FormatHistory(chat.Context.ChatHistory);
                return true;
            }

            if (key == "history_simplified" || key == "historysimplified")
            {
                value = FormatHistory(chat.Context.GetChatHistory(true));
                return true;
            }

            if (key == "count" || key == "size")
            {
                value = chat.Context.ChatHistory == null ? 0 : chat.Context.ChatHistory.Count;
                return true;
            }

            if (key == "last")
            {
                IList<ValueTuple<Role, string>> history = chat.Context.ChatHistory;
                value = history == null || history.Count == 0 ? string.Empty : history[history.Count - 1].Item2 ?? string.Empty;
                return true;
            }

            value = null;
            return false;
        }

        private bool TryGetSettingsMember(RimTalkSettings settings, string member, out object value)
        {
            switch (Normalize(member))
            {
                case "apply_mood_and_social_effects":
                case "applymoodandsocialeffects":
                    value = settings.ApplyMoodAndSocialEffects;
                    return true;
                case "use_advanced_prompt_mode":
                case "useadvancedpromptmode":
                    value = settings.UseAdvancedPromptMode;
                    return true;
                case "player_persona":
                case "playerpersona":
                    value = settings.PlayerPersona ?? string.Empty;
                    return true;
                case "simple_mode_instruction":
                case "simplemodeinstruction":
                    value = settings.SimpleModeInstruction ?? string.Empty;
                    return true;
                case "player_dialogue_mode":
                case "playerdialoguemode":
                    value = settings.PlayerDialogueMode.ToString();
                    return true;
                case "context":
                    value = settings.Context;
                    return true;
            }

            value = null;
            return false;
        }

        private RimTalkSettings GetRimTalkSettings()
        {
            try
            {
                return RimTalkSettingsProvider.Get();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetPawnStateContext(Pawn pawn)
        {
            PawnState state = RimTalkCache.Get(pawn);
            return state == null ? string.Empty : state.Context ?? string.Empty;
        }

        private static string GetPawnActivity(Pawn pawn)
        {
            if (pawn == null)
            {
                return string.Empty;
            }

            if (pawn.InMentalState)
            {
                return pawn.MentalState == null ? string.Empty : pawn.MentalState.InspectLine ?? string.Empty;
            }

            if (pawn.CurJobDef == null)
            {
                return string.Empty;
            }

            string activity = pawn.jobs == null || pawn.jobs.curDriver == null
                ? string.Empty
                : pawn.jobs.curDriver.GetReport();
            return Describer.StripConditionSuffix(activity) ?? string.Empty;
        }

        private static int GetHourOfDay(Map map)
        {
            long ticks = Find.TickManager == null ? 0L : Find.TickManager.TicksAbs;
            Vector2 longLat = GetLongLat(map);
            return GenDate.HourOfDay(ticks, longLat.x);
        }

        private static int GetDayOfQuadrum(Map map)
        {
            long ticks = Find.TickManager == null ? 0L : Find.TickManager.TicksAbs;
            Vector2 longLat = GetLongLat(map);
            return GenDate.DayOfQuadrum(ticks, longLat.x) + 1;
        }

        private static string GetQuadrum(Map map)
        {
            long ticks = Find.TickManager == null ? 0L : Find.TickManager.TicksAbs;
            Vector2 longLat = GetLongLat(map);
            return QuadrumUtility.Label(GenDate.Quadrum(ticks, longLat.x));
        }

        private static int GetYear(Map map)
        {
            long ticks = Find.TickManager == null ? 0L : Find.TickManager.TicksAbs;
            Vector2 longLat = GetLongLat(map);
            return GenDate.Year(ticks, longLat.x);
        }

        private static Vector2 GetLongLat(Map map)
        {
            return map != null && Find.WorldGrid != null
                ? Find.WorldGrid.LongLatOf(map.Tile)
                : Vector2.zero;
        }

        private static long GetAbsoluteTicks()
        {
            return Find.TickManager == null ? 0L : Find.TickManager.TicksAbs;
        }

        private string GetMapTime(Map map)
        {
            string value = _snapshot.Hour;
            if (map != null && Find.WorldGrid != null)
            {
                value = CommonUtil.GetInGameHour12HString(GetAbsoluteTicks(), GetLongLat(map));
            }

            return ApplyEnvironmentHook(map, ContextCategories.Environment.Time, value);
        }

        private string GetMapDate(Map map)
        {
            string value = _snapshot.Date;
            if (map != null && Find.WorldGrid != null)
            {
                value = GenDate.DateFullStringAt(GetAbsoluteTicks(), GetLongLat(map));
            }

            return ApplyEnvironmentHook(map, ContextCategories.Environment.Date, value);
        }

        private string GetMapSeason(Map map)
        {
            string value = _snapshot.Season;
            if (map != null && Find.WorldGrid != null)
            {
                Vector2 longLat = GetLongLat(map);
                value = SeasonUtility.Label(GenDate.Season(GetAbsoluteTicks(), longLat));
            }

            return ApplyEnvironmentHook(map, ContextCategories.Environment.Season, value);
        }

        private static string GetMapWeather(Map map)
        {
            string value = string.Empty;
            if (map != null && map.weatherManager != null && map.weatherManager.curWeather != null)
            {
                value = map.weatherManager.curWeather.label ?? string.Empty;
            }

            return ApplyEnvironmentHook(map, ContextCategories.Environment.Weather, value);
        }

        private static string GetMapTemperature(Map map)
        {
            string value = string.Empty;
            if (map != null && map.mapTemperature != null)
            {
                value = Mathf.RoundToInt(map.mapTemperature.OutdoorTemp)
                    .ToString(CultureInfo.InvariantCulture);
            }

            return ApplyEnvironmentHook(map, ContextCategories.Environment.Temperature, value);
        }

        private static string GetMapWealth(Map map)
        {
            string value = string.Empty;
            if (map != null && map.wealthWatcher != null)
            {
                value = Describer.Wealth(map.wealthWatcher.WealthTotal) ?? string.Empty;
            }

            return ApplyEnvironmentHook(map, ContextCategories.Environment.Wealth, value);
        }

        private static string GetMapEvents(Map map)
        {
            string value = map == null
                ? string.Empty
                : ContextBuilder.GetEventsContext(map, PromptService.InfoLevel.Normal) ?? string.Empty;
            return ApplyEnvironmentHook(map, ContextCategories.Environment.Events, value);
        }

        private static string ApplyEnvironmentHook(Map map, ContextCategory category, string value)
        {
            return map == null
                ? value ?? string.Empty
                : ContextHookRegistry.ApplyEnvironmentHooks(category, map, value ?? string.Empty);
        }

        private static string FormatHistory(IList<ValueTuple<Role, string>> history)
        {
            if (history == null || history.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Conversation history (reference only; do not repeat or continue):");
            for (int index = 0; index < history.Count; index++)
            {
                string message = (history[index].Item2 ?? string.Empty)
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ");
                builder.Append("- ")
                    .Append(index + 1)
                    .Append(" | role=")
                    .Append(history[index].Item1)
                    .Append(" | text=")
                    .AppendLine(message);
            }

            return builder.ToString().TrimEnd();
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }
    }

    internal sealed class RimTalkArtiGameValue
    {
        public RimTalkArtiGameValue(Map map, PromptSnapshot snapshot)
        {
            Map = map;
            Snapshot = snapshot;
        }

        public Map Map { get; }
        public PromptSnapshot Snapshot { get; }
    }

    internal sealed class RimTalkArtiChatValue
    {
        public RimTalkArtiChatValue(PromptContext context)
        {
            Context = context;
        }

        public PromptContext Context { get; }
    }
}
