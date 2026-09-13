using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using AdvancedRimTalk.Arti;
using RimTalk.Prompt;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkExpandMemoryArtiBridge
    {
        internal const string PackageId = "cj.rimtalk.expandmemory";
        internal const string Alias = "memory";

        private static readonly object SyncRoot = new object();
        private static Assembly _assembly;
        private static bool _available;

        private static Type _memoryLayerType;
        private static Type _memoryTypeType;
        private static Type _memoryEntryType;
        private static Type _memoryQueryType;
        private static Type _fourLayerMemoryCompType;
        private static Type _memoryManagerType;
        private static Type _commonKnowledgeApiType;
        private static Type _commonKnowledgeEntryType;
        private static Type _keywordMatchModeType;
        private static Type _knowledgeEntryCategoryType;
        private static Type _extendedKnowledgeEntryType;
        private static Type _memoryVariableProviderType;

        public static void Detect()
        {
            EnsureDetected();
        }

        public static bool IsAvailable
        {
            get
            {
                EnsureDetected();
                return _available;
            }
        }

        public static bool IsMemoryPackage(string packageId)
        {
            return string.Equals(packageId, PackageId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(packageId, Alias, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetCanonicalPackageId(string packageId)
        {
            return string.Equals(packageId, Alias, StringComparison.OrdinalIgnoreCase)
                ? PackageId
                : packageId;
        }

        public static bool TryCreateModuleValue(
            string packageId,
            ArtiModuleInfo module,
            PromptContext context,
            out object value)
        {
            value = null;
            if (!IsMemoryPackage(packageId)
                || module == null
                || !module.Active
                || !module.ApiAvailable
                || !IsAvailable)
            {
                return false;
            }

            value = new RimTalkExpandMemoryArtiModule(context, module.Version);
            return true;
        }

        private static void EnsureDetected()
        {
            if (_available)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_available)
                {
                    return;
                }

                Assembly found = FindAssembly();
                if (found == null)
                {
                    return;
                }

                try
                {
                    _assembly = found;
                    _memoryLayerType = GetType("RimTalk.Memory.MemoryLayer");
                    _memoryTypeType = GetType("RimTalk.Memory.MemoryType");
                    _memoryEntryType = GetType("RimTalk.Memory.MemoryEntry");
                    _memoryQueryType = GetType("RimTalk.Memory.MemoryQuery");
                    _fourLayerMemoryCompType = GetType("RimTalk.Memory.FourLayerMemoryComp");
                    _memoryManagerType = GetType("RimTalk.Memory.MemoryManager");
                    _commonKnowledgeApiType = GetType("RimTalk.Memory.CommonKnowledgeAPI");
                    _commonKnowledgeEntryType = GetType("RimTalk.Memory.CommonKnowledgeEntry");
                    _keywordMatchModeType = GetType("RimTalk.Memory.KeywordMatchMode");
                    _knowledgeEntryCategoryType = GetType("RimTalk.Memory.KnowledgeEntryCategory");
                    _extendedKnowledgeEntryType = GetType("RimTalk.Memory.ExtendedKnowledgeEntry");
                    _memoryVariableProviderType = GetType("RimTalk.Memory.API.MemoryVariableProvider");
                    _available = _memoryLayerType != null
                        && _memoryTypeType != null
                        && _memoryEntryType != null
                        && _fourLayerMemoryCompType != null
                        && _memoryManagerType != null
                        && _commonKnowledgeApiType != null
                        && _commonKnowledgeEntryType != null
                        && _memoryVariableProviderType != null;
                }
                catch (Exception exception)
                {
                    _available = false;
                    if (Prefs.DevMode)
                    {
                        Log.Warning("Advanced RimTalk could not inspect RimTalk - Expand Memory: " + exception.Message);
                    }
                }
            }
        }

        private static Assembly FindAssembly()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (string.Equals(assembly.GetName().Name, "RimTalkMemoryPatch", StringComparison.OrdinalIgnoreCase)
                        || assembly.GetType("RimTalk.Memory.CommonKnowledgeAPI", false) != null)
                    {
                        return assembly;
                    }
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        private static Type GetType(string name)
        {
            return _assembly == null ? null : _assembly.GetType(name, false);
        }

        internal static object GetComponent(Pawn pawn)
        {
            EnsureDetected();
            if (!_available || pawn == null)
            {
                return null;
            }

            ThingWithComps thing = pawn as ThingWithComps;
            if (thing == null || thing.AllComps == null)
            {
                return null;
            }

            foreach (ThingComp component in thing.AllComps)
            {
                if (component != null && _fourLayerMemoryCompType.IsInstanceOfType(component))
                {
                    return component;
                }
            }

            return null;
        }

        internal static IList GetLayerList(object component, string layerName)
        {
            object layer;
            if (!TryParseLayer(layerName, out layer))
            {
                return null;
            }

            return GetLayerList(component, layer);
        }

        internal static IList GetLayerList(object component, object layer)
        {
            if (component == null || layer == null)
            {
                return null;
            }

            string propertyName;
            switch (Convert.ToString(layer, CultureInfo.InvariantCulture))
            {
                case "Active":
                    propertyName = "ActiveMemories";
                    break;
                case "Situational":
                    propertyName = "SituationalMemories";
                    break;
                case "EventLog":
                    propertyName = "EventLogMemories";
                    break;
                case "Archive":
                    propertyName = "ArchiveMemories";
                    break;
                default:
                    return null;
            }

            object value = ReadMember(component, propertyName);
            return value as IList;
        }

        internal static string GetLayerName(object entry, object component)
        {
            object layer = ReadMember(entry, "Layer");
            if (layer != null)
            {
                return Convert.ToString(layer, CultureInfo.InvariantCulture);
            }

            if (component != null)
            {
                foreach (string name in new[] { "Active", "Situational", "EventLog", "Archive" })
                {
                    IList list = GetLayerList(component, name);
                    if (ContainsReference(list, entry))
                    {
                        return name;
                    }
                }
            }

            return string.Empty;
        }

        internal static object GetMemoryValue(Pawn pawn, object id)
        {
            object component;
            object entry;
            return TryFindMemory(pawn, id, out component, out entry)
                ? new RimTalkExpandMemoryArtiMemoryValue(pawn, component, entry)
                : null;
        }

        internal static List<object> ListMemories(Pawn pawn, string layerName)
        {
            List<object> result = new List<object>();
            object component = GetComponent(pawn);
            if (component == null)
            {
                return result;
            }

            IList list = string.IsNullOrEmpty(layerName)
                ? null
                : GetLayerList(component, layerName);
            if (list == null && string.IsNullOrEmpty(layerName))
            {
                foreach (string name in new[] { "Active", "Situational", "EventLog", "Archive" })
                {
                    AddMemoryValues(result, pawn, component, GetLayerList(component, name));
                }

                return result;
            }

            AddMemoryValues(result, pawn, component, list);
            return result;
        }

        internal static int CountMemories(Pawn pawn, string layerName)
        {
            return ListMemories(pawn, layerName).Count;
        }

        private static void AddMemoryValues(List<object> target, Pawn pawn, object component, IList source)
        {
            if (source == null)
            {
                return;
            }

            foreach (object entry in source)
            {
                if (entry != null)
                {
                    target.Add(new RimTalkExpandMemoryArtiMemoryValue(pawn, component, entry));
                }
            }
        }

        internal static long AddMemoryFromArguments(
            PromptContext context,
            IList<object> positional,
            IDictionary<string, object> named,
            string defaultLayer)
        {
            Pawn pawn;
            string content;
            int contentIndex;
            if (!TryGetPawnAndContent(context, positional, named, out pawn, out content, out contentIndex))
            {
                return 0L;
            }

            string layer = ToText(GetArgument(positional, named, contentIndex + 1, "layer", defaultLayer));
            string type = ToText(GetArgument(positional, named, contentIndex + 2, "type", "conversation"));
            float importance = ToFloat(GetArgument(positional, named, contentIndex + 3, "importance", 0.5f), 0.5f);
            string relatedPawn = ToText(GetArgument(positional, named, contentIndex + 4, "related_pawn", string.Empty));
            bool pinned = ToBool(GetArgument(positional, named, contentIndex + 5, "pinned", false), false);
            string notes = ToText(GetArgument(positional, named, contentIndex + 6, "notes", string.Empty));
            return AddMemory(
                pawn,
                content,
                layer,
                type,
                importance,
                string.IsNullOrEmpty(relatedPawn) ? null : relatedPawn,
                pinned,
                notes,
                GetArgument(positional, named, contentIndex + 7, "tags", null),
                GetArgument(positional, named, contentIndex + 8, "keywords", null));
        }

        private static long AddMemory(
            Pawn pawn,
            string content,
            string layerName,
            string typeName,
            float importance,
            string relatedPawn,
            bool pinned,
            string notes,
            object tags,
            object keywords)
        {
            try
            {
                object component = GetComponent(pawn);
                object layer;
                object type;
                if (component == null
                    || string.IsNullOrWhiteSpace(content)
                    || !TryParseLayer(layerName, out layer)
                    || !TryParseMemoryType(typeName, out type))
                {
                    return 0L;
                }

                ConstructorInfo constructor = _memoryEntryType.GetConstructor(new[]
                {
                    typeof(string),
                    _memoryTypeType,
                    _memoryLayerType,
                    typeof(float),
                    typeof(string)
                });
                if (constructor == null)
                {
                    return 0L;
                }

                object entry = constructor.Invoke(new[]
                {
                    (object)content,
                    type,
                    layer,
                    (object)Math.Max(0f, Math.Min(1f, importance)),
                    relatedPawn
                });
                SetMember(entry, "IsPinned", pinned);
                if (!string.IsNullOrEmpty(notes))
                {
                    SetMember(entry, "Notes", notes);
                }

                AddStrings(entry, "AddTag", tags);
                AddStrings(entry, "AddKeyword", keywords);
                IList list = GetLayerList(component, layer);
                if (list == null)
                {
                    return 0L;
                }

                list.Insert(0, entry);
                return GetMemoryId(entry);
            }
            catch (Exception exception)
            {
                Warn("adding a memory", exception);
                return 0L;
            }
        }

        internal static bool TryGetPawnAndId(
            PromptContext context,
            IList<object> positional,
            IDictionary<string, object> named,
            out Pawn pawn,
            out object id,
            out int nextIndex)
        {
            pawn = null;
            id = null;
            nextIndex = 0;
            bool hasNamedPawn;
            object pawnArgument = GetNamed(named, "pawn", out hasNamedPawn);
            if (hasNamedPawn)
            {
                pawn = ResolvePawn(context, pawnArgument);
            }
            else if (positional != null && positional.Count > 0 && positional[0] is Pawn)
            {
                pawn = positional[0] as Pawn;
                nextIndex = 1;
            }
            else
            {
                pawn = context == null ? null : context.CurrentPawn;
            }

            bool hasNamedId;
            id = GetNamed(named, "id", out hasNamedId);
            if (!hasNamedId)
            {
                id = GetArgument(positional, named, nextIndex, "id", null);
            }

            return pawn != null && id != null;
        }

        private static bool TryGetPawnAndContent(
            PromptContext context,
            IList<object> positional,
            IDictionary<string, object> named,
            out Pawn pawn,
            out string content,
            out int contentIndex)
        {
            pawn = null;
            content = string.Empty;
            contentIndex = 0;
            bool hasNamedPawn;
            object pawnArgument = GetNamed(named, "pawn", out hasNamedPawn);
            if (hasNamedPawn)
            {
                pawn = ResolvePawn(context, pawnArgument);
            }
            else if (positional != null && positional.Count > 0 && positional[0] is Pawn)
            {
                pawn = positional[0] as Pawn;
                contentIndex = 1;
            }
            else
            {
                pawn = context == null ? null : context.CurrentPawn;
            }

            bool hasNamedContent;
            object contentArgument = GetNamed(named, "content", out hasNamedContent);
            if (hasNamedContent)
            {
                content = ToText(contentArgument);
            }
            else
            {
                content = ToText(GetArgument(positional, named, contentIndex, "content", string.Empty));
            }

            return pawn != null && !string.IsNullOrWhiteSpace(content);
        }

        internal static Pawn ResolvePawn(PromptContext context, object candidate)
        {
            if (candidate == null)
            {
                return context == null ? null : context.CurrentPawn;
            }

            Pawn pawn = candidate as Pawn;
            if (pawn != null)
            {
                return pawn;
            }

            string key = ToText(candidate);
            if (context != null && context.AllPawns != null)
            {
                foreach (Pawn item in context.AllPawns)
                {
                    if (MatchesPawn(item, key))
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        internal static bool RemoveMemory(Pawn pawn, object id)
        {
            object component;
            object entry;
            if (!TryFindMemory(pawn, id, out component, out entry))
            {
                return false;
            }

            try
            {
                object maintainer = ReadMember(component, "Maintainer");
                object result = InvokeInstance(maintainer, "Remove", entry);
                if (result is bool)
                {
                    return (bool)result;
                }
            }
            catch (Exception exception)
            {
                Warn("removing a memory", exception);
            }

            bool removed = false;
            foreach (string layer in new[] { "Active", "Situational", "EventLog", "Archive" })
            {
                IList list = GetLayerList(component, layer);
                if (list != null && list.Contains(entry))
                {
                    list.Remove(entry);
                    removed = true;
                }
            }

            return removed;
        }

        internal static bool PinMemory(Pawn pawn, object id, bool pinned)
        {
            object component;
            object entry;
            if (!TryFindMemory(pawn, id, out component, out entry))
            {
                return false;
            }

            try
            {
                object maintainer = ReadMember(component, "Maintainer");
                InvokeInstance(maintainer, "PinMemory", entry, pinned);
                return true;
            }
            catch (Exception exception)
            {
                Warn("pinning a memory", exception);
                return SetMember(entry, "IsPinned", pinned);
            }
        }

        internal static bool MoveMemory(Pawn pawn, object id, string targetLayer)
        {
            object component;
            object entry;
            object layer;
            if (!TryFindMemory(pawn, id, out component, out entry)
                || !TryParseLayer(targetLayer, out layer))
            {
                return false;
            }

            IList target = GetLayerList(component, layer);
            if (target == null)
            {
                return false;
            }

            foreach (string name in new[] { "Active", "Situational", "EventLog", "Archive" })
            {
                IList source = GetLayerList(component, name);
                if (source != null)
                {
                    source.Remove(entry);
                }
            }

            SetMember(entry, "Layer", layer);
            target.Insert(0, entry);
            return true;
        }

        internal static bool UpdateMemory(
            Pawn pawn,
            object id,
            IList<object> positional,
            IDictionary<string, object> named,
            int nextIndex)
        {
            object component;
            object entry;
            if (!TryFindMemory(pawn, id, out component, out entry))
            {
                return false;
            }

            return UpdateMemoryEntry(entry, component, positional, named, nextIndex + 1);
        }

        internal static bool UpdateMemoryEntry(
            object entry,
            object component,
            IList<object> positional,
            IDictionary<string, object> named,
            int positionalOffset)
        {
            bool changed = false;
            bool hasValue;
            object value;

            value = GetNamed(named, "content", out hasValue);
            if (!hasValue && positional != null && positional.Count > positionalOffset)
            {
                value = positional[positionalOffset];
                hasValue = true;
            }
            if (hasValue)
            {
                changed |= SetMember(entry, "Content", ToText(value));
            }

            value = GetNamed(named, "importance", out hasValue);
            if (hasValue)
            {
                changed |= SetMember(entry, "Importance", Clamp01(ToFloat(value, 0.5f)));
            }

            value = GetNamed(named, "activity", out hasValue);
            if (hasValue)
            {
                changed |= SetMember(entry, "Activity", Clamp01(ToFloat(value, 1f)));
            }

            value = GetNamed(named, "pinned", out hasValue);
            if (hasValue)
            {
                changed |= SetMember(entry, "IsPinned", ToBool(value, false));
            }

            value = GetNamed(named, "notes", out hasValue);
            if (hasValue)
            {
                changed |= SetMember(entry, "Notes", ToText(value));
            }

            value = GetNamed(named, "layer", out hasValue);
            if (hasValue && component != null)
            {
                object layer;
                if (TryParseLayer(ToText(value), out layer))
                {
                    changed |= MoveMemoryByEntry(component, entry, layer);
                }
            }

            value = GetNamed(named, "type", out hasValue);
            if (hasValue)
            {
                object type;
                if (TryParseMemoryType(ToText(value), out type))
                {
                    changed |= SetMember(entry, "Type", type);
                }
            }

            value = GetNamed(named, "tags", out hasValue);
            if (hasValue)
            {
                changed |= ReplaceStrings(entry, "tags", value);
            }

            value = GetNamed(named, "keywords", out hasValue);
            if (hasValue)
            {
                changed |= ReplaceStrings(entry, "keywords", value);
            }

            return changed;
        }

        internal static string GetPawnMemory(Pawn pawn, string member)
        {
            if (pawn == null || _memoryVariableProviderType == null)
            {
                return string.Empty;
            }

            string methodName;
            switch (RimTalkArtiNames.Normalize(member))
            {
                case "combined":
                case "memory":
                    methodName = "GetPawnMemory";
                    break;
                case "active":
                case "abm":
                    methodName = "GetPawnABM";
                    break;
                case "eventlog":
                case "els":
                    methodName = "GetPawnELS";
                    break;
                case "archive":
                case "longterm":
                case "clpa":
                    methodName = "GetPawnCLPA";
                    break;
                case "matcheventlog":
                case "matchels":
                    methodName = "GetPawnMatchELS";
                    break;
                case "matcharchive":
                case "matchclpa":
                    methodName = "GetPawnMatchCLPA";
                    break;
                default:
                    return string.Empty;
            }

            try
            {
                return ToText(InvokeStatic(_memoryVariableProviderType, methodName, pawn));
            }
            catch (Exception exception)
            {
                Warn("reading pawn memory", exception);
                return string.Empty;
            }
        }

        internal static string GetMemoryContext(Pawn pawn, int count)
        {
            object component = GetComponent(pawn);
            if (component == null)
            {
                return string.Empty;
            }

            try
            {
                return ToText(InvokeInstance(component, "GetMemoryContext", count));
            }
            catch (Exception exception)
            {
                Warn("building memory context", exception);
                return string.Empty;
            }
        }

        internal static List<object> GetRelevantMemories(Pawn pawn, int count)
        {
            object component = GetComponent(pawn);
            if (component == null)
            {
                return new List<object>();
            }

            try
            {
                object values = InvokeInstance(component, "GetRelevantMemories", count);
                return WrapEnumerable(values, pawn, component);
            }
            catch (Exception exception)
            {
                Warn("reading relevant memories", exception);
                return new List<object>();
            }
        }

        internal static List<object> RetrieveMemories(
            Pawn pawn,
            IList<object> positional,
            IDictionary<string, object> named)
        {
            List<object> result = new List<object>();
            object component = GetComponent(pawn);
            if (component == null || _memoryQueryType == null)
            {
                return result;
            }

            try
            {
                object query = Activator.CreateInstance(_memoryQueryType);
                int positionalOffset = HasNamed(named, "pawn")
                    ? 0
                    : positional != null && positional.Count > 0 && positional[0] is Pawn ? 1 : 0;
                bool hasLayer;
                bool hasType;
                SetQueryField(query, "layer", GetNamed(named, "layer", out hasLayer));
                SetQueryField(query, "type", GetNamed(named, "type", out hasType));
                SetField(query, "relatedPawn", ToText(GetArgument(positional, named, positionalOffset, "related_pawn", string.Empty)));
                SetField(query, "tags", ToStringList(GetNamed(named, "tags")));
                SetField(query, "keywords", ToStringList(GetNamed(named, "keywords")));
                SetField(query, "maxCount", ToInt(GetArgument(positional, named, positionalOffset + 1, "max_count", 10), 10));
                SetField(query, "includeContext", ToBool(GetArgument(positional, named, positionalOffset + 2, "include_context", true), true));
                object values = InvokeInstance(component, "RetrieveMemories", query);
                return WrapEnumerable(values, pawn, component);
            }
            catch (Exception exception)
            {
                Warn("retrieving memories", exception);
                return result;
            }
        }

        internal static bool RunMaintenance(Pawn pawn, string operation, IList<object> positional)
        {
            object component = GetComponent(pawn);
            if (component == null)
            {
                return false;
            }

            try
            {
                if (operation == "run_decay" || operation == "decay")
                {
                    InvokeInstance(ReadMember(component, "Maintainer"), "RunDecay");
                    return true;
                }

                if (operation == "convert_active")
                {
                    InvokeInstance(ReadMember(component, "Maintainer"), "ConvertActiveMemories");
                    return true;
                }

                if (operation == "cleanup")
                {
                    InvokeInstance(ReadMember(component, "Maintainer"), "CleanupLowActivityMemories");
                    return true;
                }

                if (operation == "enforce_limits")
                {
                    InvokeInstance(ReadMember(component, "Maintainer"), "EnforceMemoryLimits");
                    return true;
                }

                if (operation == "summarize")
                {
                    IList source = GetLayerList(component, "Active");
                    InvokeInstance(ReadMember(component, "Summarizer"), "ManualSummarize", source);
                    return true;
                }

                if (operation == "archive")
                {
                    IList source = GetLayerList(component, "EventLog");
                    InvokeInstance(ReadMember(component, "Summarizer"), "Archive", source);
                    return true;
                }
            }
            catch (Exception exception)
            {
                Warn("running memory maintenance", exception);
            }

            return false;
        }

        internal static string AddKnowledge(IList<object> positional, IDictionary<string, object> named)
        {
            try
            {
                object result = InvokeStatic(
                    _commonKnowledgeApiType,
                    "AddKnowledge",
                    ToText(GetArgument(positional, named, 0, "tag", string.Empty)),
                    ToText(GetArgument(positional, named, 1, "content", string.Empty)),
                    ToFloat(GetArgument(positional, named, 2, "importance", 0.5f), 0.5f));
                return ToText(result);
            }
            catch (Exception exception)
            {
                Warn("adding common knowledge", exception);
                return string.Empty;
            }
        }

        internal static string AddKnowledgeEx(IList<object> positional, IDictionary<string, object> named)
        {
            try
            {
                object matchMode;
                if (!TryParseEnum(_keywordMatchModeType, ToText(GetArgument(positional, named, 3, "match_mode", "any")), out matchMode))
                {
                    matchMode = Enum.Parse(_keywordMatchModeType, "Any");
                }

                object result = InvokeStatic(
                    _commonKnowledgeApiType,
                    "AddKnowledgeEx",
                    ToText(GetArgument(positional, named, 0, "tag", string.Empty)),
                    ToText(GetArgument(positional, named, 1, "content", string.Empty)),
                    ToFloat(GetArgument(positional, named, 2, "importance", 0.5f), 0.5f),
                    matchMode,
                    ToInt(GetArgument(positional, named, 4, "target_pawn_id", -1), -1),
                    ToBool(GetArgument(positional, named, 5, "can_be_extracted", false), false),
                    ToBool(GetArgument(positional, named, 6, "can_be_matched", false), false));
                return ToText(result);
            }
            catch (Exception exception)
            {
                Warn("adding extended common knowledge", exception);
                return string.Empty;
            }
        }

        internal static int AddKnowledgeBatch(IList<object> positional, IDictionary<string, object> named)
        {
            object source = GetArgument(positional, named, 0, "entries", null);
            IEnumerable enumerable = source as IEnumerable;
            if (enumerable == null || source is string)
            {
                return 0;
            }

            int count = 0;
            foreach (object item in enumerable)
            {
                IList values = item as IList;
                IDictionary dictionary = item as IDictionary;
                string tag = values != null && values.Count > 0
                    ? ToText(values[0])
                    : dictionary != null && dictionary.Contains("tag") ? ToText(dictionary["tag"]) : string.Empty;
                string content = values != null && values.Count > 1
                    ? ToText(values[1])
                    : dictionary != null && dictionary.Contains("content") ? ToText(dictionary["content"]) : ToText(item);
                string id = AddKnowledge(
                    new List<object> { tag, content, GetArgument(positional, named, 1, "importance", 0.5f) },
                    null);
                if (!string.IsNullOrEmpty(id))
                {
                    count++;
                }
            }

            return count;
        }

        internal static object GetKnowledge(string id)
        {
            try
            {
                object entry = InvokeStatic(_commonKnowledgeApiType, "FindKnowledgeById", id);
                return entry == null ? null : new RimTalkExpandMemoryArtiKnowledgeValue(entry);
            }
            catch (Exception exception)
            {
                Warn("reading common knowledge", exception);
                return null;
            }
        }

        internal static List<object> FindKnowledge(string query, string mode)
        {
            try
            {
                string method = RimTalkArtiNames.Normalize(mode) == "content"
                    ? "FindKnowledgeByContent"
                    : "FindKnowledge";
                return WrapKnowledge(InvokeStatic(_commonKnowledgeApiType, method, query));
            }
            catch (Exception exception)
            {
                Warn("searching common knowledge", exception);
                return new List<object>();
            }
        }

        internal static List<object> AllKnowledge()
        {
            try
            {
                return WrapKnowledge(InvokeStatic(_commonKnowledgeApiType, "GetAllKnowledge"));
            }
            catch (Exception exception)
            {
                Warn("reading common knowledge list", exception);
                return new List<object>();
            }
        }

        private static List<object> WrapKnowledge(object value)
        {
            List<object> result = new List<object>();
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null || value is string)
            {
                return result;
            }

            foreach (object item in enumerable)
            {
                if (item != null)
                {
                    result.Add(new RimTalkExpandMemoryArtiKnowledgeValue(item));
                }
            }

            return result;
        }

        internal static int KnowledgeCount()
        {
            return ToInt(SafeStatic("GetKnowledgeCount"), 0);
        }

        internal static bool KnowledgeExists(string id)
        {
            return ToBool(SafeStatic("ExistsKnowledge", id), false);
        }

        internal static bool UpdateKnowledge(string id, string content)
        {
            return ToBool(SafeStatic("UpdateKnowledge", id, content), false);
        }

        internal static bool UpdateKnowledgeTag(string id, string tag)
        {
            return ToBool(SafeStatic("UpdateKnowledgeTag", id, tag), false);
        }

        internal static bool UpdateKnowledgeImportance(string id, float importance)
        {
            return ToBool(SafeStatic("UpdateKnowledgeImportance", id, importance), false);
        }

        internal static bool SetKnowledgeEnabled(string id, bool enabled)
        {
            return ToBool(SafeStatic("SetKnowledgeEnabled", id, enabled), false);
        }

        internal static bool RemoveKnowledge(string id)
        {
            return ToBool(SafeStatic("RemoveKnowledge", id), false);
        }

        internal static int RemoveKnowledgeByTag(string tag)
        {
            return ToInt(SafeStatic("RemoveKnowledgeByTag", tag), 0);
        }

        internal static bool ClearKnowledge()
        {
            return ToBool(SafeStatic("ClearAllKnowledge"), false);
        }

        internal static int ImportKnowledge(string text, bool clearExisting)
        {
            return ToInt(SafeStatic("ImportFromText", text, clearExisting), 0);
        }

        internal static string ExportKnowledge()
        {
            return ToText(SafeStatic("ExportToText"));
        }

        internal static object GetKnowledgeStats()
        {
            try
            {
                object stats = InvokeStatic(_commonKnowledgeApiType, "GetStats");
                return new RimTalkExpandMemoryArtiStatsValue(stats);
            }
            catch (Exception exception)
            {
                Warn("reading common knowledge statistics", exception);
                return new RimTalkExpandMemoryArtiStatsValue(null);
            }
        }

        internal static string InjectKnowledge(IList<object> positional, IDictionary<string, object> named, PromptContext context)
        {
            try
            {
                object manager = InvokeStatic(_memoryManagerType, "GetCommonKnowledge");
                return ToText(InvokeInstance(
                    manager,
                    "InjectKnowledge",
                    ToText(GetArgument(positional, named, 0, "context", string.Empty)),
                    ToInt(GetArgument(positional, named, 1, "max_entries", 5), 5)));
            }
            catch (Exception exception)
            {
                Warn("injecting common knowledge", exception);
                return string.Empty;
            }
        }

        internal static bool UpdateKnowledgeEntry(object entry, IList<object> positional, IDictionary<string, object> named)
        {
            return UpdateKnowledgeEntry(entry, positional, named, 0);
        }

        internal static bool UpdateKnowledgeEntry(
            object entry,
            IList<object> positional,
            IDictionary<string, object> named,
            int positionalOffset)
        {
            bool changed = false;
            bool hasValue;
            object value;
            string id = ToText(ReadMember(entry, "id"));

            value = GetNamed(named, "content", out hasValue);
            if (!hasValue && positional != null && positional.Count > positionalOffset)
            {
                value = positional[positionalOffset];
                hasValue = true;
            }
            if (hasValue)
            {
                changed |= UpdateKnowledge(id, ToText(value));
            }

            value = GetNamed(named, "tag", out hasValue);
            if (!hasValue && positional != null && positional.Count > positionalOffset + 1)
            {
                value = positional[positionalOffset + 1];
                hasValue = true;
            }
            if (hasValue)
            {
                changed |= UpdateKnowledgeTag(id, ToText(value));
            }

            value = GetNamed(named, "importance", out hasValue);
            if (!hasValue && positional != null && positional.Count > positionalOffset + 2)
            {
                value = positional[positionalOffset + 2];
                hasValue = true;
            }
            if (hasValue)
            {
                changed |= UpdateKnowledgeImportance(id, ToFloat(value, 0.5f));
            }

            value = GetNamed(named, "enabled", out hasValue);
            if (!hasValue && positional != null && positional.Count > positionalOffset + 3)
            {
                value = positional[positionalOffset + 3];
                hasValue = true;
            }
            if (hasValue)
            {
                changed |= SetKnowledgeEnabled(id, ToBool(value, false));
            }

            value = GetNamed(named, "can_be_extracted", out hasValue);
            if (hasValue)
            {
                changed |= SetExtendedKnowledge(entry, "SetCanBeExtracted", ToBool(value, false));
            }

            value = GetNamed(named, "can_be_matched", out hasValue);
            if (hasValue)
            {
                changed |= SetExtendedKnowledge(entry, "SetCanBeMatched", ToBool(value, false));
            }

            return changed;
        }

        internal static bool SetExtendedKnowledge(object entry, string methodName, bool value)
        {
            try
            {
                InvokeStatic(_extendedKnowledgeEntryType, methodName, entry, value);
                return true;
            }
            catch (Exception exception)
            {
                Warn("updating extended knowledge flags", exception);
                return false;
            }
        }

        internal static bool GetExtendedKnowledge(object entry, string methodName)
        {
            try
            {
                return ToBool(InvokeStatic(_extendedKnowledgeEntryType, methodName, entry), false);
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static object GetArgument(IList<object> positional, IDictionary<string, object> named, int index, string name, object fallback)
        {
            bool found;
            object namedValue = GetNamed(named, name, out found);
            if (found)
            {
                return namedValue;
            }

            return positional != null && index >= 0 && index < positional.Count
                ? positional[index]
                : fallback;
        }

        internal static object GetNamed(IDictionary<string, object> named, string name, out bool found)
        {
            found = false;
            if (named == null)
            {
                return null;
            }

            foreach (KeyValuePair<string, object> pair in named)
            {
                if (string.Equals(RimTalkArtiNames.Normalize(pair.Key), RimTalkArtiNames.Normalize(name), StringComparison.Ordinal))
                {
                    found = true;
                    return pair.Value;
                }
            }

            return null;
        }

        internal static object GetNamed(IDictionary<string, object> named, string name)
        {
            bool ignored;
            return GetNamed(named, name, out ignored);
        }

        internal static bool HasNamed(IDictionary<string, object> named, string name)
        {
            bool found;
            GetNamed(named, name, out found);
            return found;
        }

        internal static string ToText(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string text = value as string;
            if (text != null)
            {
                return text;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        internal static int ToInt(object value, int fallback)
        {
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                int parsed;
                return int.TryParse(ToText(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                    ? parsed
                    : fallback;
            }
        }

        internal static long ToLong(object value, long fallback)
        {
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                string text = ToText(value);
                long parsed;
                if (text.StartsWith("mem-", StringComparison.OrdinalIgnoreCase)
                    && long.TryParse(text.Substring(4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }

                return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                    ? parsed
                    : fallback;
            }
        }

        internal static float ToFloat(object value, float fallback)
        {
            try
            {
                return value == null ? fallback : Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                float parsed;
                return float.TryParse(ToText(value), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                    ? parsed
                    : fallback;
            }
        }

        internal static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        internal static bool ToBool(object value, bool fallback)
        {
            if (value is bool)
            {
                return (bool)value;
            }

            bool parsed;
            return bool.TryParse(ToText(value), out parsed) ? parsed : fallback;
        }

        internal static List<string> ToStringList(object value)
        {
            List<string> result = new List<string>();
            if (value == null)
            {
                return result;
            }

            string text = value as string;
            if (text != null)
            {
                foreach (string part in text.Split(new[] { ',', ';', '，', '；' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        result.Add(part.Trim());
                    }
                }

                return result;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                foreach (object item in enumerable)
                {
                    string itemText = ToText(item);
                    if (!string.IsNullOrWhiteSpace(itemText))
                    {
                        result.Add(itemText);
                    }
                }
            }

            return result;
        }

        private static List<object> WrapEnumerable(object value, Pawn pawn, object component)
        {
            List<object> result = new List<object>();
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null || value is string)
            {
                return result;
            }

            foreach (object item in enumerable)
            {
                if (item != null)
                {
                    result.Add(new RimTalkExpandMemoryArtiMemoryValue(pawn, component, item));
                }
            }

            return result;
        }

        private static bool TryFindMemory(Pawn pawn, object id, out object component, out object entry)
        {
            component = GetComponent(pawn);
            entry = null;
            if (id is RimTalkExpandMemoryArtiMemoryValue)
            {
                id = ((RimTalkExpandMemoryArtiMemoryValue)id).Entry;
            }

            if (_memoryEntryType != null && id != null && _memoryEntryType.IsInstanceOfType(id))
            {
                long entryId = GetMemoryId(id);
                return component != null && entryId > 0L
                    && TryFindMemoryById(component, entryId, out entry);
            }

            if (component == null)
            {
                return false;
            }

            long wanted = ToLong(id, long.MinValue);
            if (wanted == long.MinValue)
            {
                return false;
            }

            return TryFindMemoryById(component, wanted, out entry);
        }

        private static bool TryFindMemoryById(object component, long wanted, out object entry)
        {
            entry = null;
            if (component == null || wanted <= 0L)
            {
                return false;
            }

            object originMatch = null;
            foreach (string layer in new[] { "Active", "Situational", "EventLog", "Archive" })
            {
                IList list = GetLayerList(component, layer);
                if (list == null)
                {
                    continue;
                }

                foreach (object candidate in list)
                {
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (GetMemoryId(candidate) == wanted)
                    {
                        entry = candidate;
                        return true;
                    }

                    if (originMatch == null && GetMemoryOriginId(candidate) == wanted)
                    {
                        originMatch = candidate;
                    }
                }
            }

            entry = originMatch;
            return entry != null;
        }

        private static bool MoveMemoryByEntry(object component, object entry, object layer)
        {
            IList target = GetLayerList(component, layer);
            if (target == null)
            {
                return false;
            }

            foreach (string name in new[] { "Active", "Situational", "EventLog", "Archive" })
            {
                IList source = GetLayerList(component, name);
                if (source != null)
                {
                    source.Remove(entry);
                }
            }

            SetMember(entry, "Layer", layer);
            target.Insert(0, entry);
            return true;
        }

        private static long GetMemoryId(object entry)
        {
            return ToLong(ReadMember(entry, "Id"), 0L);
        }

        private static long GetMemoryOriginId(object entry)
        {
            return ToLong(ReadMember(entry, "OriginId"), 0L);
        }

        private static void AddStrings(object target, string methodName, object values)
        {
            foreach (string value in ToStringList(values))
            {
                try
                {
                    InvokeInstance(target, methodName, value);
                }
                catch (Exception)
                {
                    IList list = ReadMember(target, methodName == "AddTag" ? "tags" : "keywords") as IList;
                    if (list != null && !list.Contains(value))
                    {
                        list.Add(value);
                    }
                }
            }
        }

        private static bool ReplaceStrings(object target, string fieldName, object values)
        {
            IList list = ReadMember(target, fieldName) as IList;
            if (list == null)
            {
                return false;
            }

            list.Clear();
            foreach (string value in ToStringList(values))
            {
                list.Add(value);
            }

            return true;
        }

        private static bool ContainsReference(IList list, object value)
        {
            if (list == null)
            {
                return false;
            }

            foreach (object item in list)
            {
                if (ReferenceEquals(item, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesPawn(Pawn pawn, string key)
        {
            return pawn != null
                && (string.Equals(pawn.ThingID, key, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(pawn.LabelShort, key, StringComparison.OrdinalIgnoreCase));
        }

        private static void SetQueryField(object query, string fieldName, object value)
        {
            if (value == null)
            {
                return;
            }

            string text = ToText(value);
            Type enumType = fieldName == "layer" ? _memoryLayerType : _memoryTypeType;
            object enumValue;
            if (!TryParseEnum(enumType, text, out enumValue))
            {
                return;
            }

            Type nullableType = typeof(Nullable<>).MakeGenericType(enumType);
            SetField(query, fieldName, Activator.CreateInstance(nullableType, enumValue));
        }

        private static bool TryParseLayer(string value, out object parsed)
        {
            string key = RimTalkArtiNames.Normalize(value);
            switch (key)
            {
                case "active":
                case "abm":
                    return TryParseEnum(_memoryLayerType, "Active", out parsed);
                case "situational":
                case "shortterm":
                case "scm":
                    return TryParseEnum(_memoryLayerType, "Situational", out parsed);
                case "eventlog":
                case "midterm":
                case "els":
                    return TryParseEnum(_memoryLayerType, "EventLog", out parsed);
                case "archive":
                case "longterm":
                case "clpa":
                    return TryParseEnum(_memoryLayerType, "Archive", out parsed);
                default:
                    parsed = null;
                    return false;
            }
        }

        private static bool TryParseMemoryType(string value, out object parsed)
        {
            string key = RimTalkArtiNames.Normalize(value);
            switch (key)
            {
                case "conversation": return TryParseEnum(_memoryTypeType, "Conversation", out parsed);
                case "interaction": return TryParseEnum(_memoryTypeType, "Interaction", out parsed);
                case "action": return TryParseEnum(_memoryTypeType, "Action", out parsed);
                case "summarization":
                case "summary": return TryParseEnum(_memoryTypeType, "Summarization", out parsed);
                case "event": return TryParseEnum(_memoryTypeType, "Event", out parsed);
                case "emotion": return TryParseEnum(_memoryTypeType, "Emotion", out parsed);
                case "relationship": return TryParseEnum(_memoryTypeType, "Relationship", out parsed);
                case "internal": return TryParseEnum(_memoryTypeType, "Internal", out parsed);
                default:
                    parsed = null;
                    return false;
            }
        }

        private static bool TryParseEnum(Type type, string value, out object parsed)
        {
            parsed = null;
            if (type == null || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (string name in Enum.GetNames(type))
            {
                if (string.Equals(RimTalkArtiNames.Normalize(name), RimTalkArtiNames.Normalize(value), StringComparison.Ordinal))
                {
                    parsed = Enum.Parse(type, name);
                    return true;
                }
            }

            return false;
        }

        private static object SafeStatic(string methodName, params object[] arguments)
        {
            try
            {
                return InvokeStatic(_commonKnowledgeApiType, methodName, arguments);
            }
            catch (Exception exception)
            {
                Warn("calling common knowledge API", exception);
                return null;
            }
        }

        internal static object InvokeKnowledgeEntryMethod(object entry, string methodName)
        {
            try
            {
                return InvokeInstance(entry, methodName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object InvokeStatic(Type type, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(type, methodName, arguments == null ? 0 : arguments.Length, true);
            if (method == null)
            {
                throw new MissingMethodException(type == null ? string.Empty : type.FullName, methodName);
            }

            return Invoke(method, null, arguments);
        }

        private static object InvokeInstance(object target, string methodName, params object[] arguments)
        {
            if (target == null)
            {
                throw new NullReferenceException("The memory API returned a null object.");
            }

            MethodInfo method = FindMethod(target.GetType(), methodName, arguments == null ? 0 : arguments.Length, false);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }

            return Invoke(method, target, arguments);
        }

        private static MethodInfo FindMethod(Type type, string name, int argumentCount, bool isStatic)
        {
            if (type == null)
            {
                return null;
            }

            BindingFlags flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (string.Equals(method.Name, name, StringComparison.Ordinal)
                    && method.GetParameters().Length == argumentCount)
                {
                    return method;
                }
            }

            return null;
        }

        private static object Invoke(MethodInfo method, object target, object[] arguments)
        {
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        internal static object ReadMember(object target, string name)
        {
            if (target == null)
            {
                return null;
            }

            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanRead)
            {
                return property.GetValue(target, null);
            }

            FieldInfo field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            return field == null ? null : field.GetValue(target);
        }

        private static bool SetMember(object target, string name, object value)
        {
            if (target == null)
            {
                return false;
            }

            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value, null);
                return true;
            }

            FieldInfo field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null || field.IsInitOnly)
            {
                return false;
            }

            field.SetValue(target, value);
            return true;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target == null ? null : target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && !field.IsInitOnly)
            {
                field.SetValue(target, value);
            }
        }

        private static void Warn(string operation, Exception exception)
        {
            if (Prefs.DevMode)
            {
                Log.Warning("Advanced RimTalk Expand Memory " + operation + " failed: " + exception.Message);
            }
        }
    }

    internal sealed class RimTalkExpandMemoryArtiModule : RimTalkArtiLazyNamespace
    {
        private readonly PromptContext _context;
        private readonly RimTalkExpandMemoryArtiPawn _pawn;
        private readonly RimTalkExpandMemoryArtiKnowledge _knowledge;

        public RimTalkExpandMemoryArtiModule(PromptContext context, string version)
        {
            _context = context ?? new PromptContext();
            _pawn = new RimTalkExpandMemoryArtiPawn(_context);
            _knowledge = new RimTalkExpandMemoryArtiKnowledge();

            Set("installed", true);
            Set("active", true);
            Set("available", true);
            Set("api_available", true);
            Set("package_id", RimTalkExpandMemoryArtiBridge.PackageId);
            Set("version", version ?? string.Empty);
            Set("pawn", _pawn);
            Set("common_knowledge", _knowledge);
            Set("knowledge", _knowledge);
            Set("memory", () => this);

            foreach (string layer in new[] { "active", "abm", "short_term", "situational", "scm", "event_log", "mid_term", "els", "long_term", "archive", "clpa" })
            {
                Set(layer, new RimTalkExpandMemoryArtiLayer(_context, layer));
            }
        }
    }

    internal sealed class RimTalkExpandMemoryArtiPawn : RimTalkArtiLazyNamespace
    {
        private readonly PromptContext _context;

        public RimTalkExpandMemoryArtiPawn(PromptContext context)
        {
            _context = context;
            Set("list", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.ListMemories(ResolvePawn(positional, named), string.Empty)));
            Set("all", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.ListMemories(ResolvePawn(positional, named), string.Empty)));
            Set("count", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.CountMemories(ResolvePawn(positional, named), string.Empty)));
            Set("get", new RimTalkArtiCallable(Get));
            Set("add", new RimTalkArtiCallable(Add));
            Set("update", new RimTalkArtiCallable(Update));
            Set("remove", new RimTalkArtiCallable(Remove));
            Set("delete", new RimTalkArtiCallable(Remove));
            Set("pin", new RimTalkArtiCallable((positional, named) => SetPin(positional, named, true)));
            Set("unpin", new RimTalkArtiCallable((positional, named) => SetPin(positional, named, false)));
            Set("enable", new RimTalkArtiCallable((positional, named) => SetActivity(positional, named, 1f)));
            Set("disable", new RimTalkArtiCallable((positional, named) => SetActivity(positional, named, 0f)));
            Set("move", new RimTalkArtiCallable(Move));
            Set("context", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.GetMemoryContext(ResolvePawn(positional, named), GetCount(positional, named))));
            Set("relevant", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.GetRelevantMemories(ResolvePawn(positional, named), GetCount(positional, named))));
            Set("retrieve", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RetrieveMemories(ResolvePawn(positional, named), positional, named)));
            Set("combined", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.GetPawnMemory(ResolvePawn(positional, named), "combined")));
            Set("memory", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.GetPawnMemory(ResolvePawn(positional, named), "combined")));
            Set("run_decay", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RunMaintenance(ResolvePawn(positional, named), "run_decay", positional)));
            Set("cleanup", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RunMaintenance(ResolvePawn(positional, named), "cleanup", positional)));
            Set("enforce_limits", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RunMaintenance(ResolvePawn(positional, named), "enforce_limits", positional)));
            Set("summarize", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RunMaintenance(ResolvePawn(positional, named), "summarize", positional)));
            Set("archive", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RunMaintenance(ResolvePawn(positional, named), "archive", positional)));

            foreach (string layer in new[] { "active", "abm", "short_term", "situational", "scm", "event_log", "mid_term", "els", "long_term", "archive", "clpa" })
            {
                Set(layer, new RimTalkExpandMemoryArtiLayer(_context, layer));
            }
        }

        private object Get(IList<object> positional, IDictionary<string, object> named)
        {
            Pawn pawn;
            object id;
            int next;
            return RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out next)
                ? RimTalkExpandMemoryArtiBridge.GetMemoryValue(pawn, id)
                : null;
        }

        private object Add(IList<object> positional, IDictionary<string, object> named)
        {
            return RimTalkExpandMemoryArtiBridge.AddMemoryFromArguments(_context, positional, named, "active");
        }

        private object Update(IList<object> positional, IDictionary<string, object> named)
        {
            Pawn pawn;
            object id;
            int next;
            return RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out next)
                && RimTalkExpandMemoryArtiBridge.UpdateMemory(pawn, id, positional, named, next);
        }

        private object Remove(IList<object> positional, IDictionary<string, object> named)
        {
            Pawn pawn;
            object id;
            int unused;
            return RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out unused)
                && RimTalkExpandMemoryArtiBridge.RemoveMemory(pawn, id);
        }

        private object SetPin(IList<object> positional, IDictionary<string, object> named, bool pinned)
        {
            Pawn pawn;
            object id;
            int unused;
            return RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out unused)
                && RimTalkExpandMemoryArtiBridge.PinMemory(pawn, id, pinned);
        }

        private object SetActivity(IList<object> positional, IDictionary<string, object> named, float activity)
        {
            Pawn pawn;
            object id;
            int next;
            if (!RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out next))
            {
                return false;
            }

            object entry = RimTalkExpandMemoryArtiBridge.GetMemoryValue(pawn, id);
            return entry is RimTalkExpandMemoryArtiMemoryValue
                && RimTalkExpandMemoryArtiBridge.UpdateMemoryEntry(
                    ((RimTalkExpandMemoryArtiMemoryValue)entry).Entry,
                    ((RimTalkExpandMemoryArtiMemoryValue)entry).Component,
                    new List<object>(),
                    new Dictionary<string, object> { { "activity", activity } },
                    0);
        }

        private object Move(IList<object> positional, IDictionary<string, object> named)
        {
            Pawn pawn;
            object id;
            int next;
            if (!RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out next))
            {
                return false;
            }

            return RimTalkExpandMemoryArtiBridge.MoveMemory(
                pawn,
                id,
                RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, next + 1, "layer", "active")));
        }

        private Pawn ResolvePawn(IList<object> positional, IDictionary<string, object> named)
        {
            bool found;
            object namedPawn = RimTalkExpandMemoryArtiBridge.GetNamed(named, "pawn", out found);
            if (found)
            {
                return RimTalkExpandMemoryArtiBridge.ResolvePawn(_context, namedPawn);
            }

            return positional != null && positional.Count > 0 && positional[0] is Pawn
                ? positional[0] as Pawn
                : _context.CurrentPawn;
        }

        private int GetCount(IList<object> positional, IDictionary<string, object> named)
        {
            int index = positional != null && positional.Count > 0 && positional[0] is Pawn ? 1 : 0;
            return RimTalkExpandMemoryArtiBridge.ToInt(
                RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, index, "count", 5),
                5);
        }
    }

    internal sealed class RimTalkExpandMemoryArtiLayer : RimTalkArtiLazyNamespace
    {
        private readonly PromptContext _context;
        private readonly string _layer;

        public RimTalkExpandMemoryArtiLayer(PromptContext context, string layer)
        {
            _context = context;
            _layer = layer;
            Set("name", layer);
            Set("list", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.ListMemories(ResolvePawn(positional, named), _layer)));
            Set("all", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.ListMemories(ResolvePawn(positional, named), _layer)));
            Set("count", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.CountMemories(ResolvePawn(positional, named), _layer)));
            Set("get", new RimTalkArtiCallable(Get));
            Set("add", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.AddMemoryFromArguments(_context, positional, named, _layer)));
            Set("update", new RimTalkArtiCallable(Update));
            Set("remove", new RimTalkArtiCallable(Remove));
            Set("delete", new RimTalkArtiCallable(Remove));
            Set("pin", new RimTalkArtiCallable((positional, named) => SetPin(positional, named, true)));
            Set("unpin", new RimTalkArtiCallable((positional, named) => SetPin(positional, named, false)));
            Set("enable", new RimTalkArtiCallable((positional, named) => SetActivity(positional, named, 1f)));
            Set("disable", new RimTalkArtiCallable((positional, named) => SetActivity(positional, named, 0f)));
            Set("move", new RimTalkArtiCallable((positional, named) => Move(positional, named)));
        }

        private object Get(IList<object> positional, IDictionary<string, object> named)
        {
            Pawn pawn;
            object id;
            int unused;
            return RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out unused)
                ? RimTalkExpandMemoryArtiBridge.GetMemoryValue(pawn, id)
                : null;
        }

        private object Update(IList<object> positional, IDictionary<string, object> named)
        {
            Pawn pawn;
            object id;
            int next;
            return RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out next)
                && RimTalkExpandMemoryArtiBridge.UpdateMemory(pawn, id, positional, named, next);
        }

        private object Remove(IList<object> positional, IDictionary<string, object> named)
        {
            Pawn pawn;
            object id;
            int unused;
            return RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out unused)
                && RimTalkExpandMemoryArtiBridge.RemoveMemory(pawn, id);
        }

        private object SetPin(IList<object> positional, IDictionary<string, object> named, bool pinned)
        {
            Pawn pawn;
            object id;
            int unused;
            return RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out unused)
                && RimTalkExpandMemoryArtiBridge.PinMemory(pawn, id, pinned);
        }

        private object SetActivity(IList<object> positional, IDictionary<string, object> named, float activity)
        {
            Pawn pawn;
            object id;
            int unused;
            if (!RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out unused))
            {
                return false;
            }

            object entry = RimTalkExpandMemoryArtiBridge.GetMemoryValue(pawn, id);
            return entry is RimTalkExpandMemoryArtiMemoryValue
                && RimTalkExpandMemoryArtiBridge.UpdateMemoryEntry(
                    ((RimTalkExpandMemoryArtiMemoryValue)entry).Entry,
                    ((RimTalkExpandMemoryArtiMemoryValue)entry).Component,
                    new List<object>(),
                    new Dictionary<string, object> { { "activity", activity } },
                    0);
        }

        private object Move(IList<object> positional, IDictionary<string, object> named)
        {
            Pawn pawn;
            object id;
            int next;
            if (!RimTalkExpandMemoryArtiBridge.TryGetPawnAndId(_context, positional, named, out pawn, out id, out next))
            {
                return false;
            }

            return RimTalkExpandMemoryArtiBridge.MoveMemory(
                pawn,
                id,
                RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, next + 1, "layer", _layer)));
        }

        private Pawn ResolvePawn(IList<object> positional, IDictionary<string, object> named)
        {
            bool found;
            object namedPawn = RimTalkExpandMemoryArtiBridge.GetNamed(named, "pawn", out found);
            if (found)
            {
                return RimTalkExpandMemoryArtiBridge.ResolvePawn(_context, namedPawn);
            }

            return positional != null && positional.Count > 0 && positional[0] is Pawn
                ? positional[0] as Pawn
                : _context.CurrentPawn;
        }
    }

    internal sealed class RimTalkExpandMemoryArtiKnowledge : RimTalkArtiLazyNamespace
    {
        public RimTalkExpandMemoryArtiKnowledge()
        {
            Set("add", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.AddKnowledge(positional, named)));
            Set("add_ex", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.AddKnowledgeEx(positional, named)));
            Set("add_batch", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.AddKnowledgeBatch(positional, named)));
            Set("get", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.GetKnowledge(RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "id", string.Empty)))));
            Set("find", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.FindKnowledge(RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "tag", string.Empty)), "tag")));
            Set("find_tag", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.FindKnowledge(RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "tag", string.Empty)), "tag")));
            Set("find_content", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.FindKnowledge(RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "content", string.Empty)), "content")));
            Set("all", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.AllKnowledge()));
            Set("count", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.KnowledgeCount()));
            Set("exists", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.KnowledgeExists(RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "id", string.Empty)))));
            Set("update", new RimTalkArtiCallable((positional, named) => Update(positional, named)));
            Set("update_content", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.UpdateKnowledge(Id(positional, named), RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 1, "content", string.Empty)))));
            Set("update_tag", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.UpdateKnowledgeTag(Id(positional, named), RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 1, "tag", string.Empty)))));
            Set("update_importance", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.UpdateKnowledgeImportance(Id(positional, named), RimTalkExpandMemoryArtiBridge.ToFloat(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 1, "importance", 0.5f), 0.5f))));
            Set("set_enabled", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.SetKnowledgeEnabled(Id(positional, named), RimTalkExpandMemoryArtiBridge.ToBool(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 1, "enabled", true), true))));
            Set("enable", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.SetKnowledgeEnabled(Id(positional, named), true)));
            Set("disable", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.SetKnowledgeEnabled(Id(positional, named), false)));
            Set("remove", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RemoveKnowledge(Id(positional, named))));
            Set("delete", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RemoveKnowledge(Id(positional, named))));
            Set("remove_by_tag", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RemoveKnowledgeByTag(RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "tag", string.Empty)))));
            Set("clear", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.ClearKnowledge()));
            Set("import", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.ImportKnowledge(RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "text", string.Empty)), RimTalkExpandMemoryArtiBridge.ToBool(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 1, "clear_existing", false), false))));
            Set("export", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.ExportKnowledge()));
            Set("stats", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.GetKnowledgeStats()));
            Set("inject", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.InjectKnowledge(positional, named, null)));
        }

        private object Update(IList<object> positional, IDictionary<string, object> named)
        {
            object value = RimTalkExpandMemoryArtiBridge.GetKnowledge(Id(positional, named));
            return value is RimTalkExpandMemoryArtiKnowledgeValue
                && RimTalkExpandMemoryArtiBridge.UpdateKnowledgeEntry(((RimTalkExpandMemoryArtiKnowledgeValue)value).Entry, positional, named, 1);
        }

        private string Id(IList<object> positional, IDictionary<string, object> named)
        {
            return RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "id", string.Empty));
        }
    }

    internal sealed class RimTalkExpandMemoryArtiMemoryValue : RimTalkArtiLazyNamespace
    {
        private readonly Pawn _pawn;
        private readonly object _component;

        internal RimTalkExpandMemoryArtiMemoryValue(Pawn pawn, object component, object entry)
        {
            _pawn = pawn;
            _component = component;
            Entry = entry;
            Set("id", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "Id"), false);
            Set("origin_id", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "OriginId"), false);
            Set("content", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "Content") ?? string.Empty, false);
            Set("type", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "Type"), false);
            Set("type_name", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "TypeName") ?? string.Empty, false);
            Set("layer", () => RimTalkExpandMemoryArtiBridge.GetLayerName(Entry, Component), false);
            Set("layer_name", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "LayerName") ?? string.Empty, false);
            Set("game_tick", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "GameTick"), false);
            Set("end_game_tick", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "EndGameTick"), false);
            Set("importance", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "Importance"), false);
            Set("activity", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "Activity"), false);
            Set("age", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "AgeString") ?? string.Empty, false);
            Set("related_pawn_id", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "relatedPawnId") ?? string.Empty, false);
            Set("related_pawn_name", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "relatedPawnName") ?? string.Empty, false);
            Set("location", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "location") ?? string.Empty, false);
            Set("tags", () => RimTalkExpandMemoryArtiBridge.ToStringList(RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "tags")), false);
            Set("keywords", () => RimTalkExpandMemoryArtiBridge.ToStringList(RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "keywords")), false);
            Set("is_user_edited", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "IsUserEdited"), false);
            Set("is_pinned", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "IsPinned"), false);
            Set("is_enabled", () => RimTalkExpandMemoryArtiBridge.ToFloat(RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "Activity"), 0f) > 0f, false);
            Set("notes", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "Notes") ?? string.Empty, false);
            Set("get", new RimTalkArtiCallable((positional, named) => this));
            Set("remove", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RemoveMemory(_pawn, Entry)));
            Set("delete", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RemoveMemory(_pawn, Entry)));
            Set("pin", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.PinMemory(_pawn, Entry, true)));
            Set("unpin", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.PinMemory(_pawn, Entry, false)));
            Set("enable", new RimTalkArtiCallable((positional, named) => SetActivity(1f)));
            Set("disable", new RimTalkArtiCallable((positional, named) => SetActivity(0f)));
            Set("set_content", new RimTalkArtiCallable((positional, named) => SetContent(positional, named)));
            Set("set_importance", new RimTalkArtiCallable((positional, named) => SetField("Importance", RimTalkExpandMemoryArtiBridge.ToFloat(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "importance", 0.5f), 0.5f))));
            Set("set_activity", new RimTalkArtiCallable((positional, named) => SetField("Activity", RimTalkExpandMemoryArtiBridge.Clamp01(RimTalkExpandMemoryArtiBridge.ToFloat(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "activity", 1f), 1f)))));
            Set("set_notes", new RimTalkArtiCallable((positional, named) => SetField("Notes", RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "notes", string.Empty)))));
            Set("add_tag", new RimTalkArtiCallable((positional, named) => SetTag(positional, named, "AddTag")));
            Set("remove_tag", new RimTalkArtiCallable((positional, named) => SetTag(positional, named, "RemoveTag")));
            Set("add_keyword", new RimTalkArtiCallable((positional, named) => SetTag(positional, named, "AddKeyword")));
            Set("remove_keyword", new RimTalkArtiCallable((positional, named) => SetTag(positional, named, "RemoveKeyword")));
            Set("move", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.MoveMemory(_pawn, Entry, RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "layer", "active")))));
            Set("decay", new RimTalkArtiCallable((positional, named) => SetField("Activity", RimTalkExpandMemoryArtiBridge.Clamp01(RimTalkExpandMemoryArtiBridge.ToFloat(RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "Activity"), 0f) * (1f - RimTalkExpandMemoryArtiBridge.Clamp01(RimTalkExpandMemoryArtiBridge.ToFloat(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "rate", 0f), 0f)))))));
        }

        internal object Entry { get; }
        internal object Component { get { return _component; } }

        private object SetActivity(float value)
        {
            return SetField("Activity", RimTalkExpandMemoryArtiBridge.Clamp01(value));
        }

        private object SetContent(IList<object> positional, IDictionary<string, object> named)
        {
            return SetField("Content", RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "content", string.Empty)));
        }

        private object SetTag(IList<object> positional, IDictionary<string, object> named, string methodName)
        {
            try
            {
                MethodInfo method = Entry.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                {
                    return false;
                }

                method.Invoke(Entry, new object[] { RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.GetArgument(positional, named, 0, "value", string.Empty)) });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private object SetField(string name, object value)
        {
            try
            {
                PropertyInfo property = Entry.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(Entry, value, null);
                    return true;
                }

                FieldInfo field = Entry.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && !field.IsInitOnly)
                {
                    field.SetValue(Entry, value);
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }
    }

    internal sealed class RimTalkExpandMemoryArtiKnowledgeValue : RimTalkArtiLazyNamespace
    {
        internal RimTalkExpandMemoryArtiKnowledgeValue(object entry)
        {
            Entry = entry;
            Set("id", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "id"), false);
            Set("tag", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "tag") ?? string.Empty, false);
            Set("content", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "content") ?? string.Empty, false);
            Set("importance", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "importance"), false);
            Set("keywords", () => RimTalkExpandMemoryArtiBridge.ToStringList(RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "keywords")), false);
            Set("enabled", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "isEnabled"), false);
            Set("is_enabled", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "isEnabled"), false);
            Set("is_user_edited", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "isUserEdited"), false);
            Set("target_pawn_id", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "targetPawnId"), false);
            Set("creation_tick", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "creationTick"), false);
            Set("original_event_text", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "originalEventText") ?? string.Empty, false);
            Set("match_mode", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "matchMode"), false);
            Set("category", () => RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "category"), false);
            Set("tags", () => RimTalkExpandMemoryArtiBridge.ToStringList(RimTalkExpandMemoryArtiBridge.InvokeKnowledgeEntryMethod(Entry, "GetTags")), false);
            Set("can_be_extracted", () => RimTalkExpandMemoryArtiBridge.GetExtendedKnowledge(Entry, "CanBeExtracted"), false);
            Set("can_be_matched", () => RimTalkExpandMemoryArtiBridge.GetExtendedKnowledge(Entry, "CanBeMatched"), false);
            Set("is_rule", () => RimTalkExpandMemoryArtiBridge.ToBool(RimTalkExpandMemoryArtiBridge.InvokeKnowledgeEntryMethod(Entry, "IsRuleKnowledge"), false), false);
            Set("format", () => RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.InvokeKnowledgeEntryMethod(Entry, "FormatForExport")), false);
            Set("update", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.UpdateKnowledgeEntry(Entry, positional, named)));
            Set("enable", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.SetKnowledgeEnabled(Id(), true)));
            Set("disable", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.SetKnowledgeEnabled(Id(), false)));
            Set("remove", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RemoveKnowledge(Id())));
            Set("delete", new RimTalkArtiCallable((positional, named) => RimTalkExpandMemoryArtiBridge.RemoveKnowledge(Id())));
        }

        internal object Entry { get; }

        private string Id()
        {
            return RimTalkExpandMemoryArtiBridge.ToText(RimTalkExpandMemoryArtiBridge.ReadMember(Entry, "id"));
        }
    }

    internal sealed class RimTalkExpandMemoryArtiStatsValue : RimTalkArtiLazyNamespace
    {
        internal RimTalkExpandMemoryArtiStatsValue(object stats)
        {
            Set("total_count", () => RimTalkExpandMemoryArtiBridge.ReadMember(stats, "TotalCount") ?? 0, false);
            Set("enabled_count", () => RimTalkExpandMemoryArtiBridge.ReadMember(stats, "EnabledCount") ?? 0, false);
            Set("disabled_count", () => RimTalkExpandMemoryArtiBridge.ReadMember(stats, "DisabledCount") ?? 0, false);
            Set("user_edited_count", () => RimTalkExpandMemoryArtiBridge.ReadMember(stats, "UserEditedCount") ?? 0, false);
            Set("global_count", () => RimTalkExpandMemoryArtiBridge.ReadMember(stats, "GlobalCount") ?? 0, false);
            Set("pawn_specific_count", () => RimTalkExpandMemoryArtiBridge.ReadMember(stats, "PawnSpecificCount") ?? 0, false);
        }
    }
}
