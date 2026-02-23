using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;
using TravelTrip.Core.DTOs;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace TravelTripApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // 1. 註冊功能
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.FailureResult("輸入資料無效"));

        var request = new RegisterRequest
        {
            Email = dto.Email,
            Username = dto.Email,
            FirstName = dto.Name,
            Password = dto.Password
        };

        var result = await _authService.RegisterAsync(request);

        if (result.Success)
            return Ok(ApiResponse<object>.SuccessResult(new { token = result.Token, user = result.User }, "註冊成功"));
        
        return BadRequest(ApiResponse<object>.FailureResult(result.Message));
    }

    // 2. 登入功能
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.FailureResult("輸入資料無效"));

        var request = new LoginRequest
        {
            Email = dto.Email,
            Password = dto.Password
        };

        var result = await _authService.LoginAsync(request);

        if (result.Success)
        {
            return Ok(ApiResponse<object>.SuccessResult(new
            {
                token = result.Token,
                user = result.User
            }, "登入成功"));
        }

        return Unauthorized(ApiResponse<object>.FailureResult(result.Message));
    }
}