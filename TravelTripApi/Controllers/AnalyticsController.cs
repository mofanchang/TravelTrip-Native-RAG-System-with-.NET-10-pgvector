using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;

namespace TravelTripApi.Controllers;

/// <summary>
/// RAG 分析控制器
/// 提供搜尋排名日誌的查詢介面 (功能對標 Python 版本的 /debug/logs)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[AuthorizeRole(AdminRole.Admin)]
public class AnalyticsController : BaseController
{
    private readonly TravelDbContext _context;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(TravelDbContext context, ILogger<AnalyticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 取得最新的搜尋排名日誌 (需要 Admin)
    /// </summary>
    [HttpGet("ranking-logs")]
    public async Task<IActionResult> GetRankingLogs(int limit = 20)
    {
        var logs = await _context.SearchRankingLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
        
        return Ok(ApiResponse<List<SearchRankingLogEntity>>.SuccessResult(logs, count: logs.Count));
    }

    /// <summary>
    /// 清除所有分析日誌 (需要 Admin)
    /// </summary>
    [HttpDelete("ranking-logs")]
    public async Task<IActionResult> ClearLogs()
    {
        var logs = await _context.SearchRankingLogs.ToListAsync();
        _context.SearchRankingLogs.RemoveRange(logs);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("已清除所有搜尋排名日誌");
        return Ok(ApiResponse<object>.SuccessResult(null, "日誌已清除"));
    }
}
