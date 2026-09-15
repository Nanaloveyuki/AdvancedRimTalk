using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Verse;

namespace AdvancedRimTalk.Documentation
{
    internal sealed class AdvancedRimTalkDocumentationCatalog
    {
        private const string ResourceRoot = "docs.Arti.zh_cn.";
        private static readonly Regex MarkdownLink = new Regex(
            @"\[[^\]]+\]\(([^)\s]+)",
            RegexOptions.Compiled);

        private static readonly CategoryDefinition[] Definitions =
        {
            new CategoryDefinition(
                "guide",
                "guide/index.md",
                "guide/",
                "AdvancedRimTalk.Documentation.Guide"),
            new CategoryDefinition(
                "language",
                "language/index.md",
                "language/",
                "AdvancedRimTalk.Documentation.Language"),
            new CategoryDefinition(
                "global",
                "global/index.md",
                "global/",
                "AdvancedRimTalk.Documentation.Global"),
            new CategoryDefinition(
                "core",
                "core/index.md",
                "core/",
                "AdvancedRimTalk.Documentation.Core"),
            new CategoryDefinition(
                "core.string",
                "core/string.md",
                "core/string/",
                "AdvancedRimTalk.Documentation.CoreString"),
            new CategoryDefinition(
                "data",
                "data/index.md",
                "data/",
                "AdvancedRimTalk.Documentation.Data"),
            new CategoryDefinition(
                "memory",
                "memory/index.md",
                "memory/",
                "AdvancedRimTalk.Documentation.Memory"),
            new CategoryDefinition(
                "compatibility",
                "compatibility/index.md",
                "compatibility/",
                "AdvancedRimTalk.Documentation.Compatibility")
        };

        private AdvancedRimTalkDocumentationCatalog(
            DocumentationEntry overview,
            List<DocumentationCategory> categories)
        {
            Overview = overview;
            Categories = categories;
            entriesByPath = new Dictionary<string, DocumentationEntry>(
                StringComparer.OrdinalIgnoreCase);
            if (overview != null)
            {
                entriesByPath[overview.RelativePath] = overview;
            }

            foreach (DocumentationCategory category in categories)
            {
                foreach (DocumentationEntry entry in category.Documents)
                {
                    entriesByPath[entry.RelativePath] = entry;
                }
            }
        }

        private readonly Dictionary<string, DocumentationEntry> entriesByPath;

        public DocumentationEntry Overview { get; }

        public List<DocumentationCategory> Categories { get; }

        public bool IsAvailable
        {
            get { return Overview != null && Categories.Count > 0; }
        }

        public static AdvancedRimTalkDocumentationCatalog Create(string contentRoot)
        {
            MarkdownStore store = new MarkdownStore(
                typeof(AdvancedRimTalkDocumentationCatalog).Assembly,
                contentRoot);
            string overviewMarkdown;
            if (!store.TryRead("index.md", out overviewMarkdown))
            {
                Log.Warning(
                    "[Advanced RimTalk] Embedded Arti documentation index is unavailable.");
                return new AdvancedRimTalkDocumentationCatalog(
                    null,
                    new List<DocumentationCategory>());
            }

            DocumentationEntry overview = new DocumentationEntry(
                "index.md",
                ReadTitle(overviewMarkdown, "Arti"),
                overviewMarkdown);
            var categories = new List<DocumentationCategory>();
            var indexPaths = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            foreach (CategoryDefinition definition in Definitions)
            {
                indexPaths.Add(definition.IndexPath);
            }

            foreach (CategoryDefinition definition in Definitions)
            {
                string categoryIndexMarkdown;
                if (!store.TryRead(definition.IndexPath, out categoryIndexMarkdown))
                {
                    Log.Warning(
                        "[Advanced RimTalk] Documentation category is unavailable: "
                        + definition.IndexPath);
                    continue;
                }

                var category = new DocumentationCategory(
                    definition.Id,
                    definition.TitleKey);
                AddDocument(
                    category,
                    definition.IndexPath,
                    categoryIndexMarkdown);

                var linkedPaths = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
                AddLinkedDocuments(
                    category,
                    definition,
                    "index.md",
                    overviewMarkdown,
                    store,
                    indexPaths,
                    linkedPaths);
                AddLinkedDocuments(
                    category,
                    definition,
                    definition.IndexPath,
                    categoryIndexMarkdown,
                    store,
                    indexPaths,
                    linkedPaths);

                if (category.Documents.Count > 0)
                {
                    categories.Add(category);
                }
            }

            return new AdvancedRimTalkDocumentationCatalog(overview, categories);
        }

