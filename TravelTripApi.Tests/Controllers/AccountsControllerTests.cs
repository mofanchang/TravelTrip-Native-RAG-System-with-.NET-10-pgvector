using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
/// AccountsController 測試
/// 測試帳戶管理相關 API
/// </summary>
public class AccountsControllerTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly AuthService _authService;
    private readonly Mock<ILogger<AccountsController>> _mockLogger;
    private readonly AccountsController _controller;
    private const int TestUserId = 1;

    public AccountsControllerTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Accounts_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);

        // 創建 Configuration
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Jwt:Key", "YourSuperSecretKey1234567890123456" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:ExpireMinutes", "1440" }
        });
        var configuration = configBuilder.Build();

        // 創建服務
        var mockAuthLogger = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(_context, configuration, mockAuthLogger.Object);

        _mockLogger = new Mock<ILogger<AccountsController>>();
        _controller = new AccountsController(_authService, _mockLogger.Object);

        // 添加測試數據
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new UserEntity
        {
            Id = TestUserId,
            Email = "existing@example.com",
            Username = "existing@example.com",
            FirstName = "Existing",
            Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DateJoined = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString()),
            new Claim(ClaimTypes.Email, "existing@example.com")
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

    // ─── Register 測試 ─────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com", // 已存在
            Password = "Password123!",
            FirstName = "Duplicate",
            LastName = "User"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Register_EmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "12345", // 少於 6 個字元
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ─── Login 測試 ────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "existing@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Token);
    }

    [Fact]
    public async Task Login_InvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "existing@example.com",
            Password = "WrongPassword!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_EmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "existing@example.com",
            Password = ""
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ─── GetProfile 測試 ───────────────────────────────────

    [Fact]
    public async Task GetProfile_AuthenticatedUser_ReturnsProfile()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("existing@example.com", user.Email);
    }

    [Fact]
    public async Task GetProfile_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    // ─── TestAuth 測試 ─────────────────────────────────────

    [Fact]
    public void TestAuth_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var result = _controller.TestAuth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
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
