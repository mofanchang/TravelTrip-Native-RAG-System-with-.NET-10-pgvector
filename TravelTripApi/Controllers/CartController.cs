using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelTrip.Core.DTOs;
using TravelTripApi.Services;

namespace TravelTripApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController : BaseController
{
    private readonly CartService _cartService;
    private readonly BookingService _bookingService;
    private readonly ILogger<CartController> _logger;

    public CartController(
        CartService cartService,
        BookingService bookingService,
        ILogger<CartController> logger)
    {
        _cartService = cartService;
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// 取得購物車
    /// GET /api/cart
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        var cart = await _cartService.GetCartAsync(userId.Value);
        return Ok(ApiResponse<CartDto>.SuccessResult(cart));
    }

    /// <summary>
    /// 加入商品到購物車
    /// POST /api/cart/add
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        // 驗證輸入
        if (request.TripId <= 0)
            return BadRequest(ApiResponse<object>.FailureResult("無效的行程 ID"));

        if (request.Quantity < 1 || request.Quantity > 10)
            return BadRequest(ApiResponse<object>.FailureResult("數量必須在 1-10 之間"));

        _logger.LogInformation("加入購物車請求: UserId={UserId}, TripId={TripId}, Quantity={Quantity}", userId, request.TripId, request.Quantity);

        var (success, message, cart) = await _cartService.AddToCartAsync(userId.Value, request);

        if (success)
            return Ok(ApiResponse<CartDto>.SuccessResult(cart!, message));
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 更新購物車項目數量
    /// PUT /api/cart/items/{itemId}
    /// </summary>
    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> UpdateCartItem(
        int itemId,
        [FromBody] UpdateCartItemRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        if (request.Quantity < 1 || request.Quantity > 10)
            return BadRequest(ApiResponse<object>.FailureResult("數量必須在 1-10 之間"));

        var (success, message) = await _cartService.UpdateCartItemAsync(userId.Value, itemId, request);

        if (success)
            return Ok(ApiResponse<object>.SuccessResult(null, message));
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 從購物車移除項目
    /// DELETE /api/cart/items/{itemId}
    /// </summary>
    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> RemoveFromCart(int itemId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        var (success, message) = await _cartService.RemoveFromCartAsync(userId.Value, itemId);

        if (success)
            return Ok(ApiResponse<object>.SuccessResult(null, message));
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 清空購物車
    /// DELETE /api/cart/clear
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        var (success, message) = await _cartService.ClearCartAsync(userId.Value);

        if (success)
            return Ok(ApiResponse<object>.SuccessResult(null, message));
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 從購物車結帳（建立訂單並清空購物車）
    /// POST /api/cart/checkout
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResult("無效的使用者"));

        _logger.LogInformation("購物車結帳請求: UserId={UserId}", userId);

        // 取得購物車項目
        var cartItems = await _cartService.GetCartItemsForCheckoutAsync(userId.Value);

        if (cartItems.Count == 0)
            return BadRequest(ApiResponse<object>.FailureResult("購物車是空的"));

        // 建立訂單
        var createBookingRequest = new CreateBookingRequest
        {
            Items = cartItems
        };

        var (success, message, booking) = await _bookingService.CreateBookingAsync(userId.Value, createBookingRequest);

        if (success)
        {
            // 清空購物車
            await _cartService.ClearCartAsync(userId.Value);
            _logger.LogInformation("結帳成功: UserId={UserId}, BookingId={BookingId}", userId, booking!.Id);

            return Ok(ApiResponse<BookingDto>.SuccessResult(booking, "結帳成功"));
        }

        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 健康檢查
    /// GET /api/cart/health
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.SuccessResult(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Cart API"
        }));
    }
}
