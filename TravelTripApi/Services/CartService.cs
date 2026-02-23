using Microsoft.EntityFrameworkCore;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;
using TravelTrip.Core.DTOs;

namespace TravelTripApi.Services;

/// <summary>
/// 購物車服務
/// </summary>
public class CartService
{
    private readonly TravelDbContext _context;
    private readonly ILogger<CartService> _logger;

    public CartService(TravelDbContext context, ILogger<CartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 取得或建立使用者的購物車
    /// </summary>
    private async Task<CartEntity> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems!)
                .ThenInclude(ci => ci.Trip)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new CartEntity
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"建立新購物車: UserId={userId}, CartId={cart.Id}");
        }

        return cart;
    }

    /// <summary>
    /// 加入商品到購物車
    /// </summary>
    public async Task<(bool Success, string Message, CartDto? Cart)> AddToCartAsync(
        int userId,
        AddToCartRequest request)
    {
        try
        {
            // 驗證行程是否存在
            var trip = await _context.Trips.FindAsync(request.TripId);
            if (trip == null)
            {
                return (false, "行程不存在", null);
            }

            // 驗證數量
            if (request.Quantity < 1 || request.Quantity > 10)
            {
                return (false, "數量必須在 1-10 之間", null);
            }

            // 檢查庫存
            if (trip.AvailableSeats < request.Quantity)
            {
                return (false, $"座位不足，目前僅剩 {trip.AvailableSeats} 個座位", null);
            }

            // 取得或建立購物車
            var cart = await GetOrCreateCartAsync(userId);

            // 檢查是否已存在此商品
            var existingItem = cart.CartItems?
                .FirstOrDefault(ci => ci.TripId == request.TripId);

            if (existingItem != null)
            {
                // 更新數量
                var newQuantity = existingItem.Quantity + request.Quantity;
                if (newQuantity > 10)
                {
                    return (false, "每個行程最多只能加入 10 個", null);
                }
                
                if (trip.AvailableSeats < newQuantity)
                {
                    return (false, $"座位不足，目前僅剩 {trip.AvailableSeats} 個座位", null);
                }

                existingItem.Quantity = newQuantity;
                _logger.LogInformation($"更新購物車項目: CartItemId={existingItem.Id}, NewQuantity={newQuantity}");
            }
            else
            {
                // 新增項目
                var cartItem = new CartItemEntity
                {
                    CartId = cart.Id,
                    TripId = request.TripId,
                    Quantity = request.Quantity
                };
                _context.CartItems.Add(cartItem);
                _logger.LogInformation($"加入購物車: TripId={request.TripId}, Quantity={request.Quantity}");
            }

            await _context.SaveChangesAsync();

            // 重新載入購物車資料
            var updatedCart = await GetCartAsync(userId);

            return (true, "已加入購物車", updatedCart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"加入購物車失敗: UserId={userId}, TripId={request.TripId}");
            return (false, "加入購物車失敗", null);
        }
    }

    /// <summary>
    /// 取得購物車
    /// </summary>
    public async Task<CartDto?> GetCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems!)
                .ThenInclude(ci => ci.Trip)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            // 回傳空購物車
            return new CartDto
            {
                Id = 0,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItemDto>(),
                TotalPrice = 0,
                TotalItems = 0
            };
        }

        return MapToCartDto(cart);
    }

    /// <summary>
    /// 更新購物車項目數量
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateCartItemAsync(
        int userId,
        int cartItemId,
        UpdateCartItemRequest request)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Trip)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return (false, "購物車不存在");
            }

            var cartItem = cart.CartItems?.FirstOrDefault(ci => ci.Id == cartItemId);
            if (cartItem == null)
            {
                return (false, "購物車項目不存在");
            }

            // 驗證數量
            if (request.Quantity < 1 || request.Quantity > 10)
            {
                return (false, "數量必須在 1-10 之間");
            }

            // 檢查庫存
            if (cartItem.Trip != null && cartItem.Trip.AvailableSeats < request.Quantity)
            {
                return (false, $"座位不足，目前僅剩 {cartItem.Trip.AvailableSeats} 個座位");
            }

            cartItem.Quantity = request.Quantity;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"更新購物車項目: CartItemId={cartItemId}, Quantity={request.Quantity}");

            return (true, "購物車項目已更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新購物車項目失敗: CartItemId={cartItemId}");
            return (false, "更新失敗");
        }
    }

    /// <summary>
    /// 從購物車移除項目
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveFromCartAsync(int userId, int cartItemId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return (false, "購物車不存在");
            }

            var cartItem = cart.CartItems?.FirstOrDefault(ci => ci.Id == cartItemId);
            if (cartItem == null)
            {
                return (false, "購物車項目不存在");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"移除購物車項目: CartItemId={cartItemId}");

            return (true, "已從購物車移除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"移除購物車項目失敗: CartItemId={cartItemId}");
            return (false, "移除失敗");
        }
    }

    /// <summary>
    /// 清空購物車
    /// </summary>
    public async Task<(bool Success, string Message)> ClearCartAsync(int userId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return (true, "購物車已是空的");
            }

            if (cart.CartItems != null && cart.CartItems.Any())
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"清空購物車: CartId={cart.Id}");
            }

            return (true, "購物車已清空");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"清空購物車失敗: UserId={userId}");
            return (false, "清空購物車失敗");
        }
    }

    /// <summary>
    /// 從購物車建立訂單（內部使用）
    /// </summary>
    public async Task<List<BookingItemRequest>> GetCartItemsForCheckoutAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems!)
                .ThenInclude(ci => ci.Trip)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart?.CartItems == null || !cart.CartItems.Any())
        {
            return new List<BookingItemRequest>();
        }

        return cart.CartItems
            .Select(ci => new BookingItemRequest
            {
                TripId = ci.TripId,
                Quantity = ci.Quantity
            })
            .ToList();
    }

    /// <summary>
    /// 將 CartEntity 轉換為 CartDto
    /// </summary>
    private CartDto MapToCartDto(CartEntity cart)
    {
        var items = cart.CartItems?
            .Where(ci => ci.Trip != null)
            .Select(ci => new CartItemDto
            {
                Id = ci.Id,
                TripId = ci.TripId,
                TripName = ci.Trip!.Name,
                Quantity = ci.Quantity,
                Price = ci.Trip.Price,
                Subtotal = ci.Trip.Price * ci.Quantity,
                Trip = new TripSearchResultDto
                {
                    Id = ci.Trip.Id,
                    Name = ci.Trip.Name,
                    Description = ci.Trip.Description,
                    Country = ci.Trip.Country,
                    City = ci.Trip.City,
                    Duration = ci.Trip.Duration,
                    Keywords = ci.Trip.Keywords,
                    Price = ci.Trip.Price,
                    Image = ci.Trip.Image,
                    StartDate = ci.Trip.StartDate,
                    EndDate = ci.Trip.EndDate,
                    AvailableSeats = ci.Trip.AvailableSeats
                }
            })
            .ToList() ?? new List<CartItemDto>();

        var totalPrice = items.Sum(i => i.Subtotal);
        var totalItems = items.Sum(i => i.Quantity);

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            CreatedAt = cart.CreatedAt,
            Items = items,
            TotalPrice = totalPrice,
            TotalItems = totalItems
        };
    }
}
