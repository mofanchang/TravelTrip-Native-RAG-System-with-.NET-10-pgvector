using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelTrip.Core.DTOs;
using TravelTripApi.Services;

namespace TravelTripApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiController]
[Route("api/[controller]")]
public class ChatController : BaseController
{
    private readonly ChatbotService _chatbotService;
    private readonly TripRagService _ragService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ChatbotService chatbotService,
        TripRagService ragService,
        ILogger<ChatController> logger)
    {
        _chatbotService = chatbotService;
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// 聊天 API（主要端點）
    /// POST /api/chat
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        // 驗證輸入
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(ApiResponse<object>.FailureResult("請輸入查詢內容"));

        if (request.TopK.HasValue && (request.TopK < 1 || request.TopK > 20))
            return BadRequest(ApiResponse<object>.FailureResult("TopK 必須在 1-20 之間"));

        // 取得使用者 ID（如果已登入）
        var userId = GetCurrentUserId();

        _logger.LogInformation("收到聊天請求: {Message}, UserId: {UserId}", request.Message, userId?.ToString() ?? "匿名");

        // 處理聊天
        var response = await _chatbotService.ProcessChatAsync(request, userId);

        return Ok(ApiResponse<ChatResponse>.SuccessResult(response));
    }

    /// <summary>
    /// 簡易推薦 API（只回傳行程列表）
    /// GET /api/chat/recommend?query=想去日本賞櫻
    /// </summary>
    [HttpGet("recommend")]
    public async Task<IActionResult> Recommend([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<object>.FailureResult("請提供查詢內容"));

        var request = new ChatRequest
        {
            Message = query,
            TopK = 5
        };

        var userId = GetCurrentUserId();
        var response = await _chatbotService.ProcessChatAsync(request, userId);

        return Ok(ApiResponse<ChatResponse>.SuccessResult(response));
    }

    /// <summary>
    /// 取得聊天歷史
    /// GET /api/chat/history/{sessionId}
    /// </summary>
    [HttpGet("history/{sessionId}")]
    public async Task<IActionResult> GetHistory(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(ApiResponse<object>.FailureResult("SessionId 不能為空"));

        var history = await _chatbotService.GetChatHistoryAsync(sessionId);

        return Ok(ApiResponse<List<ChatHistoryDto>>.SuccessResult(history));
    }

    /// <summary>
    /// 重建 RAG 索引（需要 Admin 及以上）
    /// POST /api/chat/rebuild-index
    /// </summary>
    [Authorize]
    [AuthorizeRole(AdminRole.Admin)]
    [HttpPost("rebuild-index")]
    public async Task<IActionResult> RebuildIndex()
    {
        try
        {
            _logger.LogInformation("開始重建 RAG 索引");

            var success = await _ragService.RefreshRagAsync();

            if (success)
            {
                _logger.LogInformation("RAG 索引重建成功");
                return Ok(ApiResponse<object>.SuccessResult(null, "RAG 索引重建成功"));
            }
            
            _logger.LogError("RAG 索引重建失敗");
            return StatusCode(500, ApiResponse<object>.FailureResult("RAG 索引重建失敗"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重建 RAG 索引失敗");
            return StatusCode(500, ApiResponse<object>.FailureResult($"重建索引失敗: {ex.Message}"));
        }
    }

    /// <summary>
    /// 測試 RAG 搜尋（除錯用，需要 Admin 及以上）
    /// GET /api/chat/test-rag?query=日本&amp;topK=3
    /// </summary>
    [Authorize]
    [AuthorizeRole(AdminRole.Admin)]
    [HttpGet("test-rag")]
    public async Task<IActionResult> TestRag(
        [FromQuery] string query,
        [FromQuery] int topK = 3)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<object>.FailureResult("請提供查詢內容"));

        if (topK < 1 || topK > 20)
            return BadRequest(ApiResponse<object>.FailureResult("TopK 必須在 1-20 之間"));

        var results = await _ragService.SearchSimilarTripsAsync(query, topK);

        var data = new
        {
            query,
            topK,
            results = results.Select(r => new
            {
                r.Id,
                r.Name,
                r.Country,
                r.City,
                r.SimilarityScore,
                similarityPercent = $"{r.SimilarityScore * 100:F1}%"
            })
        };

        return Ok(ApiResponse<object>.SuccessResult(data));
    }

    /// <summary>
    /// 健康檢查
    /// GET /api/chat/health
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.SuccessResult(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Chatbot API"
        }));
    }
}