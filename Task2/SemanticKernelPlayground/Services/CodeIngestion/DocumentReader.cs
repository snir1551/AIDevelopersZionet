using SemanticKernelPlayground.Models;
using System.Text.RegularExpressions;

namespace SemanticKernelPlayground.Services.CodeIngestion
{
    public class DocumentReader
    {

        public static IEnumerable<TextChunk> ParseFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var docName = Path.GetFileName(filePath);
            var chunks = new List<TextChunk>();

            var currentChunk = new List<string>();
            int paragraphId = 0;
            string? methodSignature = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if ((trimmed.StartsWith("public ") ||
                     trimmed.StartsWith("private ") ||
                     trimmed.StartsWith("protected ") ||
                     trimmed.StartsWith("internal ")) &&
                     trimmed.Contains("(") && trimmed.Contains(")") && !trimmed.EndsWith(";"))
                {
                    if (currentChunk.Count > 0)
                    {
                        AddChunk(chunks, currentChunk, docName, ref paragraphId, methodSignature);
                        currentChunk.Clear();
                    }
                    methodSignature = trimmed;
                }

                currentChunk.Add(line);
            }

            if (currentChunk.Count > 0)
            {
                AddChunk(chunks, currentChunk, docName, ref paragraphId, methodSignature);
            }

            return chunks;
        }

        private static void AddChunk(List<TextChunk> chunks, List<string> lines, string docName, ref int id, string? methodSignature)
        {
            var text = string.Join("\n", lines).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                id++;
                var commentPrefix = string.IsNullOrWhiteSpace(methodSignature) ? string.Empty : $"// METHOD: {methodSignature}\n";
                chunks.Add(new TextChunk
                {
                    Key = $"{docName}_{id}",
                    DocumentName = docName,
                    ParagraphId = id,
                    Text = commentPrefix + text,
                    TextEmbedding = ReadOnlyMemory<float>.Empty
                });
            }
        }
    }
}
