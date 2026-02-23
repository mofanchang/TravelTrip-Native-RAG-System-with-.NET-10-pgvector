using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TravelTrip.Core.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace TravelTripApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : BaseController
{
    private readonly TravelDbContext _context;

    public UserController(TravelDbContext context) => _context = context;

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(ApiResponse<object>.FailureResult("未授權"));

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(ApiResponse<object>.FailureResult("找不到使用者"));

        return Ok(ApiResponse<object>.SuccessResult(new { 
            name = user.FirstName, 
            email = user.Email 
        }));
    }
}