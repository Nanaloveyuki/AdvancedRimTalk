using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace AdvancedRimTalk.Prompt
{
    internal sealed class PromptPreview
    {
        private PromptPreview(string plainText, string rawResponseJson)
        {
            PlainText = plainText;
            RawResponseJson = rawResponseJson;
        }

        public string PlainText { get; private set; }
        public string RawResponseJson { get; private set; }

        public static PromptPreview FromMessages<TRole>(IList<ValueTuple<TRole, string>> messages)
        {
            StringBuilder plain = new StringBuilder();
            List<PreviewMessage> serialized = new List<PreviewMessage>();
            if (messages != null)
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    string role = messages[i].Item1.ToString().ToLowerInvariant();
                    if (role == "ai") role = "assistant";
                    if (i > 0) plain.AppendLine().AppendLine();
                    plain.Append("[").Append(role).AppendLine("]").Append(messages[i].Item2 ?? string.Empty);
                    serialized.Add(new PreviewMessage
                    {
                        Role = role,
                        Content = messages[i].Item2 ?? string.Empty
                    });
                }
            }
            using (MemoryStream stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(List<PreviewMessage>)).WriteObject(stream, serialized);
                return new PromptPreview(plain.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [DataContract]
        private sealed class PreviewMessage
        {
            [DataMember(Name = "role", Order = 0)]
            public string Role;
            [DataMember(Name = "content", Order = 1)]
            public string Content;
        }
    }
}
