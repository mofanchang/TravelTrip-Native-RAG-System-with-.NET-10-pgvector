using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using TravelTripApi.Services;
using TravelTripApi.Models;
using TravelTripApi.Data.Repositories;

namespace TravelTripApi.Tests;

/// <summary>
/// 測試 TripRagService：搜索與推薦流程
/// 使用 Mock Repository，不依賴資料庫
/// </summary>
public class TripRagServiceTests
{
    private readonly Mock<ITripRepository> _mockRepo;
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _mockEmbeddingGenerator;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<TripRagService>> _mockLogger;
    private readonly TripRagService _service;
    private readonly List<TripEntity> _fakeTripList;

    public TripRagServiceTests()
    {
        // ─── 建立假資料 ─────────────────────────────────────
        _fakeTripList = new List<TripEntity>
        {
            new TripEntity { Id = 1, Name = "東京美食探索5日遊", Country = "日本", City = "東京", Keywords = "美食,東京" },
            new TripEntity { Id = 2, Name = "釜山海雲台度假4日", Country = "韓國", City = "釜山", Keywords = "海灘,釜山" },
            new TripEntity { Id = 3, Name = "瑞士阿爾卑斯山8日", Country = "瑞士", City = "因特拉肯", Keywords = "自然,瑞士" }
        };

        // ─── Mock 設定 ────────────────────────────
        _mockRepo = new Mock<ITripRepository>();
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<TripRagService>>();

        // Mock Repository Methods
        _mockRepo.Setup(r => r.GetTripsToVectorizeAsync(It.IsAny<int>())).ReturnsAsync(_fakeTripList);
        _mockRepo.Setup(r => r.GetAllTripsAsync()).ReturnsAsync(_fakeTripList);
        _mockRepo.Setup(r => r.GetTripByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => _fakeTripList.FirstOrDefault(t => t.Id == id));
        _mockRepo.Setup(r => r.SearchSimilarTripsAsync(It.IsAny<Pgvector.Vector>(), It.IsAny<int>()))
            .ReturnsAsync((Pgvector.Vector v, int limit) => 
                _fakeTripList.Select(t => (t, 0.1)).Take(limit).ToList());

        _mockRepo.Setup(r => r.UpdateTripAsync(It.IsAny<TripEntity>())).ReturnsAsync(true);
        _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        _mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);

        // ─── Mock IEmbeddingGenerator 設定 ──────────────────
        _mockEmbeddingGenerator.Setup(g => g.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Embedding<float>> { new Embedding<float>(new float[1024]) });

        _service = new TripRagService(_mockRepo.Object, _mockEmbeddingGenerator.Object, _mockConfig.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InitializeRagAsync_WithTrips_ReturnsTrue()
    {
        bool result = await _service.InitializeRagAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task SearchSimilarTripsAsync_EmptyQuery_ReturnsEmpty()
    {
        var results = await _service.SearchSimilarTripsAsync("");
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchSimilarTripsAsync_ValidQuery_ReturnsResults()
    {
        var results = await _service.SearchSimilarTripsAsync("日本");
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task RecommendTripsByThemeAsync_ValidTheme_ReturnsResults()
    {
        var results = await _service.RecommendTripsByThemeAsync("海灘");
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetAllTripsAsync_ReturnsAllTrips()
    {
        var results = await _service.GetAllTripsAsync();
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetTripByIdAsync_ValidId_ReturnsTrip()
    {
        var result = await _service.GetTripByIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal("東京美食探索5日遊", result.Name);
    }

    [Fact]
    public async Task RefreshRagAsync_ReturnsTrue()
    {
        bool result = await _service.RefreshRagAsync();
        Assert.True(result);
    }
}
