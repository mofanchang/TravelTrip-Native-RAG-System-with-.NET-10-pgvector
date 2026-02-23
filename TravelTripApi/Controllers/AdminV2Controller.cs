using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;
using TravelTripApi.Services;

namespace TravelTripApi.Controllers;

/// <summary>
/// 生產級管理後台 API
/// 需要 JWT 認證 + 管理員角色檢查
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]  // ✅ 所有端點都需要認證
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminV2Controller : BaseController
{
    private readonly TravelDbContext _context;
    private readonly AuditService _auditService;
    private readonly FileUploadService _fileUploadService;
    private readonly ILogger<AdminV2Controller> _logger;

    public AdminV2Controller(
        TravelDbContext context,
        AuditService auditService,
        FileUploadService fileUploadService,
        ILogger<AdminV2Controller> logger)
    {
        _context = context;
        _auditService = auditService;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// 取得當前管理員資訊
    /// GET /api/adminv2/me
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentAdmin()
    {
        var adminId = GetCurrentUserId();
        if (!adminId.HasValue)
            return Unauthorized(ApiResponse<object>.FailureResult("未登入"));

        var user = await _context.Users.FindAsync(adminId);
        if (user?.AdminRole == null)
            return Forbid();

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AdminRole,
            user.AdminLastLogin
        }));
    }

    // ==================== 行程管理 ====================

    /// <summary>
    /// 取得所有行程（需要 Viewer 及以上）
    /// GET /api/adminv2/trips
    /// </summary>
    [HttpGet("trips")]
    [AuthorizeRole(AdminRole.Viewer)]
    public async Task<ActionResult> GetAllTrips()
    {
        try
        {
            var trips = await _context.Trips
                .OrderByDescending(t => t.Id)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Country,
                    t.City,
                    t.Price,
                    t.Image,
                    t.AvailableSeats,
                    t.Duration,
                    t.StartDate,
                    t.EndDate,
                    t.Description,
                    t.Keywords
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResult(trips, count: trips.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得行程列表失敗");
            return StatusCode(500, ApiResponse<object>.FailureResult(ex.Message));
        }
    }

    /// <summary>
    /// 建立新行程（需要 Admin 及以上）
    /// POST /api/adminv2/trips
    /// </summary>
    [HttpPost("trips")]
    [AuthorizeRole(AdminRole.Admin)]
    public async Task<ActionResult> CreateTrip([FromBody] CreateTripRequest request)
    {
        var adminId = GetCurrentUserId();
        if (!adminId.HasValue)
            return Unauthorized();

        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var trip = new TripEntity
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Image = request.Image,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                AvailableSeats = request.AvailableSeats,
                Location = request.Location,
                Duration = request.Duration,
                Country = request.Country,
                City = request.City,
                Keywords = request.Keywords ?? ""
            };

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(adminId.Value, AuditActionType.Create, "trip", trip.Id, newValue: trip);
            _logger.LogInformation("建立行程成功: {TripName}", trip.Name);

            return Ok(ApiResponse<object>.SuccessResult(new { tripId = trip.Id }, "行程建立成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立行程失敗");
            await _auditService.LogActionAsync(adminId.Value, AuditActionType.Create, "trip", 0, isSuccess: false, errorMessage: ex.Message);
            return StatusCode(500, ApiResponse<object>.FailureResult(ex.Message));
        }
    }

    /// <summary>
    /// 更新行程（需要 Editor 及以上）
    /// PUT /api/adminv2/trips/{id}
    /// </summary>
    [HttpPut("trips/{id}")]
    [AuthorizeRole(AdminRole.Editor)]
    public async Task<ActionResult> UpdateTrip(int id, [FromBody] UpdateTripRequest request)
    {
        var adminId = GetCurrentUserId();
        if (!adminId.HasValue)
            return Unauthorized();

        try
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
                return NotFound(ApiResponse<object>.FailureResult("行程不存在"));

            var oldValue = new { trip.Name, trip.Price, trip.Description, trip.Image };

            trip.Name = request.Name ?? trip.Name;
            trip.Description = request.Description ?? trip.Description;
            trip.Price = request.Price > 0 ? request.Price : trip.Price;
            trip.Image = request.Image ?? trip.Image;
            trip.StartDate = request.StartDate != default ? request.StartDate : trip.StartDate;
            trip.EndDate = request.EndDate != default ? request.EndDate : trip.EndDate;
            trip.AvailableSeats = request.AvailableSeats >= 0 ? request.AvailableSeats : trip.AvailableSeats;
            trip.Location = request.Location ?? trip.Location;
            trip.Duration = request.Duration > 0 ? request.Duration : trip.Duration;
            trip.Country = request.Country ?? trip.Country;
            trip.City = request.City ?? trip.City;
            trip.Keywords = request.Keywords ?? trip.Keywords;

            _context.Trips.Update(trip);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(adminId.Value, AuditActionType.Update, "trip", id, oldValue: oldValue, newValue: new { trip.Name, trip.Price, trip.Description, trip.Image });
            return Ok(ApiResponse<object>.SuccessResult(null, "行程更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新行程失敗");
            await _auditService.LogActionAsync(adminId.Value, AuditActionType.Update, "trip", id, isSuccess: false, errorMessage: ex.Message);
            return StatusCode(500, ApiResponse<object>.FailureResult(ex.Message));
        }
    }

    /// <summary>
    /// 刪除行程（只有 SuperAdmin）
    /// DELETE /api/adminv2/trips/{id}
    /// </summary>
    [HttpDelete("trips/{id}")]
    [AuthorizeRole(AdminRole.SuperAdmin)]
    public async Task<ActionResult> DeleteTrip(int id)
    {
        var adminId = GetCurrentUserId();
        if (!adminId.HasValue)
            return Unauthorized();

        try
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
                return NotFound(ApiResponse<object>.FailureResult("行程不存在"));

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(adminId.Value, AuditActionType.Delete, "trip", id, oldValue: trip);
            _logger.LogInformation("刪除行程成功: {TripName}", trip.Name);

            return Ok(ApiResponse<object>.SuccessResult(null, "行程刪除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除行程失敗");
            await _auditService.LogActionAsync(adminId.Value, AuditActionType.Delete, "trip", id, isSuccess: false, errorMessage: ex.Message);
            return StatusCode(500, ApiResponse<object>.FailureResult(ex.Message));
        }
    }

    // ==================== 使用者管理 ====================

    /// <summary>
    /// 取得所有使用者（需要 Admin 及以上）
    /// GET /api/adminv2/users
    /// </summary>
    [HttpGet("users")]
    [AuthorizeRole(AdminRole.Admin)]
    public async Task<ActionResult> GetAllUsers()
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.AdminRole == null)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    u.DateJoined,
                    BookingCount = u.Bookings != null ? u.Bookings.Count : 0
                })
                .OrderByDescending(u => u.DateJoined)
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResult(users, count: users.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得使用者列表失敗");
            return StatusCode(500, ApiResponse<object>.FailureResult(ex.Message));
        }
    }

    /// <summary>
    /// 停用/啟用使用者（需要 Admin 及以上）
    /// PATCH /api/adminv2/users/{id}/toggle-active
    /// </summary>
    [HttpPatch("users/{id}/toggle-active")]
    [AuthorizeRole(AdminRole.Admin)]
    public async Task<ActionResult> ToggleUserActive(int id)
    {
        var adminId = GetCurrentUserId();
        if (!adminId.HasValue)
            return Unauthorized();

        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(ApiResponse<object>.FailureResult("使用者不存在"));

            if (user.AdminRole != null)
                return BadRequest(ApiResponse<object>.FailureResult("無法停用管理員帳號"));

            user.IsActive = !user.IsActive;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(adminId.Value, AuditActionType.Update, "user", id, oldValue: new { IsActive = !user.IsActive }, newValue: new { IsActive = user.IsActive });
            return Ok(ApiResponse<object>.SuccessResult(new { isActive = user.IsActive }, $"使用者已{(user.IsActive ? "啟用" : "停用")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切換使用者狀態失敗");
            await _auditService.LogActionAsync(adminId.Value, AuditActionType.Update, "user", id, isSuccess: false, errorMessage: ex.Message);
            return StatusCode(500, ApiResponse<object>.FailureResult(ex.Message));
        }
    }

    // ==================== 審計日誌 ====================

    /// <summary>
    /// 取得審計日誌（只有 SuperAdmin）
    /// GET /api/adminv2/audit-logs?days=30&amp;adminId=1
    /// </summary>
    [HttpGet("audit-logs")]
    [AuthorizeRole(AdminRole.SuperAdmin)]
    public async Task<ActionResult> GetAuditLogs(int? adminId = null, string? entityType = null, int days = 30)
    {
        try
        {
            var logs = await _auditService.GetAllLogsAsync(adminId, entityType, null, days);

            return Ok(ApiResponse<object>.SuccessResult(logs.Select(l => new
            {
                l.Id,
                l.AdminId,
                AdminEmail = l.Admin?.Email,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.CreatedAt,
                l.IsSuccess,
                l.IpAddress,
                l.ErrorMessage
            }).ToList(), count: logs.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得審計日誌失敗");
            return StatusCode(500, ApiResponse<object>.FailureResult(ex.Message));
        }
    }

    // ==================== 統計資訊 ====================

    /// <summary>
    /// 取得統計資訊（需要 Viewer 及以上）
    /// GET /api/adminv2/stats
    /// </summary>
    [HttpGet("stats")]
    [AuthorizeRole(AdminRole.Viewer)]
    public async Task<ActionResult> GetStats()
    {
        try
        {
            var stats = new
            {
                TotalTrips = await _context.Trips.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(u => u.AdminRole == null),
                TotalAdmins = await _context.Users.CountAsync(u => u.AdminRole != null),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive && u.AdminRole == null),
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalRevenue = await _context.Bookings.SumAsync(b => (decimal?)b.Total) ?? 0
            };

            return Ok(ApiResponse<object>.SuccessResult(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得統計資訊失敗");
            return StatusCode(500, ApiResponse<object>.FailureResult(ex.Message));
        }
    }
}
