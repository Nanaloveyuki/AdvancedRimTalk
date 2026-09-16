using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedRimTalk.Arti;

namespace AdvancedRimTalk.Prompt
{
    internal sealed class ArtiPromptAnalysisContext
    {
        private List<string> documents = new List<string>();
        private readonly HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        public IEnumerable<string> Names => names;

        public bool Refresh(IList<ArtiPromptPart> parts, ArtiPromptPart current)
        {
            var previous = new List<string>();
            int index = parts == null || current == null ? -1 : parts.IndexOf(current);
            for (int i = 0; i < index; i++)
            {
                ArtiPromptPart part = parts[i];
                if (part != null && part.Enabled) previous.Add(part.Content ?? string.Empty);
            }
            if (documents.SequenceEqual(previous, StringComparer.Ordinal)) return false;
            documents = previous;
            names.Clear();
            foreach (string document in documents)
                foreach (ArtiCodeBlock block in new ArtiDocumentParser().Parse(document).CodeBlocks)
                    if (!block.HasErrors) AddDeclarations(block.ParseResult?.Program, names);
            return true;
        }

        internal static void AddDeclarations(ArtiProgram program, ISet<string> names)
        {
            if (program == null) return;
            foreach (ArtiStatement statement in program.Statements)
            {
                if (statement is ArtiFunctionDeclarationStatement function) names.Add(function.Name);
                else if (statement is ArtiVariableDeclarationStatement constant && constant.IsConst
                    && constant.Name != "_") names.Add(constant.Name);
            }
        }
    }
}