        public DocumentationEntry Find(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            DocumentationEntry entry;
            return entriesByPath.TryGetValue(
                relativePath.Replace('\\', '/'),
                out entry)
                ? entry
                : null;
        }

        private static void AddDocument(
            DocumentationCategory category,
            string relativePath,
            string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown)
                || category.Documents.Exists(
                    entry => string.Equals(
                        entry.RelativePath,
                        relativePath,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            category.Documents.Add(
                new DocumentationEntry(
                    relativePath,
                    ReadTitle(
                        markdown,
                        Path.GetFileNameWithoutExtension(relativePath)),
                    markdown));
        }

        private static void AddLinkedDocuments(
            DocumentationCategory category,
            CategoryDefinition definition,
            string sourcePath,
            string sourceMarkdown,
            MarkdownStore store,
            HashSet<string> indexPaths,
            HashSet<string> linkedPaths)
        {
            foreach (Match match in MarkdownLink.Matches(sourceMarkdown))
            {
                string relativePath = ResolvePath(
                    sourcePath,
                    match.Groups[1].Value);
                if (relativePath == null
                    || !relativePath.EndsWith(
                        ".md",
                        StringComparison.OrdinalIgnoreCase)
                    || (!string.Equals(
                            relativePath,
                            definition.IndexPath,
                            StringComparison.OrdinalIgnoreCase)
                        && !relativePath.StartsWith(
                            definition.DocumentPrefix,
                            StringComparison.OrdinalIgnoreCase))
                    || IsOwnedByAnotherCategory(definition, relativePath)
                    || indexPaths.Contains(relativePath)
                    || !linkedPaths.Add(relativePath))
                {
                    continue;
                }

                string markdown;
                if (store.TryRead(relativePath, out markdown))
                {
                    AddDocument(category, relativePath, markdown);
                }
            }
        }

        private static bool IsOwnedByAnotherCategory(
            CategoryDefinition current,
            string relativePath)
        {
            CategoryDefinition owner = null;
            int ownerSpecificity = -1;
            foreach (CategoryDefinition definition in Definitions)
            {
                int specificity = -1;
                if (string.Equals(
                        relativePath,
                        definition.IndexPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    specificity = definition.IndexPath.Length;
                }
                else if (relativePath.StartsWith(
                    definition.DocumentPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    specificity = definition.DocumentPrefix.Length;
                }

                if (specificity > ownerSpecificity)
                {
                    owner = definition;
                    ownerSpecificity = specificity;
                }
            }

            return owner != null && owner != current;
        }

        private static string ReadTitle(string markdown, string fallback)
        {
            using (var reader = new StringReader(markdown ?? string.Empty))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed[0] != '#')
                    {
                        continue;
                    }

                    int markerLength = 0;
                    while (markerLength < trimmed.Length
                        && trimmed[markerLength] == '#')
                    {
                        markerLength++;
                    }

                    if (markerLength < trimmed.Length
                        && char.IsWhiteSpace(trimmed[markerLength]))
                    {
                        string title = trimmed.Substring(markerLength).Trim();
                        if (title.Length > 0)
                        {
                            return title;
                        }
                    }
                }
            }

            return fallback;
        }

