using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelTrip.Core.Models;

/// <summary>
/// 密碼重置 Token 實體
/// 用於儲存密碼重置請求的臨時 Token
/// </summary>
[Table("password_reset_tokens")]
public class PasswordResetTokenEntity
{
    /// <summary>
    /// Token ID
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// 使用者 ID
    /// </summary>
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// 重置 Token（UUID）
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token 過期時間
    /// </summary>
    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 是否已使用
    /// </summary>
    [Column("is_used")]
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// 建立時間
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 使用時間
    /// </summary>
    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    // 導航屬性
    /// <summary>
    /// 關聯的使用者
    /// </summary>
    [ForeignKey("UserId")]
    public UserEntity? User { get; set; }
}
