using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelTrip.Core.DTOs;
using TravelTripApi.Services;

namespace TravelTripApi.Controllers;

/// <summary>
/// 密碼管理控制器
/// 處理密碼重置、變更等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PasswordController : BaseController
{
    private readonly PasswordResetService _passwordResetService;
    private readonly ILogger<PasswordController> _logger;

    public PasswordController(
        PasswordResetService passwordResetService,
        ILogger<PasswordController> logger)
    {
        _passwordResetService = passwordResetService;
        _logger = logger;
    }

    /// <summary>
    /// 忘記密碼 - 發送重置連結到 Email
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<PasswordResponse>.FailureResult("請提供有效的 Email 地址"));

        _logger.LogInformation("收到忘記密碼請求: {Email}", request.Email);

        var result = await _passwordResetService.RequestPasswordResetAsync(request);
        return Ok(ApiResponse<PasswordResponse>.SuccessResult(result));
    }

    /// <summary>
    /// 驗證重置 Token 是否有效
    /// </summary>
    [AllowAnonymous]
    [HttpGet("validate-token")]
    public async Task<IActionResult> ValidateToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(ApiResponse<PasswordResponse>.FailureResult("Token 不能為空"));

        var (isValid, message, _) = await _passwordResetService.ValidateResetTokenAsync(token);

        return Ok(ApiResponse<PasswordResponse>.SuccessResult(new PasswordResponse
        {
            Success = isValid,
            Message = message
        }));
    }

    /// <summary>
    /// 重置密碼
    /// </summary>
    [AllowAnonymous]
    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<PasswordResponse>.FailureResult("輸入資料不正確"));

        _logger.LogInformation("收到重置密碼請求: Token={TokenPreview}...", request.Token[..8]);

        var result = await _passwordResetService.ResetPasswordAsync(request);

        if (result.Success)
            return Ok(ApiResponse<PasswordResponse>.SuccessResult(result));
        
        return BadRequest(ApiResponse<PasswordResponse>.FailureResult(result.Message));
    }

    /// <summary>
    /// 變更密碼（已登入使用者）
    /// </summary>
    [Authorize]
    [HttpPost("change")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<PasswordResponse>.FailureResult("輸入資料不正確"));

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<PasswordResponse>.FailureResult("無效的使用者"));

        _logger.LogInformation("收到變更密碼請求: UserId={UserId}", userId);

        var result = await _passwordResetService.ChangePasswordAsync(userId.Value, request);

        if (result.Success)
            return Ok(ApiResponse<PasswordResponse>.SuccessResult(result));
        
        return BadRequest(ApiResponse<PasswordResponse>.FailureResult(result.Message));
    }

    /// <summary>
    /// 健康檢查
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.SuccessResult(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Password API"
        }));
    }
}
