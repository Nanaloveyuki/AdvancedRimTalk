using System;
using System.Security;
using System.Text;

namespace AdvancedRimTalk.Prompt
{
    public enum SystemInstructionFormat
    {
        Markdown,
        Xml
    }

    public sealed class SystemInstructionDocument
    {
        public string Identity { get; set; } = string.Empty;
        public string Rules { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string Memory { get; set; } = string.Empty;
        public string OutputContract { get; set; } = string.Empty;
    }

    public static class SystemInstructionRenderer
    {
        public static string Render(SystemInstructionDocument document, SystemInstructionFormat format)
        {
            document = document ?? new SystemInstructionDocument();
            return format == SystemInstructionFormat.Xml ? RenderXml(document) : RenderMarkdown(document);
        }

        private static string RenderXml(SystemInstructionDocument document)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("<system_instruction>");
            AppendXmlSection(output, "identity", document.Identity);
            AppendXmlSection(output, "rules", document.Rules);
            AppendXmlSection(output, "current_state", document.CurrentState);
            AppendXmlSection(output, "memory", document.Memory);
            AppendXmlSection(output, "output_contract", document.OutputContract);
            output.Append("</system_instruction>");
            return output.ToString();
        }

        private static string RenderMarkdown(SystemInstructionDocument document)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("# System Instruction");
            AppendMarkdownSection(output, "Identity", document.Identity);
            AppendMarkdownSection(output, "Rules", document.Rules);
            AppendMarkdownSection(output, "Current State", document.CurrentState);
            AppendMarkdownSection(output, "Memory", document.Memory);
            AppendMarkdownSection(output, "Output Contract", document.OutputContract);
            return output.ToString().TrimEnd();
        }

        private static void AppendXmlSection(StringBuilder output, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            output.Append("  <").Append(name).Append(">");
            output.Append(SecurityElement.Escape(value));
            output.Append("</").Append(name).AppendLine(">");
        }

        private static void AppendMarkdownSection(StringBuilder output, string title, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            output.AppendLine();
            output.Append("## ").AppendLine(title);
            output.AppendLine(value);
        }
    }
}
