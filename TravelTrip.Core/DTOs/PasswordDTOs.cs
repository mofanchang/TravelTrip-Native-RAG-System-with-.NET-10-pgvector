using System.ComponentModel.DataAnnotations;

namespace TravelTrip.Core.DTOs;

/// <summary>
/// 忘記密碼請求
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// Email 地址
    /// </summary>
    [Required(ErrorMessage = "Email 不能為空")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// 重置密碼請求
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// 重置 Token
    /// </summary>
    [Required(ErrorMessage = "Token 不能為空")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 新密碼
    /// </summary>
    [Required(ErrorMessage = "新密碼不能為空")]
    [MinLength(6, ErrorMessage = "密碼長度至少 6 個字元")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 確認密碼
    /// </summary>
    [Required(ErrorMessage = "確認密碼不能為空")]
    [Compare("NewPassword", ErrorMessage = "兩次密碼輸入不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 變更密碼請求（已登入使用者）
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 目前密碼
    /// </summary>
    [Required(ErrorMessage = "目前密碼不能為空")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密碼
    /// </summary>
    [Required(ErrorMessage = "新密碼不能為空")]
    [MinLength(6, ErrorMessage = "密碼長度至少 6 個字元")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 確認新密碼
    /// </summary>
    [Required(ErrorMessage = "確認密碼不能為空")]
    [Compare("NewPassword", ErrorMessage = "兩次密碼輸入不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 密碼操作回應
/// </summary>
public class PasswordResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
