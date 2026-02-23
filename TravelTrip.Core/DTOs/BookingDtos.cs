namespace TravelTrip.Core.DTOs;

/// <summary>
/// 建立訂單請求
/// </summary>
public class CreateBookingRequest
{
    /// <summary>
    /// 購物車項目（行程 ID 列表）
    /// </summary>
    public List<BookingItemRequest> Items { get; set; } = new();
}

/// <summary>
/// 訂單項目請求
/// </summary>
public class BookingItemRequest
{
    public int TripId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// 訂單回應 DTO
/// </summary>
public class BookingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaypalTransactionId { get; set; }
    public DateTime? PaymentCompletedAt { get; set; }
    public List<BookingItemDto> Items { get; set; } = new();
    public UserDto? User { get; set; }
}

/// <summary>
/// 訂單項目 DTO
/// </summary>
public class BookingItemDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
    public TripSearchResultDto? Trip { get; set; }
}

/// <summary>
/// 更新訂單狀態請求
/// </summary>
public class UpdateBookingStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? PaypalTransactionId { get; set; }
}

/// <summary>
/// 訂單列表回應
/// </summary>
public class BookingListResponse
{
    public int TotalCount { get; set; }
    public List<BookingDto> Bookings { get; set; } = new();
}
