using System;
using AdvancedRimTalk.Integration;
using RimTalk.Prompt;
using Scriban;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class MixedTemplateChecks
    {
        internal static void Run()
        {
            Check("before {{% core.emit(\"Arti\") %}} / {{ for x in [1,2,3] }}{{ x }}{{ end }}",
                "before Arti / 123");
            Check("{{ if true }}native{{ else }}wrong{{ end }} {{% core.emit(\"tail\") %}}", "native tail");
            Check("{{% core.emit(\"plain\") %}} {{ \"quote: \\\"value\\\"\" }}", "plain quote: \"value\"");
            Check("{{% core.emit(\"{{ 2 + 3 }}\") %}} / {{ 6 + 1 }}", "{{ 2 + 3 }} / 7");
            Check("{{% core.emit(\"{{% core.emit('again') %}} {{ art.topic }}\") %}} / {{ 1 }}",
                "{{% core.emit('again') %}} {{ art.topic }} / 1");
            Check("{{% core.emit(\"{{ broken\") %}} / {{ 3 }}", "{{ broken / 3");
        }

        private static void Check(string source, string expected)
        {
            var arti = ArtiPromptDocumentRenderer.Render(source, new PromptContext { IsPreview = true });
            if (arti.HasErrors) throw new Exception("Arti preprocessing failed for mixed template.");
            Template native = Template.Parse(arti.TemplateText);
            if (native.HasErrors) throw new Exception("Native Scriban parse failed after Arti preprocessing.");
            string result = arti.Restore(native.Render());
            if (result != expected) throw new Exception("Mixed template mismatch: " + result);
        }
    }
}
