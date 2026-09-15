using System;
using System.Reflection;
using AdvancedRimTalk.UI;
using UnityEngine;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal sealed class IrisMenusMarkdownRenderer
    {
        private readonly Func<string> getMarkdown;
        private readonly MethodInfo parseMethod;
        private readonly ArtiEditorIntelligence intelligence = new ArtiEditorIntelligence();
        private string cachedSource;
        private string renderedText;
        private bool failureLogged;

        private IrisMenusMarkdownRenderer(
            Func<string> getMarkdown,
            MethodInfo parseMethod)
        {
            this.getMarkdown = getMarkdown;
            this.parseMethod = parseMethod;
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

                MethodInfo parse = markdownType.GetMethod(
                    "Parse",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);
                if (parse == null)
                {
                    return null;
                }

                return new IrisMenusMarkdownRenderer(getMarkdown, parse);
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
                Refresh();
                return Mathf.Max(
                    0f,
                    Text.CalcHeight(renderedText ?? string.Empty, Mathf.Max(1f, width)));
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
                Refresh();
                Widgets.Label(rect, renderedText ?? string.Empty);
            }
            catch (Exception exception)
            {
                LogFailure(exception);
            }
        }

        private void Refresh()
        {
            string source = getMarkdown() ?? string.Empty;
            if (renderedText != null
                && string.Equals(cachedSource, source, StringComparison.Ordinal))
            {
                return;
            }

            renderedText = ArtiDocumentationMarkup.Render(
                source,
                ParseMarkdown,
                intelligence);
            cachedSource = source;
            Failed = false;
        }

        private string ParseMarkdown(string source)
        {
            object value = parseMethod.Invoke(null, new object[] { source });
            return value as string ?? Convert.ToString(value) ?? string.Empty;
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
