using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelTrip.Core.DTOs;
using TravelTripApi.Services;

namespace TravelTripApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BookingsController : BaseController
{
    private readonly BookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(BookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// 建立訂單
    /// POST /api/bookings
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        // 驗證輸入
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(ApiResponse<object>.FailureResult("訂單項目不能為空"));

        foreach (var item in request.Items.Where(item => item.Quantity <= 0))
        {
            return BadRequest(ApiResponse<object>.FailureResult("數量必須大於 0"));
        }

        _logger.LogInformation("建立訂單請求: UserId={UserId}, ItemsCount={ItemsCount}", userId, request.Items.Count);

        var (success, message, booking) = await _bookingService.CreateBookingAsync(userId.Value, request);

        if (success)
            return CreatedAtAction(nameof(GetBooking), new { id = booking!.Id }, ApiResponse<BookingDto>.SuccessResult(booking, "訂單建立成功"));
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 取得單一訂單
    /// GET /api/bookings/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        var booking = await _bookingService.GetBookingByIdAsync(id, userId.Value);

        if (booking == null)
            return NotFound(ApiResponse<object>.FailureResult("訂單不存在"));

        return Ok(ApiResponse<BookingDto>.SuccessResult(booking));
    }

    /// <summary>
    /// 取得我的所有訂單
    /// GET /api/bookings/my-bookings?skip=0&amp;take=20
    /// </summary>
    [HttpGet("my-bookings")]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        var result = await _bookingService.GetUserBookingsAsync(userId.Value, skip, take);

        return Ok(ApiResponse<BookingListResponse>.SuccessResult(result));
    }

    /// <summary>
    /// 更新訂單狀態
    /// PUT /api/bookings/{id}/status
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateBookingStatus(
        int id,
        [FromBody] UpdateBookingStatusRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        // 驗證狀態值
        var validStatuses = new[] { "Pending", "Completed", "Cancelled" };
        if (!validStatuses.Contains(request.Status))
            return BadRequest(ApiResponse<object>.FailureResult("無效的訂單狀態"));

        var (success, message) = await _bookingService.UpdateBookingStatusAsync(id, userId.Value, request);

        if (success)
            return Ok(ApiResponse<object>.SuccessResult(null, message));
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 取消訂單
    /// POST /api/bookings/{id}/cancel
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        var (success, message) = await _bookingService.CancelBookingAsync(id, userId.Value);

        if (success)
            return Ok(ApiResponse<object>.SuccessResult(null, message));
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 健康檢查
    /// GET /api/bookings/health
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.SuccessResult(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Bookings API"
        }));
    }
}
