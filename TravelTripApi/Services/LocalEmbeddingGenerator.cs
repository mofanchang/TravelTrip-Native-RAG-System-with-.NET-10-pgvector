using Microsoft.Extensions.AI;
using SmartComponents.LocalEmbeddings;

namespace TravelTripApi.Services;

/// <summary>
/// 使用 SmartComponents.LocalEmbeddings 的原生 .NET 向量產生器
/// 這是免費且在地端運作的，模型為 all-MiniLM-L6-v2 (384 維度)
/// </summary>
public class LocalEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly LocalEmbedder _embedder;
    private readonly EmbeddingGeneratorMetadata _metadata;

    public LocalEmbeddingGenerator()
    {
        _embedder = new LocalEmbedder();
        _metadata = new EmbeddingGeneratorMetadata("SmartComponents.LocalEmbeddings", null, "all-MiniLM-L6-v2", 384);
    }
    public void Dispose()
    {
        _embedder.Dispose();
    }

    public EmbeddingGeneratorMetadata Metadata => _metadata;

    public TService? GetService<TService>(object? serviceKey = null) where TService : class
    {
        return this as TService;
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, 
        EmbeddingGenerationOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        // SmartComponents.LocalEmbeddings 是同步執行的，我們將其封裝在 Task 中
        return await Task.Run(() =>
        {
            var embeddings = values.Select(v => 
            {
                var vector = _embedder.Embed(v);
                return new Embedding<float>(vector.Values);
            }).ToList();

            return new GeneratedEmbeddings<Embedding<float>>(embeddings);
        }, cancellationToken);
    }
}
