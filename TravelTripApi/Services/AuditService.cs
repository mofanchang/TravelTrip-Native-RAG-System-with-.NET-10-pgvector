using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;
using System.Security.Claims;

namespace TravelTripApi.Services;

/// <summary>
/// 審計日誌服務 - 記錄所有管理員操作
/// </summary>
public class AuditService
{
    private readonly TravelDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        TravelDbContext context,
        ILogger<AuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 記錄管理員操作
    /// </summary>
    public async Task LogActionAsync(
        int adminId,
        AuditActionType action,
        string entityType,
        int entityId,
        object? oldValue = null,
        object? newValue = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            var auditLog = new AdminAuditLogEntity
            {
                AdminId = adminId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            _context.AdminAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"審計日誌已記錄: {action} {entityType}({entityId}) 由管理員 {adminId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄審計日誌時發生錯誤");
            // 不要拋出異常，以免影響主業務邏輯
        }
    }

    /// <summary>
    /// 取得指定管理員的操作日誌
    /// </summary>
    public async Task<List<AdminAuditLogEntity>> GetAdminLogsAsync(int adminId, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        return await _context.AdminAuditLogs
            .Where(l => l.AdminId == adminId && l.CreatedAt >= startDate)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 取得所有操作日誌
    /// </summary>
    public async Task<List<AdminAuditLogEntity>> GetAllLogsAsync(
        int? adminId = null,
        string? entityType = null,
        AuditActionType? action = null,
        int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var query = _context.AdminAuditLogs
            .Where(l => l.CreatedAt >= startDate)
            .AsQueryable();

        if (adminId.HasValue)
            query = query.Where(l => l.AdminId == adminId);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(l => l.EntityType == entityType);

        if (action.HasValue)
            query = query.Where(l => l.Action == action);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 記錄管理員登入
    /// </summary>
    public async Task LogLoginAsync(int adminId)
    {
        await LogActionAsync(adminId, AuditActionType.Login, "admin_login", adminId);

        // 同時更新使用者的最後登入時間
        var user = await _context.Users.FindAsync(adminId);
        if (user != null)
        {
            user.AdminLastLogin = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 記錄管理員登出
    /// </summary>
    public async Task LogLogoutAsync(int adminId)
    {
        await LogActionAsync(adminId, AuditActionType.Logout, "admin_logout", adminId);
    }

    /// <summary>
    /// 從 HttpContext 中提取管理員 ID
    /// </summary>
    public int? GetCurrentAdminId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userIdClaim = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdClaim?.Value, out var adminId))
            return adminId;

        return null;
    }
}
