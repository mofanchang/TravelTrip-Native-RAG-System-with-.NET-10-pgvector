using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;
using TravelTrip.Core.DTOs;

namespace TravelTripApi.Services;

/// <summary>
/// AI 聊天機器人服務
/// 整合 RAG（檢索增強生成）
/// </summary>
public class ChatbotService
{
    private readonly TravelDbContext _context;
    private readonly ILogger<ChatbotService> _logger;
    private readonly TripRagService _ragService;

    public ChatbotService(
        TravelDbContext context,
        ILogger<ChatbotService> logger,
        TripRagService ragService)
    {
        _context = context;
        _logger = logger;
        _ragService = ragService;
    }

    /// <summary>
    /// 處理聊天請求
    /// </summary>
    public async Task<ChatResponse> ProcessChatAsync(ChatRequest request, int? userId = null)
    {
        try
        {
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            _logger.LogInformation($"處理聊天請求: {request.Message}, SessionId: {sessionId}");

            // 1. 使用 RAG 搜尋相似行程
            var ragResults = await _ragService.SearchSimilarTripsAsync(request.Message, request.TopK);

            // 2. 生成回應
            var aiResponse = GenerateAiResponse(request.Message, ragResults);

            // 3. 轉換為推薦格式
            var recommendations = ragResults.Select(trip => new TripRecommendation
            {
                Id = trip.Id,
                Name = trip.Name,
                Description = trip.Description,
                Country = trip.Country,
                City = trip.City,
                Duration = trip.Duration,
                Keywords = trip.Keywords,
                Price = trip.Price,
                Image = trip.Image,
                SimilarityScore = trip.SimilarityScore,
                MatchType = GetMatchType(trip.SimilarityScore),
                Link = $"/trips/{trip.Id}/"
            }).ToList();

            // 4. 儲存聊天記錄
            await SaveChatHistoryAsync(userId, sessionId, request.Message, aiResponse, recommendations);

            return new ChatResponse
            {
                Query = request.Message,
                AiResponse = aiResponse,
                RecommendedTrips = recommendations,
                Total = recommendations.Count,
                Method = "RAG",
                SessionId = sessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"處理聊天請求失敗: {request.Message}");
            return new ChatResponse
            {
                Query = request.Message,
                AiResponse = "抱歉，我現在遇到了一些問題。請稍後再試，或者試著用不同的方式描述您的需求。",
                RecommendedTrips = new List<TripRecommendation>(),
                Total = 0,
                Method = "ERROR"
            };
        }
    }

