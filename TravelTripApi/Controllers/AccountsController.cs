using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelTrip.Core.DTOs;
using TravelTripApi.Services;
using BCrypt.Net;

namespace TravelTripApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : BaseController
{
    private readonly AuthService _authService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(AuthService authService, ILogger<AccountsController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 使用者註冊
    /// POST /api/accounts/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<AuthResponse>.FailureResult("輸入資料無效"));

        _logger.LogInformation("收到註冊請求: {Email}", request.Email);

        var result = await _authService.RegisterAsync(request);

        if (result.Success)
            return Ok(ApiResponse<AuthResponse>.SuccessResult(result, "註冊成功"));
        
        return BadRequest(ApiResponse<AuthResponse>.FailureResult(result.Message));
    }

    /// <summary>
    /// 使用者登入
    /// POST /api/accounts/login
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<AuthResponse>.FailureResult("輸入資料無效"));

        _logger.LogInformation("收到登入請求: {Email}", request.Email);

        var result = await _authService.LoginAsync(request);

        if (result.Success)
            return Ok(ApiResponse<AuthResponse>.SuccessResult(result, "登入成功"));
        
        return Unauthorized(ApiResponse<AuthResponse>.FailureResult(result.Message));
    }

    /// <summary>
    /// 取得當前使用者資料（需要認證）
    /// GET /api/accounts/profile
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的 Token"));

        var user = await _authService.GetUserByIdAsync(userId.Value);

        if (user == null)
            return NotFound(ApiResponse<object>.FailureResult("使用者不存在"));

        return Ok(ApiResponse<UserDto>.SuccessResult(user));
    }

    /// <summary>
    /// 測試認證是否有效
    /// GET /api/accounts/test-auth
    /// </summary>
    [Authorize]
    [HttpGet("test-auth")]
    public IActionResult TestAuth()
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        var userId = GetCurrentUserId();

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            userId = userId,
            email = userEmail,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        }, "認證成功"));
    }

    /// <summary>
    /// 健康檢查
    /// GET /api/accounts/health
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.SuccessResult(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Accounts API"
        }));
    }
}
