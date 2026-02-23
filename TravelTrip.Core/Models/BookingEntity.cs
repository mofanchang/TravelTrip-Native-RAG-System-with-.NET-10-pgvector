using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelTrip.Core.Models;

/// <summary>
/// 訂單主表
/// 對應 Django 的 Booking
/// </summary>
[Table("bookings_booking")]
public class BookingEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// 使用者 ID（外鍵）
    /// </summary>
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// 訂單建立時間
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 訂單總金額
    /// </summary>
    [Column("total", TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    /// <summary>
    /// 訂單狀態（Pending, Completed, Cancelled）
    /// </summary>
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// PayPal 交易 ID
    /// </summary>
    [MaxLength(100)]
    [Column("paypal_transaction_id")]
    public string? PaypalTransactionId { get; set; }

    /// <summary>
    /// 付款完成時間
    /// </summary>
    [Column("payment_completed_at")]
    public DateTime? PaymentCompletedAt { get; set; }

    // 導航屬性
    [ForeignKey("UserId")]
    public UserEntity? User { get; set; }

    public ICollection<BookingItemEntity>? BookingItems { get; set; }
}

/// <summary>
/// 訂單項目（訂單明細）
/// 用於記錄每個訂單中包含的行程
/// </summary>
[Table("bookings_bookingitem")]
public class BookingItemEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// 所屬訂單 ID（外鍵）
    /// </summary>
    [Required]
    [Column("booking_id")]
    public int BookingId { get; set; }

    /// <summary>
    /// 行程 ID（外鍵）
    /// </summary>
    [Required]
    [Column("trip_id")]
    public int TripId { get; set; }

    /// <summary>
    /// 預訂人數
    /// </summary>
    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// 單價（下單時的價格，避免價格變動影響歷史訂單）
    /// </summary>
    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    /// <summary>
    /// 小計（數量 * 單價）
    /// </summary>
    [Column("subtotal", TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    // 導航屬性
    [ForeignKey("BookingId")]
    public BookingEntity? Booking { get; set; }

    [ForeignKey("TripId")]
    public TripEntity? Trip { get; set; }
}
