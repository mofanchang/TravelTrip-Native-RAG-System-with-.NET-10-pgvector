using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTripApi.Data;

namespace TravelTripApi.Tests.Helpers;

/// <summary>
/// 測試輔助工具
/// - 提供可用的 In-Memory DbContext
/// - Mock IConfiguration
/// - Mock ILogger
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// 建立「已初始化完成」的 In-Memory DbContext
    /// 等同 Django：migrate 完成後的狀態
    /// </summary>
    public static TravelDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging() // 測試用，方便 debug
            .Options;

        var context = new TravelDbContext(options);

        // 關鍵：初始化 EF Core 內部結構
        context.Database.EnsureDeleted(); // 確保測試之間完全隔離
        context.Database.EnsureCreated(); // 等同 migrate

        return context;
    }

    /// <summary>
    /// Mock IConfiguration（JWT / Email / Frontend）
    /// </summary>
    public static IConfiguration CreateMockConfiguration()
    {
        var mockConfig = new Mock<IConfiguration>();

        // JWT
        mockConfig.Setup(c => c["Jwt:Secret"])
            .Returns("TestSecretKeyThatIsAtLeast32CharactersLong12345");
        mockConfig.Setup(c => c["Jwt:Issuer"])
            .Returns("TravelTripApi");
        mockConfig.Setup(c => c["Jwt:Audience"])
            .Returns("TravelTripClient");
        mockConfig.Setup(c => c["Jwt:ExpireMinutes"])
            .Returns("1440");

        // Email
        mockConfig.Setup(c => c["Email:Smtp:Host"])
            .Returns("smtp.test.com");
        mockConfig.Setup(c => c["Email:Smtp:Port"])
            .Returns("587");
        mockConfig.Setup(c => c["Email:Smtp:Username"])
            .Returns("test");
        mockConfig.Setup(c => c["Email:Smtp:Password"])
            .Returns("test");
        mockConfig.Setup(c => c["Email:From"])
            .Returns("test@test.com");
        mockConfig.Setup(c => c["Email:FromName"])
            .Returns("Test");
        mockConfig.Setup(c => c["Frontend:Url"])
            .Returns("http://localhost:3000");

        return mockConfig.Object;
    }

    /// <summary>
    /// Mock ILogger
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }
}