    /// <summary>
    /// 使用進階 AI 處理聊天（需要外部 AI 服務）
    /// </summary>
    public async Task<ChatResponse> ProcessChatWithAiAsync(
        ChatRequest request,
        IChatCompletionService chatService,
        int? userId = null)
    {
        try
        {
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            var ragResults = await _ragService.SearchSimilarTripsAsync(request.Message, 3);
            var context = BuildRagContext(ragResults);

            var systemPrompt = @"你是一個專業的旅遊顧問助手。根據使用者的需求，從提供的行程資料中推薦最合適的旅遊方案。
請用親切、專業的語氣回答，並針對使用者的需求提供具體建議。";

            var userPrompt = $@"使用者需求：{request.Message}

可用的行程資料：
{context}

請根據使用者需求，推薦最合適的行程，並說明推薦理由。";

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(userPrompt);

            var aiResponse = await chatService.GetChatMessageContentAsync(chatHistory);

            var recommendations = ragResults.Select(trip => new TripRecommendation
            {
                Id = trip.Id,
                Name = trip.Name,
                Description = trip.Description,
                Country = trip.Country,
                City = trip.City,
                Duration = trip.Duration,
                Keywords = trip.Keywords,
                Price = trip.Price,
                Image = trip.Image,
                SimilarityScore = trip.SimilarityScore,
                MatchType = GetMatchType(trip.SimilarityScore),
                Link = $"/trips/{trip.Id}/"
            }).ToList();

            await SaveChatHistoryAsync(userId, sessionId, request.Message, aiResponse.Content ?? "", recommendations);

            return new ChatResponse
            {
                Query = request.Message,
                AiResponse = aiResponse.Content ?? "",
                RecommendedTrips = recommendations,
                Total = recommendations.Count,
                Method = "RAG + AI",
                SessionId = sessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI 聊天處理失敗，回退到基本 RAG");
            return await ProcessChatAsync(request, userId);
        }
    }

    /// <summary>
    /// 取得聊天歷史
    /// </summary>
    public async Task<List<ChatHistoryDto>> GetChatHistoryAsync(string sessionId)
    {
        var history = await _context.ChatHistories
            .Where(ch => ch.SessionId == sessionId)
            .OrderBy(ch => ch.CreatedAt)
            .ToListAsync();

        return history.Select(ch => new ChatHistoryDto
        {
            Id = ch.Id,
            SessionId = ch.SessionId,
            UserMessage = ch.UserMessage,
            AiResponse = ch.AiResponse,
            CreatedAt = ch.CreatedAt,
            RecommendedTripIds = string.IsNullOrEmpty(ch.RecommendedTripIds)
                ? null
                : JsonSerializer.Deserialize<List<int>>(ch.RecommendedTripIds),
            SimilarityScore = ch.SimilarityScore
        }).ToList();
    }

    // ─── Private Helpers ─────────────────────────────────

    private string GenerateAiResponse(string userMessage, List<TripSearchResultDto> trips)
    {
        if (trips.Count == 0)
            return "抱歉，目前沒有找到完全符合您需求的行程。您可以試試調整搜尋條件，或者瀏覽我們的熱門行程！";

        var bestTrip = trips.First();

        if (bestTrip.SimilarityScore >= 0.8)
            return $"太好了！我找到了非常符合您需求的行程：「{bestTrip.Name}」。這是一個為期 {bestTrip.Duration} 天的 {bestTrip.Country}{bestTrip.City} 之旅：{bestTrip.Description}。非常推薦給您！";
        else if (bestTrip.SimilarityScore >= 0.6)
            return $"根據您的需求，我為您推薦「{bestTrip.Name}」這個行程。這是一個 {bestTrip.Duration} 天的 {bestTrip.Country}{bestTrip.City} 之旅。我認為這個行程的特色非常符合您的描述，建議您可以參考看看！";
        else
            return $"我為您找到了「{bestTrip.Name}」，一個 {bestTrip.Duration} 天的 {bestTrip.Country}{bestTrip.City} 行程。雖然與您的描述有些微差異，但這是目前最接近的推薦選項，希望您會喜歡！";
    }

    private string BuildRagContext(List<TripSearchResultDto> trips)
    {
        return string.Join("\n\n", trips.Select((trip, index) =>
            $"行程 {index + 1}：\n" +
            $"- 名稱：{trip.Name}\n" +
            $"- 國家/城市：{trip.Country} / {trip.City}\n" +
            $"- 天數：{trip.Duration} 天\n" +
            $"- 價格：NT$ {trip.Price:N0}\n" +
            $"- 關鍵字：{trip.Keywords}\n" +
            $"- 描述：{trip.Description}\n" +
            $"- 相似度：{trip.SimilarityScore:P0}"
        ));
    }

    private async Task SaveChatHistoryAsync(
        int? userId, string sessionId, string userMessage,
        string aiResponse, List<TripRecommendation> recommendations)
    {
        try
        {
            var chatHistory = new ChatHistoryEntity
            {
                UserId = userId,
                SessionId = sessionId,
                UserMessage = userMessage,
                AiResponse = aiResponse,
                RecommendedTripIds = JsonSerializer.Serialize(recommendations.Select(r => r.Id).ToList()),
                SimilarityScore = recommendations.FirstOrDefault()?.SimilarityScore,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatHistories.Add(chatHistory);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "儲存聊天記錄失敗");
        }
    }

    private string GetMatchType(double similarityScore)
    {
        if (similarityScore >= 0.8) return "RAG_HIGH_QUALITY";
        if (similarityScore >= 0.6) return "RAG_MEDIUM_QUALITY";
        if (similarityScore >= 0.4) return "RAG_LOW_QUALITY";
        return "FALLBACK";
    }
}
