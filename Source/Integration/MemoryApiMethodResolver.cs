using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace AdvancedRimTalk.Integration
{
    internal static class MemoryApiMethodResolver
    {
        internal static MethodInfo Resolve(Type type, string name, object[] arguments, bool isStatic)
        {
            if (type == null) throw new MissingMethodException(string.Empty, name);
            BindingFlags flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            MethodBase[] candidates = type.GetMethods(flags)
                .Where(method => method.Name == name && !method.ContainsGenericParameters
                    && method.GetParameters().Length == (arguments == null ? 0 : arguments.Length))
                .Cast<MethodBase>().ToArray();
            if (candidates.Length == 0) throw new MissingMethodException(type.FullName, name);

            // The binder may reorder its argument array; never mutate the caller's values.
            object[] values = arguments == null ? new object[0] : (object[])arguments.Clone();
            object state;
            return (MethodInfo)Type.DefaultBinder.BindToMethod(flags, candidates, ref values,
                null, CultureInfo.InvariantCulture, null, out state);
        }
    }
}
