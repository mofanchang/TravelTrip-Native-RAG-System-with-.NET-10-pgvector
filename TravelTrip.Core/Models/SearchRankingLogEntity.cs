using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelTrip.Core.Models;

/// <summary>
/// 搜尋排名日誌 (Analytics)
/// 用於記錄搜尋詞與行程之間的評分細節，供後續分析排名準確度使用
/// </summary>
[Table("search_ranking_logs")]
public class SearchRankingLogEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("query_text")]
    public string QueryText { get; set; } = string.Empty;

    [Column("trip_name")]
    public string TripName { get; set; } = string.Empty;

    [Column("semantic_score")]
    public double SemanticScore { get; set; }

    [Column("keyword_bonus")]
    public double KeywordBonus { get; set; }

    [Column("final_score")]
    public double FinalScore { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
