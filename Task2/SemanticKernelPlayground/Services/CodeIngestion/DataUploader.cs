

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernelPlayground.Models;

#pragma warning disable SKEXP0001

namespace SemanticKernelPlayground.Services.CodeIngestion
{
    public class DataUploader
    {
        private readonly IVectorStore _vectorStore;
        private readonly ITextEmbeddingGenerationService _embeddingService;

        public DataUploader(IVectorStore vectorStore, ITextEmbeddingGenerationService embeddingService)
        {
            _vectorStore = vectorStore;
            _embeddingService = embeddingService;
        }

        public async Task UploadToVectorStore(string collectionName, IEnumerable<TextChunk> chunks)
        {
            var collection = _vectorStore.GetCollection<string, TextChunk>(collectionName);
            await collection.CreateCollectionIfNotExistsAsync();

            foreach (var chunk in chunks)
            {
                Console.WriteLine($"Embedding: {chunk.Key}");
                chunk.TextEmbedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Text);
                await collection.UpsertAsync(chunk);
            }
        }
    }
}

#pragma warning restore SKEXP0001
