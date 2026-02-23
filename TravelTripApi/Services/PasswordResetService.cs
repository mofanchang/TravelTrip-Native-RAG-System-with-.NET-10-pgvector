using Microsoft.EntityFrameworkCore;
using TravelTrip.Core.Data;
using TravelTrip.Core.DTOs;
using TravelTrip.Core.Models;
using System.Security.Cryptography;

namespace TravelTripApi.Services;

/// <summary>
/// 密碼重置服務
/// 處理密碼重置的業務邏輯
/// </summary>
public class PasswordResetService
{
    private readonly TravelDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        TravelDbContext context,
        EmailService emailService,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// 請求密碼重置（發送重置郵件）
    /// </summary>
    public async Task<PasswordResponse> RequestPasswordResetAsync(ForgotPasswordRequest request)
    {
        try
        {
            // 查找使用者
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            // 即使找不到使用者，也返回成功訊息（安全考量，不洩漏使用者是否存在）
            if (user == null)
            {
                _logger.LogWarning($"密碼重置請求：Email 不存在 - {request.Email}");
                return new PasswordResponse
                {
                    Success = true,
                    Message = "如果該 Email 存在，我們已發送重置連結到您的信箱"
                };
            }

            // 檢查使用者是否啟用
            if (!user.IsActive)
            {
                _logger.LogWarning($"密碼重置請求：帳號已停用 - {request.Email}");
                return new PasswordResponse
                {
                    Success = false,
                    Message = "此帳號已被停用，請聯繫客服"
                };
            }

            // 產生重置 Token（UUID）
            var resetToken = Guid.NewGuid().ToString();

            // 儲存重置 Token（1 小時有效）
            var tokenEntity = new PasswordResetTokenEntity
            {
                UserId = user.Id,
                Token = resetToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            // 發送重置郵件
            var emailSent = await _emailService.SendPasswordResetEmailAsync(
                user.Email,
                resetToken,
                user.FirstName ?? user.Email
            );

            if (!emailSent)
            {
                _logger.LogError($"發送密碼重置郵件失敗: {user.Email}");
            }

            _logger.LogInformation($"密碼重置請求成功: {user.Email}");

            return new PasswordResponse
            {
                Success = true,
                Message = "如果該 Email 存在，我們已發送重置連結到您的信箱"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "請求密碼重置失敗");
            return new PasswordResponse
            {
                Success = false,
                Message = "系統錯誤，請稍後再試"
            };
        }
    }

    /// <summary>
    /// 驗證重置 Token 是否有效
    /// </summary>
    public async Task<(bool IsValid, string Message, PasswordResetTokenEntity? Token)> ValidateResetTokenAsync(string token)
    {
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (resetToken == null)
        {
            return (false, "無效的重置連結", null);
        }

        if (resetToken.IsUsed)
        {
            return (false, "此重置連結已被使用", null);
        }

        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return (false, "重置連結已過期，請重新申請", null);
        }

        if (resetToken.User == null || !resetToken.User.IsActive)
        {
            return (false, "帳號不存在或已被停用", null);
        }

        return (true, "Token 有效", resetToken);
    }

    /// <summary>
    /// 重置密碼
    /// </summary>
    public async Task<PasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            // 驗證 Token
            var (isValid, message, resetToken) = await ValidateResetTokenAsync(request.Token);

            if (!isValid || resetToken == null)
            {
                return new PasswordResponse
                {
                    Success = false,
                    Message = message
                };
            }

            // 更新使用者密碼
            var user = resetToken.User!;
            user.Password = HashPassword(request.NewPassword);

            // 標記 Token 為已使用
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 發送密碼變更通知
            await _emailService.SendPasswordChangedNotificationAsync(
                user.Email,
                user.FirstName ?? user.Email
            );

            _logger.LogInformation($"密碼重置成功: {user.Email}");

            return new PasswordResponse
            {
                Success = true,
                Message = "密碼重置成功，請使用新密碼登入"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置密碼失敗");
            return new PasswordResponse
            {
                Success = false,
                Message = "系統錯誤，請稍後再試"
            };
        }
    }

    /// <summary>
    /// 變更密碼（已登入使用者）
    /// </summary>
    public async Task<PasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        try
        {
            // 查找使用者
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return new PasswordResponse
                {
                    Success = false,
                    Message = "使用者不存在"
                };
            }

            // 驗證目前密碼
            if (!VerifyPassword(request.CurrentPassword, user.Password))
            {
                _logger.LogWarning($"變更密碼失敗：目前密碼錯誤 - {user.Email}");
                return new PasswordResponse
                {
                    Success = false,
                    Message = "目前密碼錯誤"
                };
            }

            // 更新密碼
            user.Password = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            // 發送密碼變更通知
            await _emailService.SendPasswordChangedNotificationAsync(
                user.Email,
                user.FirstName ?? user.Email
            );

            _logger.LogInformation($"密碼變更成功: {user.Email}");

            return new PasswordResponse
            {
                Success = true,
                Message = "密碼變更成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "變更密碼失敗");
            return new PasswordResponse
            {
                Success = false,
                Message = "系統錯誤，請稍後再試"
            };
        }
    }

    /// <summary>
    /// 清理過期的重置 Token（定期執行）
    /// </summary>
    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.PasswordResetTokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow || t.IsUsed)
                .Where(t => t.CreatedAt < DateTime.UtcNow.AddDays(-7)) // 保留 7 天內的記錄
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.PasswordResetTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"清理過期 Token: {expiredTokens.Count} 筆");
            }

            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理過期 Token 失敗");
            return 0;
        }
    }

    /// <summary>
    /// 雜湊密碼（使用 PBKDF2）
    /// 相容於 Django 的 pbkdf2_sha256 格式
    /// </summary>
    private string HashPassword(string password)
    {
        const int iterations = 600000;
        const int saltSize = 16;
        const int hashSize = 32;

        var salt = new byte[saltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashSize);
        var saltBase64 = Convert.ToBase64String(salt);
        var hashBase64 = Convert.ToBase64String(hash);
        return $"pbkdf2_sha256${iterations}${saltBase64}${hashBase64}";
    }

    /// <summary>
    /// 驗證密碼
    /// 相容於 Django 的 pbkdf2_sha256 格式
    /// </summary>
    private bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            if (!hashedPassword.StartsWith("pbkdf2_sha256$"))
            {
                // 支援 BCrypt (用於測試或舊資料)
                try {
                    if (BCrypt.Net.BCrypt.Verify(password, hashedPassword)) return true;
                } catch { }
                return false;
            }
            
            var parts = hashedPassword.Split('$');
            if (parts.Length != 4) return false;

            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var storedHash = Convert.FromBase64String(parts[3]);

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, storedHash.Length);
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }
}
