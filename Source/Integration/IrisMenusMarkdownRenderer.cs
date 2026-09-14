using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal sealed class IrisMenusMarkdownRenderer
    {
        private readonly object markdown;
        private readonly MethodInfo measureMethod;
        private readonly MethodInfo drawMethod;
        private bool failureLogged;

        private IrisMenusMarkdownRenderer(
            object markdown,
            MethodInfo measureMethod,
            MethodInfo drawMethod)
        {
            this.markdown = markdown;
            this.measureMethod = measureMethod;
            this.drawMethod = drawMethod;
        }

        public bool Failed { get; private set; }

        public static IrisMenusMarkdownRenderer TryCreate(Func<string> getMarkdown)
        {
            if (getMarkdown == null)
            {
                throw new ArgumentNullException(nameof(getMarkdown));
            }

            try
            {
                Type markdownType = FindType("IrisMenus.Markdown");
                if (markdownType == null)
                {
                    return null;
                }

                MethodInfo factory = null;
                foreach (MethodInfo method in markdownType.GetMethods(
                    BindingFlags.Public | BindingFlags.Static))
                {
                    if (!string.Equals(
                            method.Name,
                            "Dynamic",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if ((parameters.Length == 1
                        && parameters[0].ParameterType == typeof(Func<string>))
                        || (parameters.Length == 2
                            && parameters[0].ParameterType == typeof(Func<string>)
                            && parameters[1].IsOptional))
                    {
                        factory = method;
                        break;
                    }
                }

                if (factory == null)
                {
                    return null;
                }

                object document = factory.Invoke(
                    null,
                    factory.GetParameters().Length == 1
                        ? new object[] { getMarkdown }
                        : new object[] { getMarkdown, null });
                if (document == null)
                {
                    return null;
                }

                Type documentType = document.GetType();
                MethodInfo measure = documentType.GetMethod(
                    "Measure",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(float) },
                    null);
                MethodInfo draw = documentType.GetMethod(
                    "Draw",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(Rect) },
                    null);
                if (measure == null || draw == null)
                {
                    return null;
                }

                return new IrisMenusMarkdownRenderer(document, measure, draw);
            }
            catch (Exception exception)
            {
                Log.Warning(
                    "[Advanced RimTalk] Could not connect to IrisMenus Markdown: "
                    + exception);
                return null;
            }
        }

        private static Type FindType(string name)
        {
            Type type = GenTypes.GetTypeInAnyAssembly(name);
            if (type != null)
            {
                return type;
            }

            type = Type.GetType(name + ", IrisMenus", false);
            if (type != null)
            {
                return type;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public float Measure(float width)
        {
            try
            {
                object value = measureMethod.Invoke(
                    markdown,
                    new object[] { Mathf.Max(1f, width) });
                return Mathf.Max(0f, Convert.ToSingle(value));
            }
            catch (Exception exception)
            {
                LogFailure(exception);
                return 0f;
            }
        }

        public void Draw(Rect rect)
        {
            try
            {
                drawMethod.Invoke(markdown, new object[] { rect });
            }
            catch (Exception exception)
            {
                LogFailure(exception);
            }
        }

        private void LogFailure(Exception exception)
        {
            if (failureLogged)
            {
                return;
            }

            failureLogged = true;
            Failed = true;
            Log.Warning(
                "[Advanced RimTalk] IrisMenus Markdown rendering failed: "
                + exception);
        }
    }
}
