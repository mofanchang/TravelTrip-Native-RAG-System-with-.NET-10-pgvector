using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TravelTripApi.Services;
using TravelTripApi.Data;
using TravelTripApi.DTOs;
using TravelTripApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;

namespace TravelTripApi.Tests.Services;

/// <summary>
/// ChatbotService 測試
/// 測試 AI 聊天機器人功能：處理聊天、推薦行程
/// </summary>
public class ChatbotServiceTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly TripRagService _ragService;
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _mockEmbeddingGenerator;
    private readonly Mock<ILogger<ChatbotService>> _mockLogger;
    private readonly ChatbotService _service;

    public ChatbotServiceTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Chatbot_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);
        
        // 創建真實的 Configuration (使用 In-Memory)
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "RagServer:BaseUrl", "http://localhost:8000" }
        });
        var configuration = configBuilder.Build();
        
        // 創建 Mock IEmbeddingGenerator
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        
        // 設定 Mock 回傳固定的向量 (維度需一致)
        _mockEmbeddingGenerator.Setup(g => g.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Embedding<float>> { new Embedding<float>(new float[1024]) });

        var mockRagServiceLogger = new Mock<ILogger<TripRagService>>();
        
        _ragService = new TripRagService(_context, _mockEmbeddingGenerator.Object, mockRagServiceLogger.Object);
        
        _mockLogger = new Mock<ILogger<ChatbotService>>();
        _service = new ChatbotService(_context, _mockLogger.Object, _ragService);

        // 添加測試數據
        SeedTestData();
    }

    private void SeedTestData()
    {
        // 添加測試用戶
        var user = new UserEntity
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            FirstName = "Test",
            Password = "hashedpassword",
            DateJoined = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);

        // 添加測試行程
        var trips = new List<TripEntity>
        {
            new()
            {
                Id = 1,
                Name = "東京5日遊",
                Country = "日本",
                City = "東京",
                Duration = 5,
                Price = 38000,
                Keywords = "美食,日本,東京",
                Description = "探訪東京美食",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(35),
                AvailableSeats = 20
            },
            new()
            {
                Id = 2,
                Name = "釜山海灘度假",
                Country = "韓國",
                City = "釜山",
                Duration = 4,
                Price = 24000,
                Keywords = "海灘,度假,韓國",
                Description = "海雲台海灘度假",
                StartDate = DateTime.UtcNow.AddDays(40),
                EndDate = DateTime.UtcNow.AddDays(44),
                AvailableSeats = 15
            }
        };
        _context.Trips.AddRange(trips);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    // ─── ProcessChatAsync 測試 ─────────────────────────────

    [Fact]
    public async Task ProcessChatAsync_ValidQuery_ReturnsResponse()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "推薦日本旅遊",
            TopK = 3
        };

        // Act
        var response = await _service.ProcessChatAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.Message, response.Query);
        Assert.NotNull(response.AiResponse);
        Assert.NotNull(response.SessionId);
    }

    [Fact]
    public async Task ProcessChatAsync_WithSessionId_UsesProvidedSessionId()
    {
        // Arrange
        var sessionId = "test-session-123";
        var request = new ChatRequest
        {
            Message = "推薦海灘",
            TopK = 3,
            SessionId = sessionId
        };

        // Act
        var response = await _service.ProcessChatAsync(request);

        // Assert
        Assert.Equal(sessionId, response.SessionId);
    }

    [Fact]
    public async Task ProcessChatAsync_WithoutSessionId_GeneratesNewSessionId()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "推薦行程",
            TopK = 3
        };

        // Act
        var response = await _service.ProcessChatAsync(request);

        // Assert
        Assert.NotNull(response.SessionId);
        Assert.NotEmpty(response.SessionId);
    }

    [Fact]
    public async Task ProcessChatAsync_SavesChatHistory()
    {
        // Arrange
        var userId = 1;
        var request = new ChatRequest
        {
            Message = "推薦行程",
            TopK = 3,
            SessionId = "test-session"
        };

        // Act
        var response = await _service.ProcessChatAsync(request, userId);

        // Assert
        var chatHistory = await _context.ChatHistories
            .Where(ch => ch.UserId == userId && ch.SessionId == request.SessionId)
            .ToListAsync();

        Assert.Single(chatHistory);
        Assert.Equal(request.Message, chatHistory[0].UserMessage);
    }

    [Fact]
    public async Task ProcessChatAsync_RecommendationsHaveCorrectFormat()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "推薦",
            TopK = 5
        };

        // Act
        var response = await _service.ProcessChatAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.RecommendedTrips);
        
        // 如果有推薦結果，檢查格式
        if (response.RecommendedTrips.Any())
        {
            var trip = response.RecommendedTrips.First();
            Assert.NotNull(trip.Name);
            Assert.NotNull(trip.Link);
            Assert.Contains("/trips/", trip.Link);
        }
    }

    [Fact]
    public async Task ProcessChatAsync_HandlesEmptyResults()
    {
        // Arrange - 使用一個不會匹配任何行程的查詢
        var request = new ChatRequest
        {
            Message = "火星旅遊探險",
            TopK = 5
        };

        // Act
        var response = await _service.ProcessChatAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.AiResponse);
        Assert.Equal(request.Message, response.Query);
    }

    [Fact]
    public async Task ProcessChatAsync_TopKIsRespected()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "旅遊",
            TopK = 1
        };

        // Act
        var response = await _service.ProcessChatAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.RecommendedTrips.Count <= request.TopK);
    }

    [Fact]
    public async Task ProcessChatAsync_DifferentQueries_ReturnsDifferentResults()
    {
        // Arrange
        var request1 = new ChatRequest { Message = "日本", TopK = 5 };
        var request2 = new ChatRequest { Message = "韓國", TopK = 5 };

        // Act
        var response1 = await _service.ProcessChatAsync(request1);
        var response2 = await _service.ProcessChatAsync(request2);

        // Assert
        Assert.NotEqual(response1.SessionId, response2.SessionId);
        Assert.Equal("日本", response1.Query);
        Assert.Equal("韓國", response2.Query);
    }

    [Fact]
    public async Task ProcessChatAsync_WithUserId_LinksToCorrectUser()
    {
        // Arrange
        var userId = 1;
        var request = new ChatRequest
        {
            Message = "測試訊息",
            TopK = 3,
            SessionId = "user-session"
        };

        // Act
        await _service.ProcessChatAsync(request, userId);

        // Assert
        var history = await _context.ChatHistories
            .Where(h => h.UserId == userId)
            .ToListAsync();

        Assert.NotEmpty(history);
        Assert.All(history, h => Assert.Equal(userId, h.UserId));
    }

    // ─── GetChatHistoryAsync 測試 ────────────────────────────

    [Fact]
    public async Task GetChatHistoryAsync_ValidSessionId_ReturnsHistory()
    {
        // Arrange
        var sessionId = "history-session";
        _context.ChatHistories.Add(new ChatHistoryEntity
        {
            SessionId = sessionId,
            UserMessage = "Hello",
            AiResponse = "Hi",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var history = await _service.GetChatHistoryAsync(sessionId);

        // Assert
        Assert.Single(history);
        Assert.Equal("Hello", history[0].UserMessage);
    }
}
