using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace TravelTrip.Core.Models;

[Table("trips_trip")]
public class TripEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(500)]
    [Column("image")]
    public string Image { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("available_seats")]
    public int AvailableSeats { get; set; }

    [MaxLength(100)]
    [Column("location")]
    public string Location { get; set; } = string.Empty;

    [Column("duration")]
    public int Duration { get; set; }

    [MaxLength(100)]
    [Column("country")]
    public string Country { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("city")]
    public string City { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("keywords")]
    public string Keywords { get; set; } = string.Empty;

    /// <summary>
    /// 向量嵌入（使用 pgvector 的 Vector 類型）
    /// 384 維度對應本地中文 Embedding 模型
    /// 可為空，如果為空則表示尚未生成 Embedding
    /// </summary>
    [Column("embedding")]
    public Vector? Embedding { get; set; }

    /// <summary>
    /// 取得用於生成 Embedding 的完整文本
    /// </summary>
    public string GetEmbeddingText()
    {
        return $"{Name} {Country} {City} {Keywords} {Description} {Location}";
    }
}
