using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TravelTripApi.Controllers;
using TravelTripApi.Services;
using TravelTripApi.Data;
using TravelTripApi.DTOs;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TravelTripApi.Tests.Controllers;

/// <summary>
/// TripsController 測試
/// 測試行程相關 API：取得、搜尋、推薦
/// </summary>
public class TripsControllerTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly TripRagService _ragService;
    private readonly RagEmbeddingClient _ragClient;
    private readonly Mock<ILogger<TripsController>> _mockControllerLogger;
    private readonly TripsController _controller;

    public TripsControllerTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Trips_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);
        
        // 創建真實的 Configuration (使用 In-Memory)
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "RagServer:BaseUrl", "http://localhost:8000" }
        });
        var configuration = configBuilder.Build();
        
        // 創建 Mock IHttpClientFactory
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockHttpClient = new HttpClient();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(mockHttpClient);
        
        // 創建真實的 Service 實例
        var mockRagClientLogger = new Mock<ILogger<RagEmbeddingClient>>();
        var mockRagServiceLogger = new Mock<ILogger<TripRagService>>();
        
        _ragClient = new RagEmbeddingClient(
            mockHttpClientFactory.Object,
            mockRagClientLogger.Object,
            configuration);
        _ragService = new TripRagService(_context, _ragClient, mockRagServiceLogger.Object);
        
        _mockControllerLogger = new Mock<ILogger<TripsController>>();
        _controller = new TripsController(_ragService, _mockControllerLogger.Object);

        // 添加測試數據
        SeedTestData();
    }

    private void SeedTestData()
    {
        var trips = new List<TravelTripApi.Models.TripEntity>
        {
            new()
            {
                Id = 1,
                Name = "東京美食5日遊",
                Country = "日本",
                City = "東京",
                Duration = 5,
                Price = 38000,
                Keywords = "美食,日本,東京",
                Description = "探訪東京美食",
                AvailableSeats = 20,
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(35)
            },
            new()
            {
                Id = 2,
                Name = "大阪美食之旅",
                Country = "日本",
                City = "大阪",
                Duration = 4,
                Price = 32000,
                Keywords = "美食,日本,大阪",
                Description = "品嚐大阪美食",
                AvailableSeats = 15,
                StartDate = DateTime.UtcNow.AddDays(40),
                EndDate = DateTime.UtcNow.AddDays(44)
            }
        };

        _context.Trips.AddRange(trips);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    // ─── GetAll 測試 ───────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithAllTrips()
    {
        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsTwoTrips()
    {
        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseValue = okResult.Value;
        
        // 使用反射獲取 data 屬性
        var dataProperty = responseValue?.GetType().GetProperty("data");
        var data = dataProperty?.GetValue(responseValue) as List<TripSearchResultDto>;
        
        Assert.NotNull(data);
        Assert.Equal(2, data.Count);
    }

    // ─── GetById 測試 ──────────────────────────────────────

    [Fact]
    public async Task GetById_ValidId_ReturnsTrip()
    {
        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var trip = Assert.IsType<TripSearchResultDto>(okResult.Value);
        Assert.Equal(1, trip.Id);
        Assert.Equal("東京美食5日遊", trip.Name);
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(9999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ─── Search 測試 ───────────────────────────────────────

    [Fact]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search("", 5);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_NullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search(null!, 5);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_WhitespaceQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search("   ", 5);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_ValidQuery_ReturnsOk()
    {
        // Act
        var result = await _controller.Search("日本", 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    // ─── Recommend 測試 ────────────────────────────────────

    [Fact]
    public async Task Recommend_EmptyTheme_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Recommend("", 5);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Recommend_NullTheme_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Recommend(null!, 5);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Recommend_ValidTheme_ReturnsOk()
    {
        // Act
        var result = await _controller.Recommend("美食", 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    // ─── RAG System 測試 ───────────────────────────────────

    [Fact]
    public async Task InitializeRag_ReturnsResult()
    {
        // Act
        var result = await _controller.InitializeRag();

        // Assert
        // RAG 初始化可能失敗（因為沒有真實的 Python 服務）
        // 但 Controller 應該返回適當的回應
        Assert.True(result is OkObjectResult || result is BadRequestObjectResult);
    }

    [Fact]
    public async Task RefreshRag_ReturnsResult()
    {
        // Act
        var result = await _controller.RefreshRag();

        // Assert
        // RAG 刷新可能失敗（因為沒有真實的 Python 服務）
        // 但 Controller 應該返回適當的回應
        Assert.True(result is OkObjectResult || result is BadRequestObjectResult);
    }
}
