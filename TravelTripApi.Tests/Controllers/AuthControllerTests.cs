using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using TravelTripApi.Controllers;
using TravelTripApi.Data;
using TravelTripApi.Models;
using TravelTripApi.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TravelTripApi.Tests.Controllers;

/// <summary>
/// AuthController 測試
/// 測試用戶認證相關功能：登入、註冊
/// </summary>
public class AuthControllerTests
{
    private readonly TravelDbContext _context;
    private readonly AuthController _controller;
    private readonly IConfiguration _config;

    public AuthControllerTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Auth_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);

        // Mock Configuration
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Key", "YourSuperSecretKey1234567890123456"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _controller = new AuthController(_context, _config);
    }

    // ─── Register 測試 ─────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            Name = "New User"
        };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // 驗證資料庫
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        Assert.NotNull(user);
        Assert.Equal(registerDto.Email, user.Email);
        Assert.True(BCrypt.Net.BCrypt.Verify(registerDto.Password, user.Password));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange - 先建立一個用戶
        var existingUser = new UserEntity
        {
            Email = "existing@example.com",
            Username = "existing@example.com",
            FirstName = "Existing",
            Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DateJoined = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            Email = "existing@example.com", // 同樣的 Email
            Password = "Password123!",
            Name = "Duplicate User"
        };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    // ─── Login 測試 ────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange - 先註冊一個用戶
        var password = "Password123!";
        var user = new UserEntity
        {
            Email = "test@example.com",
            Username = "test@example.com",
            FirstName = "Test User",
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            DateJoined = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic? responseValue = okResult.Value;
        Assert.NotNull(responseValue);
        
        // 使用反射檢查 token 屬性
        var tokenProperty = responseValue?.GetType().GetProperty("token");
        Assert.NotNull(tokenProperty);
        var token = tokenProperty?.GetValue(responseValue) as string;
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task Login_InvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange - 先建立一個用戶
        var user = new UserEntity
        {
            Email = "test@example.com",
            Username = "test@example.com",
            FirstName = "Test User",
            Password = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            DateJoined = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword123!" // 錯誤的密碼
        };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmptyEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Register_CreatesUserWithEncryptedPassword()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "secure@example.com",
            Password = "MySecretPassword123!",
            Name = "Secure User"
        };

        // Act
        await _controller.Register(registerDto);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        Assert.NotNull(user);
        
        // 密碼應該被加密（不等於原始密碼）
        Assert.NotEqual(registerDto.Password, user.Password);
        
        // 但是可以通過 BCrypt 驗證
        Assert.True(BCrypt.Net.BCrypt.Verify(registerDto.Password, user.Password));
    }
}
