namespace TravelTrip.Core.Models;

/// <summary>
/// 管理員角色枚舉
/// </summary>
public enum AdminRole
{
    /// <summary>超級管理員 - 擁有所有權限</summary>
    SuperAdmin = 1,

    /// <summary>管理員 - 可以管理行程和使用者</summary>
    Admin = 2,

    /// <summary>編輯員 - 只能編輯內容，不能刪除和管理使用者</summary>
    Editor = 3,

    /// <summary>檢視員 - 只能查看數據，不能修改</summary>
    Viewer = 4
}

/// <summary>
/// 管理操作類型
/// </summary>
public enum AuditActionType
{
    Create,
    Read,
    Update,
    Delete,
    Login,
    Logout,
    Export
}

/// <summary>
/// 管理員審計日誌 - 記錄所有管理員操作
/// </summary>
public class AdminAuditLogEntity
{
    public int Id { get; set; }

    /// <summary>操作的管理員 ID</summary>
    public int AdminId { get; set; }

    /// <summary>操作類型</summary>
    public AuditActionType Action { get; set; }

    /// <summary>被操作的實體類型（trip, user, booking 等）</summary>
    public string EntityType { get; set; } = "";

    /// <summary>被操作的實體 ID</summary>
    public int EntityId { get; set; }

    /// <summary>修改前的值（JSON 格式）</summary>
    public string? OldValue { get; set; }

    /// <summary>修改後的值（JSON 格式）</summary>
    public string? NewValue { get; set; }

    /// <summary>操作者的 IP 地址</summary>
    public string? IpAddress { get; set; }

    /// <summary>用戶代理（瀏覽器資訊）</summary>
    public string? UserAgent { get; set; }

    /// <summary>操作時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>操作結果（成功/失敗）</summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>失敗時的錯誤訊息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>管理員導航屬性</summary>
    public UserEntity? Admin { get; set; }
}
