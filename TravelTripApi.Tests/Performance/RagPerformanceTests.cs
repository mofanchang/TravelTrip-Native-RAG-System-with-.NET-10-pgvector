using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using TravelTripApi.Services;
using TravelTripApi.Models;
using Moq;
using Microsoft.Extensions.Logging;
using TravelTripApi.Data;
using Microsoft.EntityFrameworkCore;
using TravelTripApi.Tests.TestHelpers;

namespace TravelTripApi.Tests.Performance;

/// <summary>
/// 效能測試範例 - 雖然目前因系統權限可能無法執行，
/// 但這份程式碼在面試時展示非常有價值，證明你有考量高併發場景。
/// </summary>
public class RagPerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<TravelDbContext> _mockContext;
    private readonly Mock<RagEmbeddingClient> _mockRagClient;
    private readonly Mock<ILogger<TripRagService>> _mockLogger;
    private readonly TripRagService _service;

    public RagPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // 快速 Setup Mock
        _mockContext = new Mock<TravelDbContext>();
        _mockRagClient = new Mock<RagEmbeddingClient>();
        _mockLogger = new Mock<ILogger<TripRagService>>();

        // 模擬大量資料回傳 (模擬高負載下的資料庫讀取)
        var fakeTrips = Enumerable.Range(1, 100).Select(i => new TripEntity 
        { 
            Id = i, 
            Name = $"Trip {i}", 
            Price = 1000 + i 
        }).ToList();
        
        var mockSet = new Mock<DbSet<TripEntity>>();
        var queryable = fakeTrips.AsQueryable();

        mockSet.As<IQueryable<TripEntity>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<TripEntity>(queryable.Provider));
        mockSet.As<IQueryable<TripEntity>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<TripEntity>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<TripEntity>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        mockSet.As<IAsyncEnumerable<TripEntity>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<TripEntity>(queryable.GetEnumerator()));
        
        _mockContext.Setup(c => c.Trips).Returns(mockSet.Object);

        // 模擬 Python 服務回應
        _mockRagClient.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<RagSearchResult> { new RagSearchResult { Id = 1, SimilarityScore = 0.9 } });

        _service = new TripRagService(_mockContext.Object, _mockRagClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Search_ConcurrentRequests_ShouldRespondFast()
    {
        // Arrange
        int concurrentRequests = 50; // 模擬 50 個同時請求
        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        _output.WriteLine($"🚀 開始執行 {concurrentRequests} 個並發搜尋請求...");
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_service.SearchSimilarTripsAsync("測試查詢", 5));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert & Report
        _output.WriteLine($"✅ 完成！總耗時: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"📊 平均每個請求耗時: {stopwatch.ElapsedMilliseconds / (double)concurrentRequests} ms");

        // 驗證總耗時是否在可接受範圍 (例如 50 個請求不超過 1000ms - 注意這是 Mock 的速度)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "系統在高併發下回應過慢！");
    }
}
