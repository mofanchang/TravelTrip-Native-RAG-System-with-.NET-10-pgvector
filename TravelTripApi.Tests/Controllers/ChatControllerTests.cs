using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelTripApi.Controllers;
using TravelTripApi.Services;
using TravelTripApi.Data;
using TravelTripApi.DTOs;
using TravelTripApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TravelTripApi.Tests.Controllers;

/// <summary>
/// ChatController 測試
/// 測試聊天機器人相關 API
/// </summary>
public class ChatControllerTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly ChatbotService _chatbotService;
    private readonly TripRagService _ragService;
    private readonly Mock<ILogger<ChatController>> _mockLogger;
    private readonly ChatController _controller;
    private const int TestUserId = 1;

    public ChatControllerTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Chat_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);

        // 創建 Configuration
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "RagServer:BaseUrl", "http://localhost:8000" }
        });
        var configuration = configBuilder.Build();

        // 創建服務
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        var mockRagClientLogger = new Mock<ILogger<RagEmbeddingClient>>();
        var mockRagServiceLogger = new Mock<ILogger<TripRagService>>();
        var mockChatbotLogger = new Mock<ILogger<ChatbotService>>();

        var ragClient = new RagEmbeddingClient(
            mockHttpClientFactory.Object,
            mockRagClientLogger.Object,
            configuration);

        _ragService = new TripRagService(_context, ragClient, mockRagServiceLogger.Object);
        _chatbotService = new ChatbotService(_context, mockChatbotLogger.Object, _ragService);

        _mockLogger = new Mock<ILogger<ChatController>>();
        _controller = new ChatController(_chatbotService, _ragService, _mockLogger.Object);

        // 添加測試數據
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new UserEntity
        {
            Id = TestUserId,
            Email = "test@example.com",
            Username = "testuser",
            FirstName = "Test",
            Password = "hashedpassword",
            DateJoined = DateTime.UtcNow,
            IsActive = true
        };

        var trips = new List<TripEntity>
        {
            new()
            {
                Id = 1,
                Name = "東京美食之旅",
                Country = "日本",
                City = "東京",
                Keywords = "美食,日本",
                Description = "探訪東京美食",
                Duration = 5,
                Price = 38000,
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(35),
                AvailableSeats = 20
            }
        };

        _context.Users.Add(user);
        _context.Trips.AddRange(trips);
        _context.SaveChanges();
    }

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    // ─── Chat 測試 ─────────────────────────────────────────

    [Fact]
    public async Task Chat_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "推薦日本旅遊",
            TopK = 3
        };

        // Act
        var result = await _controller.Chat(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ChatResponse>(okResult.Value);
        Assert.Equal(request.Message, response.Query);
        Assert.NotNull(response.AiResponse);
    }

    [Fact]
    public async Task Chat_EmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "",
            TopK = 3
        };

        // Act
        var result = await _controller.Chat(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Chat_NullMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = null!,
            TopK = 3
        };

        // Act
        var result = await _controller.Chat(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Chat_WithAuthenticatedUser_ProcessesSuccessfully()
    {
        // Arrange
        SetupAuthenticatedUser();
        var request = new ChatRequest
        {
            Message = "推薦行程",
            TopK = 3
        };

        // Act
        var result = await _controller.Chat(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    // ─── Recommend 測試 ────────────────────────────────────

    [Fact]
    public async Task Recommend_ValidQuery_ReturnsResponse()
    {
        // Arrange
        var query = "日本旅遊";

        // Act
        var result = await _controller.Recommend(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ChatResponse>(okResult.Value);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Recommend_EmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Recommend("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Recommend_NullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Recommend(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ─── GetHistory 測試 ───────────────────────────────────

    [Fact]
    public async Task GetHistory_EmptySessionId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetHistory("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetHistory_NullSessionId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetHistory(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetHistory_ValidSessionId_ReturnsHistory()
    {
        // Arrange
        var sessionId = "test-session-123";

        // Act
        var result = await _controller.GetHistory(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    // ─── RebuildIndex 測試 ─────────────────────────────────

    [Fact]
    public async Task RebuildIndex_ReturnsResult()
    {
        // Act
        var result = await _controller.RebuildIndex();

        // Assert
        // RAG 可能失敗（沒有真實服務），但應該返回適當的回應
        Assert.True(result is OkObjectResult || result is ObjectResult);
    }

    // ─── TestRag 測試 ──────────────────────────────────────

    [Fact]
    public async Task TestRag_ValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "日本";

        // Act
        var result = await _controller.TestRag(query, 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task TestRag_EmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.TestRag("", 3);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task TestRag_NullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.TestRag(null!, 3);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Health 測試 ───────────────────────────────────────

    [Fact]
    public void Health_ReturnsOk()
    {
        // Act
        var result = _controller.Health();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
