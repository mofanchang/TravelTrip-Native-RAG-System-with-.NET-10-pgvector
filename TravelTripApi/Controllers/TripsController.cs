using Microsoft.AspNetCore.Mvc;
using TravelTripApi.Services;
using TravelTrip.Core.DTOs;

namespace TravelTripApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiController]
[Route("api/[controller]")]
public class TripsController : BaseController
{
    private readonly TripRagService _ragService;
    private readonly ILogger<TripsController> _logger;

    public TripsController(TripRagService ragService, ILogger<TripsController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有行程
    /// GET /api/trips
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("取得所有行程");
        var results = await _ragService.GetAllTripsAsync();
        return Ok(ApiResponse<List<TripSearchResultDto>>.SuccessResult(results));
    }

    /// <summary>
    /// 取得單一行程詳情
    /// GET /api/trips/1
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("取得行程詳情: {TripId}", id);
        var trip = await _ragService.GetTripByIdAsync(id);
        if (trip == null) 
            return NotFound(ApiResponse<object>.FailureResult("找不到該行程"));
        
        return Ok(ApiResponse<TripSearchResultDto>.SuccessResult(trip));
    }

    /// <summary>
    /// 使用 RAG 搜尋相似行程
    /// GET /api/trips/search?query=日本海灘度假&amp;topK=5
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query, 
        [FromQuery] int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) 
            return BadRequest(ApiResponse<object>.FailureResult("請提供搜尋字串"));

        if (topK < 1 || topK > 20)
            return BadRequest(ApiResponse<object>.FailureResult("TopK 必須在 1-20 之間"));

        _logger.LogInformation("搜尋行程: Query={Query}, TopK={TopK}", query, topK);
        var results = await _ragService.SearchSimilarTripsAsync(query, topK);
        
        return Ok(ApiResponse<List<TripSearchResultDto>>.SuccessResult(results));
    }

    /// <summary>
    /// 根據主題推薦行程
    /// GET /api/trips/recommend?theme=海灘&amp;topK=5
    /// </summary>
    [HttpGet("recommend")]
    public async Task<IActionResult> Recommend(
        [FromQuery] string theme,
        [FromQuery] int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(theme))
            return BadRequest(ApiResponse<object>.FailureResult("請提供主題"));

        if (topK < 1 || topK > 20)
            return BadRequest(ApiResponse<object>.FailureResult("TopK 必須在 1-20 之間"));

        _logger.LogInformation("推薦行程: Theme={Theme}, TopK={TopK}", theme, topK);
        var results = await _ragService.RecommendTripsByThemeAsync(theme, topK);
        
        return Ok(ApiResponse<List<TripSearchResultDto>>.SuccessResult(results));
    }

    /// <summary>
    /// 刷新 RAG 系統（需要 Admin 及以上）
    /// POST /api/trips/rag/refresh
    /// </summary>
    [Authorize]
    [AuthorizeRole(AdminRole.Admin)]
    [HttpPost("rag/refresh")]
    public async Task<IActionResult> RefreshRag()
    {
        _logger.LogInformation("刷新 RAG 系統");
        var success = await _ragService.RefreshRagAsync();
        
        if (success)
            return Ok(ApiResponse<object>.SuccessResult(null, "RAG 系統已刷新"));
        
        return BadRequest(ApiResponse<object>.FailureResult("RAG 系統刷新失敗"));
    }

    /// <summary>
    /// 初始化 RAG 系統（需要 Admin 及以上）
    /// POST /api/trips/rag/initialize
    /// </summary>
    [Authorize]
    [AuthorizeRole(AdminRole.Admin)]
    [HttpPost("rag/initialize")]
    public async Task<IActionResult> InitializeRag()
    {
        _logger.LogInformation("初始化 RAG 系統");
        var success = await _ragService.InitializeRagAsync();
        
        if (success)
            return Ok(ApiResponse<object>.SuccessResult(null, "RAG 系統已初始化"));
        
        return BadRequest(ApiResponse<object>.FailureResult("RAG 系統初始化失敗"));
    }
}
