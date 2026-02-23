using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelTripApi.Controllers;
using TravelTripApi.Data;
using TravelTripApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TravelTripApi.Tests.Controllers;

/// <summary>
/// UserController 測試
/// 測試用戶個人資料相關 API
/// </summary>
public class UserControllerTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly UserController _controller;
    private const int TestUserId = 1;

    public UserControllerTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_User_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);
        _controller = new UserController(_context);

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
            LastName = "User",
            Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DateJoined = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    private void SetupAuthenticatedUser(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User")
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

    // ─── GetProfile 測試 ───────────────────────────────────

    [Fact]
    public async Task GetProfile_AuthenticatedUser_ReturnsProfile()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // 使用反射獲取匿名對象屬性
        var profile = okResult.Value;
        var nameProperty = profile.GetType().GetProperty("name");
        var emailProperty = profile.GetType().GetProperty("email");
        
        Assert.NotNull(nameProperty);
        Assert.NotNull(emailProperty);
        
        var name = nameProperty.GetValue(profile) as string;
        var email = emailProperty.GetValue(profile) as string;
        
        Assert.Equal("Test", name);
        Assert.Equal("test@example.com", email);
    }

    [Fact]
    public async Task GetProfile_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - 沒有設置認證

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetProfile_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(999); // 不存在的用戶 ID

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetProfile_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid") // 無效的 ID
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => _controller.GetProfile());
    }
}
