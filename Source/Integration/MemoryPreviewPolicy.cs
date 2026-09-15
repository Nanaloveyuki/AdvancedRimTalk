using System;
using System.Collections.Generic;
using AdvancedRimTalk.Arti;

namespace AdvancedRimTalk.Integration
{
    internal interface IMemoryArtiValue { }

    internal static class MemoryPreviewPolicy
    {
        private static readonly HashSet<string> Mutations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "add", "addex", "addbatch", "update", "updatecontent", "updatetag", "updateimportance",
            "remove", "delete", "removebytag", "clear", "import", "pin", "unpin", "enable", "disable",
            "move", "rundecay", "cleanup", "enforcelimits", "summarize", "archive", "setenabled",
            "setcontent", "setimportance", "setactivity", "setnotes", "addtag", "removetag",
            "addkeyword", "removekeyword", "decay"
        };

        internal static bool IsMutation(string normalizedMember) => Mutations.Contains(normalizedMember);

        internal static object Guard(bool preview, object target, string normalizedMember, object value)
        {
            return preview && target is IMemoryArtiValue && value is IArtiCallable && IsMutation(normalizedMember)
                ? new SkippedWrite(normalizedMember) : value;
        }

        private sealed class SkippedWrite : IArtiCallable
        {
            private readonly string member;
            internal SkippedWrite(string member) { this.member = member; }
            public object Invoke(IList<object> positionalArguments, IDictionary<string, object> namedArguments)
            {
                throw new InvalidOperationException("Memory write skipped during preview: " + member);
            }
        }
    }
}
