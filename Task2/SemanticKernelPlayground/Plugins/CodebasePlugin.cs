#pragma warning disable SKEXP0001

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernelPlayground.Models;
using SemanticKernelPlayground.Services.CodeIngestion;
using System.ComponentModel;
using System.Text;

namespace SemanticKernelPlayground.Plugins;

public class CodebasePlugin
{
    private readonly IVectorStore _vectorStore;
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public CodebasePlugin(IVectorStore vectorStore, ITextEmbeddingGenerationService embeddingService)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
    }

    [KernelFunction]
    [Description("Ingest all C# files from a folder into the vector store for semantic search")]
    public async Task<string> IngestCodebase(
    [Description("Path to the folder containing C# files")] string path)
    {
        if (!Directory.Exists(path))
            return $"Directory not found: {path}";

        var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        var allChunks = new List<TextChunk>();

        foreach (var file in files)
            allChunks.AddRange(DocumentReader.ParseFile(file));

        var uploader = new DataUploader(_vectorStore, _embeddingService);
        await uploader.UploadToVectorStore("codebase", allChunks);

        return $"Indexed {allChunks.Count} chunks from {files.Length} files.";
    }

    [KernelFunction]
    [Description("Ask a question about the ingested codebase using vector similarity search")]
    public async Task<string> Ask(
        [Description("A natural language question about the codebase")] string query)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
        var collection = _vectorStore.GetCollection<string, TextChunk>("codebase");

        await collection.CreateCollectionIfNotExistsAsync();

        var results = collection.SearchEmbeddingAsync(embedding, top: 5);

        var sb = new StringBuilder();

        await foreach (var result in results)
        {
            var chunk = result.Record;
            sb.AppendLine($"{chunk.DocumentName} (Line {chunk.ParagraphId}):");
            sb.AppendLine(chunk.Text);
            sb.AppendLine();
        }

        return sb.Length > 0 ? sb.ToString() : "No relevant information found.";
    }
}