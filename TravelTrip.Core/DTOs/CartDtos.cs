namespace TravelTrip.Core.DTOs;

/// <summary>
/// 加入購物車請求
/// </summary>
public class AddToCartRequest
{
    public int TripId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// 更新購物車項目請求
/// </summary>
public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}

/// <summary>
/// 購物車回應 DTO
/// </summary>
public class CartDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public int TotalItems { get; set; }
}

/// <summary>
/// 購物車項目 DTO
/// </summary>
public class CartItemDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
    public TripSearchResultDto? Trip { get; set; }
}
