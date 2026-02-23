using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelTrip.Core.Models;

/// <summary>
/// 購物車主表
/// 對應 Django 的 Cart
/// </summary>
[Table("cart_cart")]
public class CartEntity
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
    /// 購物車建立時間
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 導航屬性
    [ForeignKey("UserId")]
    public UserEntity? User { get; set; }

    public ICollection<CartItemEntity>? CartItems { get; set; }
}

/// <summary>
/// 購物車項目
/// 對應 Django 的 CartItem
/// </summary>
[Table("cart_cartitem")]
public class CartItemEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// 所屬購物車 ID（外鍵）
    /// </summary>
    [Required]
    [Column("cart_id")]
    public int CartId { get; set; }

    /// <summary>
    /// 行程 ID（外鍵）
    /// </summary>
    [Required]
    [Column("trip_id")]
    public int TripId { get; set; }

    /// <summary>
    /// 數量（限制 1-10）
    /// </summary>
    [Column("quantity")]
    [Range(1, 10)]
    public int Quantity { get; set; } = 1;

    // 導航屬性
    [ForeignKey("CartId")]
    public CartEntity? Cart { get; set; }

    [ForeignKey("TripId")]
    public TripEntity? Trip { get; set; }
}
