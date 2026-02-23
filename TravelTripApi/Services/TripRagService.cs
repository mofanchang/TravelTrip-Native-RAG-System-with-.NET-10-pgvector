using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using TravelTrip.Core.DTOs;
using TravelTrip.Core.Data.Repositories;
using TravelTrip.Core.Models;

namespace TravelTripApi.Services;

/// <summary>
/// Trip RAG 伺服器 (.NET 10 原生版本)
/// 使用 Microsoft.Extensions.AI 進行向量化，並使用 Repository 層進行資料存取
/// </summary>
public class TripRagService
{
    private readonly ITripRepository _tripRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IConfiguration _config;
    private readonly ILogger<TripRagService> _logger;

    // 定義加權分數常數，消除 Magic Numbers
    private const double TITLE_MATCH_BONUS = 0.4;
    private const double KEYWORD_MATCH_BONUS = 0.2;
    private const int DEFAULT_SEARCH_BUFFER_MULTIPLIER = 2;

    public TripRagService(
        ITripRepository tripRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IConfiguration config,
        ILogger<TripRagService> logger)
    {
        _tripRepository = tripRepository;
        _embeddingGenerator = embeddingGenerator;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// 初始化：為資料庫中尚未有向量的行程生成 Embedding (使用 .NET 10 並行處理)
    /// </summary>
    public async Task<bool> InitializeRagAsync()
    {
        try
        {
            _logger.LogInformation("🚀 開始初始化 .NET 10 原生 RAG (並行模式)...");

            var batchSize = _config.GetValue<int>("RagInitialization:BatchSize", 10);
            var tripsToVectorize = await _tripRepository.GetTripsToVectorizeAsync(batchSize);

            if (tripsToVectorize.Count == 0)
            {
                _logger.LogInformation("✅ 所有行程已完成向量化");
                return true;
            }

            _logger.LogInformation("📊 正在並行生成 {Count} 個行程的向量...", tripsToVectorize.Count);

            // 使用 .NET 10 Parallel.ForEachAsync 提升效能
            await Parallel.ForEachAsync(tripsToVectorize, async (trip, ct) =>
            {
                var text = trip.GetEmbeddingText();
                var embedding = await _embeddingGenerator.GenerateAsync(new[] { text }, cancellationToken: ct);
                trip.Embedding = new Pgvector.Vector(embedding[0].Vector.ToArray());
                await _tripRepository.UpdateTripAsync(trip);
            });

            await _tripRepository.SaveChangesAsync();
            _logger.LogInformation("✅ RAG 並行初始化完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ RAG 初始化異常");
            return false;
        }
    }

    /// <summary>
    /// 使用 EF Core 10 原生向量搜尋相似行程 (透過 Repository)
    /// </summary>
    public async Task<List<TripSearchResultDto>> SearchSimilarTripsAsync(string query, int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<TripSearchResultDto>();

        _logger.LogInformation("🔍 .NET 10 向量搜尋: {Query}", query);

        // 1. 生成搜尋詞的向量
        var queryEmbeddingResult = await _embeddingGenerator.GenerateAsync(new[] { query });
        var queryVector = new Pgvector.Vector(queryEmbeddingResult[0].Vector.ToArray());

        // 2. 初步篩選 (取 TopK * 倍率)
        var bufferMultiplier = _config.GetValue<int>("Rag:SearchBufferMultiplier", DEFAULT_SEARCH_BUFFER_MULTIPLIER);
        var rawResults = await _tripRepository.SearchSimilarTripsAsync(queryVector, topK * bufferMultiplier);

        // 3. 🧠 記憶體中二次排名與權重加分
        var finalResults = rawResults.Select(r => 
        {
            var semanticScore = 1 - r.Distance;
            double keywordScore = 0.0;
            
            // 提取核心關鍵字 (簡單邏輯：前兩個字)
            string coreKeyword = query.Length >= 2 ? query.Substring(0, 2) : query.Trim();

            if (r.Entity.Name.Contains(coreKeyword, StringComparison.OrdinalIgnoreCase))
                keywordScore += TITLE_MATCH_BONUS;

            if (r.Entity.Keywords.Contains(coreKeyword, StringComparison.OrdinalIgnoreCase) || 
                r.Entity.City.Contains(coreKeyword, StringComparison.OrdinalIgnoreCase))
                keywordScore += KEYWORD_MATCH_BONUS;

            var finalScore = semanticScore + keywordScore;

            return new { 
                Entity = r.Entity, 
                SemanticScore = semanticScore, 
                Bonus = keywordScore, 
                FinalScore = finalScore 
            };
        })
        .OrderByDescending(r => r.FinalScore)
        .Take(topK)
        .ToList();

        // 4. 記錄排名分析
        foreach (var res in finalResults)
        {
            await _tripRepository.AddSearchRankingLogAsync(new SearchRankingLogEntity
            {
                QueryText = query,
                TripName = res.Entity.Name,
                SemanticScore = res.SemanticScore,
                KeywordBonus = res.Bonus,
                FinalScore = res.FinalScore,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _tripRepository.SaveChangesAsync();

        return finalResults.Select(r => MapToDto(r.Entity, r.FinalScore)).ToList();
    }

    public async Task<List<TripSearchResultDto>> RecommendTripsByThemeAsync(string theme, int topK = 5)
    {
        return await SearchSimilarTripsAsync($"推薦 {theme} 主題的旅遊行程", topK);
    }

    public async Task<bool> RefreshRagAsync()
    {
        await _tripRepository.ClearAllEmbeddingsAsync();
        await _tripRepository.SaveChangesAsync();
        return await InitializeRagAsync();
    }

    public async Task<List<TripSearchResultDto>> GetAllTripsAsync()
    {
        var trips = await _tripRepository.GetAllTripsAsync();
        return trips.Select(t => MapToDto(t, 0)).ToList();
    }

    public async Task<TripSearchResultDto?> GetTripByIdAsync(int id)
    {
        var trip = await _tripRepository.GetTripByIdAsync(id);
        return trip != null ? MapToDto(trip, 0) : null;
    }

    private static TripSearchResultDto MapToDto(TripEntity trip, double similarityScore)
    {
        return new TripSearchResultDto
        {
            Id = trip.Id,
            Name = trip.Name,
            Country = trip.Country,
            City = trip.City,
            Duration = trip.Duration,
            Keywords = trip.Keywords,
            Description = trip.Description,
            Price = trip.Price,
            Image = trip.Image,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            AvailableSeats = trip.AvailableSeats,
            SimilarityScore = similarityScore
        };
    }
}
