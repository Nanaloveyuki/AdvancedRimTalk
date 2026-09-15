using System;
using System.Reflection;
using AdvancedRimTalk.Integration;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class MemoryApiMethodResolverChecks
    {
        public static void Run()
        {
            Check("Update", new object[] { "text" }, typeof(string));
            Check("Update", new object[] { 42 }, typeof(int));
            Check("Nullable", new object[] { null }, typeof(string));
            Expect<MissingMethodException>(() => MemoryApiMethodResolver.Resolve(typeof(Api), "Missing", null, false));
            Expect<MissingMethodException>(() => MemoryApiMethodResolver.Resolve(typeof(Api), "Update", new object[] { new object() }, false));
            Expect<AmbiguousMatchException>(() => MemoryApiMethodResolver.Resolve(typeof(Api), "Ambiguous", new object[] { null }, false));
            MethodInfo method = MemoryApiMethodResolver.Resolve(typeof(Api), "Static", null, true);
            if (!method.IsStatic) throw new Exception("Static memory API lookup selected an instance method.");
            Expect<MissingMethodException>(() => MemoryApiMethodResolver.Resolve(typeof(Api), "Static", null, false));
        }

        private static void Check(string name, object[] args, Type parameter)
        {
            MethodInfo method = MemoryApiMethodResolver.Resolve(typeof(Api), name, args, false);
            if (method.GetParameters()[0].ParameterType != parameter)
                throw new Exception("Incorrect memory API overload.");
        }

        private static void Expect<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name);
        }

        public sealed class Api
        {
            public void Update(int value) { }
            public void Update(string value) { }
            public void Nullable(string value) { }
            public void Ambiguous(string value) { }
            public void Ambiguous(Uri value) { }
            public static void Static() { }
        }
    }
}
