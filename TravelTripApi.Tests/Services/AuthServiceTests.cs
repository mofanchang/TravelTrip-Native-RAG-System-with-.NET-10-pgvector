using Xunit;
using TravelTripApi.Services;
using TravelTripApi.DTOs;
using TravelTripApi.Models;
using TravelTripApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TravelTripApi.Tests.Services;

/// <summary>
/// 認證服務測試
/// </summary>
public class AuthServiceTests
{
    /// <summary>
    /// 測試註冊新使用者成功
    /// </summary>
    [Fact]
    public async Task Register_NewUser_ReturnsSuccessWithToken()
    {
        // Arrange: 建立 In-Memory DbContext、Mock Config、Mock Logger
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<AuthService>();
        var service = new AuthService(context, config, logger);

        var request = new RegisterRequest
        {
            Email = "newuser@test.com",
            Password = "Password123",
            FirstName = "Test",
            LastName = "User"
        };

        // Act: 呼叫註冊
        var result = await service.RegisterAsync(request);

        // Assert: 驗證結果
        Assert.True(result.Success);
        Assert.Equal("註冊成功", result.Message);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("newuser@test.com", result.User.Email);
        
        // 驗證使用者已存入資料庫
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        Assert.NotNull(user);
        Assert.True(user.IsActive);
        Assert.StartsWith("pbkdf2_sha256$", user.Password); // 密碼已加密
    }

    /// <summary>
    /// 測試重複 Email 註冊失敗
    /// </summary>
    [Fact]
    public async Task Register_DuplicateEmail_ReturnsFailed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<AuthService>();
        var service = new AuthService(context, config, logger);

        // 先建立一個使用者
        var existingUser = new UserEntity
        {
            Email = "existing@test.com",
            Password = "hashed_password",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "existing@test.com",
            Password = "Password123"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("此 Email 已被註冊", result.Message);
        Assert.Null(result.Token);
    }

    /// <summary>
    /// 測試使用正確帳號密碼登入成功
    /// </summary>
    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccessWithToken()
    {
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<AuthService>();
        var service = new AuthService(context, config, logger);

        // 先註冊使用者
        var registerRequest = new RegisterRequest
        {
            Email = "login@test.com",
            Password = "Password123"
        };
        await service.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login@test.com",
            Password = "Password123"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("登入成功", result.Message);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        
        // 驗證 LastLogin 已更新
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "login@test.com");
        Assert.NotNull(user);
        Assert.NotNull(user.LastLogin);
    }

    /// <summary>
    /// 測試密碼錯誤登入失敗
    /// </summary>
    [Fact]
    public async Task Login_WrongPassword_ReturnsFailed()
    {
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<AuthService>();
        var service = new AuthService(context, config, logger);

        var registerRequest = new RegisterRequest
        {
            Email = "test@test.com",
            Password = "CorrectPassword123"
        };
        await service.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "test@test.com",
            Password = "WrongPassword123"
        };

        var result = await service.LoginAsync(loginRequest);

        Assert.False(result.Success);
        Assert.Equal("Email 或密碼錯誤", result.Message);
        Assert.Null(result.Token);
    }

    /// <summary>
    /// 測試不存在使用者登入失敗
    /// </summary>
    [Fact]
    public async Task Login_NonExistentUser_ReturnsFailed()
    {
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<AuthService>();
        var service = new AuthService(context, config, logger);

        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "Password123"
        };

        var result = await service.LoginAsync(loginRequest);

        Assert.False(result.Success);
        Assert.Equal("Email 或密碼錯誤", result.Message);
    }

    /// <summary>
    /// 測試停用帳號登入失敗
    /// 注意：AuthService 現在為資安考量，不會透露帳號是否停用
    /// </summary>
    [Fact]
    public async Task Login_InactiveUser_ReturnsFailed()
    {
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<AuthService>();
        var service = new AuthService(context, config, logger);

        var user = new UserEntity
        {
            Email = "inactive@test.com",
            Password = "pbkdf2_sha256$600000$dGVzdHNhbHQ=$dGVzdGhhc2g=",
            IsActive = false,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "inactive@test.com",
            Password = "Password123"
        };

        var result = await service.LoginAsync(loginRequest);

        Assert.False(result.Success);
        // 資安考量，停用帳號與錯誤密碼統一回傳
        Assert.Equal("Email 或密碼錯誤", result.Message);
        Assert.Null(result.Token);
    }

    /// <summary>
    /// 測試依 ID 取得使用者資料成功
    /// </summary>
    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsUserDto()
    {
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<AuthService>();
        var service = new AuthService(context, config, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed_password",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await service.GetUserByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("test@test.com", result.Email);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("User", result.LastName);
    }

    /// <summary>
    /// 測試依 ID 取得不存在使用者資料回傳 null
    /// </summary>
    [Fact]
    public async Task GetUserById_NonExistentUser_ReturnsNull()
    {
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<AuthService>();
        var service = new AuthService(context, config, logger);

        var result = await service.GetUserByIdAsync(999);

        Assert.Null(result);
    }
}
