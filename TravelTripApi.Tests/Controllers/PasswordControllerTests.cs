using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelTripApi.Controllers;
using TravelTripApi.Services;
using TravelTripApi.Data;
using TravelTripApi.DTOs;
using TravelTripApi.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TravelTripApi.Tests.Controllers;

/// <summary>
/// PasswordController 測試
/// 測試密碼管理相關 API
/// </summary>
public class PasswordControllerTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly PasswordResetService _passwordResetService;
    private readonly EmailService _emailService;
    private readonly Mock<ILogger<PasswordController>> _mockLogger;
    private readonly PasswordController _controller;
    private const int TestUserId = 1;

    public PasswordControllerTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Password_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);

        // 創建 Configuration
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Email:Smtp:Host", "smtp.test.com" },
            { "Email:Smtp:Port", "587" },
            { "Email:Smtp:Username", "test" },
            { "Email:Smtp:Password", "test" },
            { "Email:From", "test@test.com" },
            { "Email:FromName", "Test" },
            { "Frontend:Url", "http://localhost:3000" }
        });
        var configuration = configBuilder.Build();

        // 創建服務
        var mockEmailLogger = new Mock<ILogger<EmailService>>();
        var mockPasswordLogger = new Mock<ILogger<PasswordResetService>>();

        _emailService = new EmailService(configuration, mockEmailLogger.Object);
        _passwordResetService = new PasswordResetService(_context, _emailService, mockPasswordLogger.Object);

        _mockLogger = new Mock<ILogger<PasswordController>>();
        _controller = new PasswordController(_passwordResetService, _mockLogger.Object);

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
            Password = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
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
            new Claim(ClaimTypes.Email, "test@example.com")
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

    // ─── ForgotPassword 測試 ───────────────────────────────

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOk()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PasswordResponse>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ForgotPassword_NonExistentEmail_ReturnsOkForSecurity()
    {
        // Arrange - 出於安全考量，即使 Email 不存在也返回成功
        var request = new ForgotPasswordRequest
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PasswordResponse>(okResult.Value);
        Assert.True(response.Success);
    }

    // ─── ValidateToken 測試 ────────────────────────────────

    [Fact]
    public async Task ValidateToken_EmptyToken_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ValidateToken("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ValidateToken_NullToken_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ValidateToken(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ValidateToken_InvalidToken_ReturnsFailure()
    {
        // Act
        var result = await _controller.ValidateToken("invalid-token");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PasswordResponse>(okResult.Value);
        Assert.False(response.Success);
    }

    // ─── ResetPassword 測試 ────────────────────────────────

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Token = "invalid-token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ─── ChangePassword 測試 ───────────────────────────────

    [Fact]
    public async Task ChangePassword_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - 沒有設置認證
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task ChangePassword_InvalidCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        SetupAuthenticatedUser();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PasswordResponse>(okResult.Value);
        Assert.True(response.Success);
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
