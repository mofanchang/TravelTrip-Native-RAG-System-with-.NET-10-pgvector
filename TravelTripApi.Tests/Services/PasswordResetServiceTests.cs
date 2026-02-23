using Xunit;
using TravelTripApi.Services;
using TravelTripApi.DTOs;
using TravelTripApi.Models;
using TravelTripApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TravelTripApi.Tests.Services;

/// <summary>
/// 密碼重置服務測試
/// </summary>
public class PasswordResetServiceTests
{
    [Fact]
    public async Task RequestPasswordReset_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<PasswordResetService>();
        var emailLogger = TestHelper.CreateMockLogger<EmailService>();
        
        var emailService = new EmailService(config, emailLogger);
        var service = new PasswordResetService(context, emailService, logger);

        // 新增測試使用者
        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed_password",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new ForgotPasswordRequest { Email = "test@test.com" };

        // Act
        var result = await service.RequestPasswordResetAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("如果該 Email 存在，我們已發送重置連結到您的信箱", result.Message);
        
        // 驗證 Token 已建立
        var token = await context.PasswordResetTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);
        Assert.NotNull(token);
        Assert.False(token.IsUsed);
        Assert.True(token.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task RequestPasswordReset_NonExistentEmail_ReturnsSuccessWithoutCreatingToken()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<PasswordResetService>();
        var emailLogger = TestHelper.CreateMockLogger<EmailService>();
        
        var emailService = new EmailService(config, emailLogger);
        var service = new PasswordResetService(context, emailService, logger);

        var request = new ForgotPasswordRequest { Email = "nonexistent@test.com" };

        // Act
        var result = await service.RequestPasswordResetAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("如果該 Email 存在，我們已發送重置連結到您的信箱", result.Message);
        
        // 驗證沒有建立 Token（安全考量：不洩漏使用者是否存在）
        var tokenCount = await context.PasswordResetTokens.CountAsync();
        Assert.Equal(0, tokenCount);
    }

    [Fact]
    public async Task RequestPasswordReset_InactiveUser_ReturnsFailed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<PasswordResetService>();
        var emailLogger = TestHelper.CreateMockLogger<EmailService>();
        
        var emailService = new EmailService(config, emailLogger);
        var service = new PasswordResetService(context, emailService, logger);

        // 新增停用的使用者
        var user = new UserEntity
        {
            Email = "inactive@test.com",
            Password = "hashed_password",
            IsActive = false,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new ForgotPasswordRequest { Email = "inactive@test.com" };

        // Act
        var result = await service.RequestPasswordResetAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("此帳號已被停用，請聯繫客服", result.Message);
    }

    [Fact]
    public async Task ValidateResetToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<PasswordResetService>();
        var emailLogger = TestHelper.CreateMockLogger<EmailService>();
        
        var emailService = new EmailService(config, emailLogger);
        var service = new PasswordResetService(context, emailService, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed_password",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = new PasswordResetTokenEntity
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        context.PasswordResetTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var (isValid, message, _) = await service.ValidateResetTokenAsync(token.Token);

        // Assert
        Assert.True(isValid);
        Assert.Equal("Token 有效", message);
    }

    [Fact]
    public async Task ValidateResetToken_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<PasswordResetService>();
        var emailLogger = TestHelper.CreateMockLogger<EmailService>();
        
        var emailService = new EmailService(config, emailLogger);
        var service = new PasswordResetService(context, emailService, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed_password",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = new PasswordResetTokenEntity
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // 已過期
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        context.PasswordResetTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var (isValid, message, _) = await service.ValidateResetTokenAsync(token.Token);

        // Assert
        Assert.False(isValid);
        Assert.Equal("重置連結已過期，請重新申請", message);
    }

    [Fact]
    public async Task ValidateResetToken_UsedToken_ReturnsFalse()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<PasswordResetService>();
        var emailLogger = TestHelper.CreateMockLogger<EmailService>();
        
        var emailService = new EmailService(config, emailLogger);
        var service = new PasswordResetService(context, emailService, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed_password",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = new PasswordResetTokenEntity
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true, // 已使用
            CreatedAt = DateTime.UtcNow
        };
        context.PasswordResetTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var (isValid, message, _) = await service.ValidateResetTokenAsync(token.Token);

        // Assert
        Assert.False(isValid);
        Assert.Equal("此重置連結已被使用", message);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_UpdatesPasswordAndMarksTokenAsUsed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<PasswordResetService>();
        var emailLogger = TestHelper.CreateMockLogger<EmailService>();
        
        var emailService = new EmailService(config, emailLogger);
        var service = new PasswordResetService(context, emailService, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "old_hashed_password",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = new PasswordResetTokenEntity
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        context.PasswordResetTokens.Add(token);
        await context.SaveChangesAsync();

        var request = new ResetPasswordRequest
        {
            Token = token.Token,
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        // Act
        var result = await service.ResetPasswordAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("密碼重置成功，請使用新密碼登入", result.Message);
        
        // 驗證密碼已更新
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.NotEqual("old_hashed_password", updatedUser.Password);
        Assert.StartsWith("pbkdf2_sha256$", updatedUser.Password);
        
        // 驗證 Token 已標記為使用
        var updatedToken = await context.PasswordResetTokens.FindAsync(token.Id);
        Assert.NotNull(updatedToken);
        Assert.True(updatedToken.IsUsed);
        Assert.NotNull(updatedToken.UsedAt);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsFailed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var config = TestHelper.CreateMockConfiguration();
        var logger = TestHelper.CreateMockLogger<PasswordResetService>();
        var emailLogger = TestHelper.CreateMockLogger<EmailService>();
        
        var emailService = new EmailService(config, emailLogger);
        var service = new PasswordResetService(context, emailService, logger);

        var request = new ResetPasswordRequest
        {
            Token = "invalid-token",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        // Act
        var result = await service.ResetPasswordAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("無效的重置連結", result.Message);
    }
}
