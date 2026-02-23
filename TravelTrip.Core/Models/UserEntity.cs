using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // 如果需要忽略循環引用

namespace TravelTrip.Core.Models;

[Table("accounts_customuser")] // 對應妳 Django 的資料表名稱
public class UserEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("is_superuser")]
    public bool IsSuperuser { get; set; }

    [MaxLength(150)]
    [Column("username")]
    public string? Username { get; set; }

    [MaxLength(150)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(150)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("is_staff")]
    public bool IsStaff { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("date_joined")]
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 管理員角色（null = 普通使用者）
    /// </summary>
    [Column("admin_role")]
    public AdminRole? AdminRole { get; set; }

    /// <summary>
    /// 最後登入時間
    /// </summary>
    [Column("admin_last_login")]
    public DateTime? AdminLastLogin { get; set; }

    // 關聯到訂單，使用 JsonIgnore 防止 API 回傳時發生無限迴圈
    [JsonIgnore]
    public ICollection<BookingEntity>? Bookings { get; set; }

    /// <summary>
    /// 該管理員執行的審計日誌
    /// </summary>
    [JsonIgnore]
    public ICollection<AdminAuditLogEntity>? AuditLogs { get; set; }
}