        private static string ResolvePath(string currentPath, string link)
        {
            if (string.IsNullOrWhiteSpace(link)
                || link.StartsWith("#", StringComparison.Ordinal)
                || link.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || link.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            int fragment = link.IndexOf('#');
            if (fragment >= 0)
            {
                link = link.Substring(0, fragment);
            }

            link = link.Replace('\\', '/');
            if (link.Length == 0)
            {
                return null;
            }

            string directory = Path.GetDirectoryName(currentPath);
            string combined = string.IsNullOrEmpty(directory)
                ? link
                : directory.Replace('\\', '/') + "/" + link;
            var segments = new List<string>();
            foreach (string segment in combined.Split('/'))
            {
                if (string.IsNullOrEmpty(segment) || segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (segments.Count == 0)
                    {
                        return null;
                    }

                    segments.RemoveAt(segments.Count - 1);
                    continue;
                }

                segments.Add(segment);
            }

            return segments.Count == 0
                ? null
                : string.Join("/", segments.ToArray());
        }

        private sealed class CategoryDefinition
        {
            public CategoryDefinition(
                string id,
                string indexPath,
                string documentPrefix,
                string titleKey)
            {
                Id = id;
                IndexPath = indexPath;
                DocumentPrefix = documentPrefix;
                TitleKey = titleKey;
            }

            public string Id { get; }

            public string IndexPath { get; }

            public string DocumentPrefix { get; }

            public string TitleKey { get; }
        }

        private sealed class MarkdownStore
        {
            private readonly Assembly assembly;
            private readonly string contentRoot;
            private readonly Dictionary<string, string> cache =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public MarkdownStore(Assembly assembly, string contentRoot)
            {
                this.assembly = assembly;
                this.contentRoot = contentRoot;
            }

            public bool TryRead(string relativePath, out string markdown)
            {
                relativePath = relativePath.Replace('\\', '/');
                if (cache.TryGetValue(relativePath, out markdown))
                {
                    return markdown != null;
                }

                markdown = ReadEmbedded(relativePath);
                if (markdown == null && !string.IsNullOrWhiteSpace(contentRoot))
                {
                    string externalPath = Path.Combine(
                        contentRoot,
                        "docs",
                        "Arti",
                        "zh_cn",
                        relativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(externalPath))
                    {
                        try
                        {
                            markdown = File.ReadAllText(
                                externalPath,
                                Encoding.UTF8);
                        }
                        catch (Exception exception)
                        {
                            Log.Warning(
                                "[Advanced RimTalk] Could not read documentation '"
                                + externalPath
                                + "': "
                                + exception);
                        }
                    }
                }

                cache[relativePath] = markdown;
                return markdown != null;
            }

            private string ReadEmbedded(string relativePath)
            {
                string suffix = ResourceRoot
                    + relativePath.Replace('/', '.');
                foreach (string resourceName in assembly.GetManifestResourceNames())
                {
                    if (!string.Equals(
                            resourceName,
                            suffix,
                            StringComparison.OrdinalIgnoreCase)
                        && !resourceName.EndsWith(
                            "." + suffix,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            return null;
                        }

                        using (var reader = new StreamReader(
                            stream,
                            Encoding.UTF8,
                            true))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }

                return null;
            }
        }
    }

    internal sealed class DocumentationCategory
    {
        public DocumentationCategory(string id, string titleKey)
        {
            Id = id;
            TitleKey = titleKey;
            Documents = new List<DocumentationEntry>();
        }

        public string Id { get; }

        public string TitleKey { get; }

        public List<DocumentationEntry> Documents { get; }
    }

    internal sealed class DocumentationEntry
    {
        public DocumentationEntry(
            string relativePath,
            string title,
            string markdown)
        {
            RelativePath = relativePath;
            Title = title;
            Markdown = markdown;
        }

        public string RelativePath { get; }

        public string Title { get; }

        public string Markdown { get; }
    }
}
