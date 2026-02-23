using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelTrip.Core.Models;

/// <summary>
/// 聊天記錄
/// 用於儲存使用者與 AI 的對話歷史
/// </summary>
[Table("chatbot_chathistory")]
public class ChatHistoryEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// 使用者 ID（可選，匿名使用者為 null）
    /// </summary>
    [Column("user_id")]
    public int? UserId { get; set; }

    /// <summary>
    /// Session ID（用於追蹤對話）
    /// </summary>
    [MaxLength(100)]
    [Column("session_id")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 使用者訊息
    /// </summary>
    [Required]
    [Column("user_message")]
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// AI 回應
    /// </summary>
    [Column("ai_response")]
    public string AiResponse { get; set; } = string.Empty;

    /// <summary>
    /// 推薦的行程 ID 列表（JSON 格式）
    /// </summary>
    [Column("recommended_trip_ids")]
    public string? RecommendedTripIds { get; set; }

    /// <summary>
    /// RAG 搜尋相似度分數
    /// </summary>
    [Column("similarity_score")]
    public double? SimilarityScore { get; set; }

    /// <summary>
    /// 對話建立時間
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 導航屬性
    [ForeignKey("UserId")]
    public UserEntity? User { get; set; }
}
