namespace TravelTrip.Core.DTOs;

/// <summary>
/// 聊天請求
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public int TopK { get; set; } = 1;
}

/// <summary>
/// 聊天回應
/// </summary>
public class ChatResponse
{
    public string Query { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public List<TripRecommendation> RecommendedTrips { get; set; } = new();
    public int Total { get; set; }
    public string Method { get; set; } = "RAG";
    public string? SessionId { get; set; }
}

/// <summary>
/// 行程推薦結果
/// </summary>
public class TripRecommendation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Keywords { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public string MatchType { get; set; } = "RAG";
    public string Link { get; set; } = string.Empty;
}

/// <summary>
/// 聊天歷史回應
/// </summary>
public class ChatHistoryDto
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<int>? RecommendedTripIds { get; set; }
    public double? SimilarityScore { get; set; }
}
