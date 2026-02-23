using Microsoft.EntityFrameworkCore;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;
using TravelTrip.Core.DTOs;

namespace TravelTripApi.Services;

/// <summary>
/// 訂單服務
/// </summary>
public class BookingService
{
    private readonly TravelDbContext _context;
    private readonly ILogger<BookingService> _logger;

    public BookingService(TravelDbContext context, ILogger<BookingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 建立訂單
    /// </summary>
    public async Task<(bool Success, string Message, BookingDto? Booking)> CreateBookingAsync(
        int userId, 
        CreateBookingRequest request)
    {
        try
        {
            // 驗證所有行程是否存在且有足夠座位
            var tripIds = request.Items.Select(i => i.TripId).ToList();
            var trips = await _context.Trips
                .Where(t => tripIds.Contains(t.Id))
                .ToListAsync();

            if (trips.Count != tripIds.Count)
            {
                return (false, "部分行程不存在", null);
            }

            // 檢查庫存
            foreach (var item in request.Items)
            {
                var trip = trips.First(t => t.Id == item.TripId);
                if (trip.AvailableSeats < item.Quantity)
                {
                    return (false, $"行程「{trip.Name}」座位不足", null);
                }
            }

            // 計算總金額
            decimal total = 0;
            var bookingItems = new List<BookingItemEntity>();

            foreach (var item in request.Items)
            {
                var trip = trips.First(t => t.Id == item.TripId);
                var subtotal = trip.Price * item.Quantity;
                total += subtotal;

                bookingItems.Add(new BookingItemEntity
                {
                    TripId = item.TripId,
                    Quantity = item.Quantity,
                    Price = trip.Price,
                    Subtotal = subtotal
                });
            }

            // 建立訂單
            var booking = new BookingEntity
            {
                UserId = userId,
                Total = total,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                BookingItems = bookingItems
            };

            _context.Bookings.Add(booking);

            // 更新庫存
            foreach (var item in request.Items)
            {
                var trip = trips.First(t => t.Id == item.TripId);
                trip.AvailableSeats -= item.Quantity;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"訂單建立成功: BookingId={booking.Id}, UserId={userId}, Total={total}");

            // 載入完整資料並轉換為 DTO
            var bookingDto = await GetBookingByIdAsync(booking.Id, userId);

            return (true, "訂單建立成功", bookingDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"建立訂單失敗: UserId={userId}");
            return (false, "建立訂單失敗，請稍後再試", null);
        }
    }

    /// <summary>
    /// 取得單一訂單
    /// </summary>
    public async Task<BookingDto?> GetBookingByIdAsync(int bookingId, int userId)
    {
        var booking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.BookingItems!)
                .ThenInclude(bi => bi.Trip)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

        if (booking == null)
            return null;

        return MapToBookingDto(booking);
    }

    /// <summary>
    /// 取得使用者的所有訂單
    /// </summary>
    public async Task<BookingListResponse> GetUserBookingsAsync(int userId, int skip = 0, int take = 20)
    {
        var query = _context.Bookings
            .Include(b => b.BookingItems!)
                .ThenInclude(bi => bi.Trip)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt);

        var totalCount = await query.CountAsync();
        var bookings = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return new BookingListResponse
        {
            TotalCount = totalCount,
            Bookings = bookings.Select(MapToBookingDto).ToList()
        };
    }

    /// <summary>
    /// 更新訂單狀態
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateBookingStatusAsync(
        int bookingId, 
        int userId, 
        UpdateBookingStatusRequest request)
    {
        try
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                return (false, "訂單不存在");
            }

            booking.Status = request.Status;

            if (!string.IsNullOrEmpty(request.PaypalTransactionId))
            {
                booking.PaypalTransactionId = request.PaypalTransactionId;
            }

            if (request.Status == "Completed")
            {
                booking.PaymentCompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"訂單狀態更新: BookingId={bookingId}, Status={request.Status}");

            return (true, "訂單狀態更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新訂單狀態失敗: BookingId={bookingId}");
            return (false, "更新訂單狀態失敗");
        }
    }

    /// <summary>
    /// 取消訂單
    /// </summary>
    public async Task<(bool Success, string Message)> CancelBookingAsync(int bookingId, int userId)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems!)
                    .ThenInclude(bi => bi.Trip)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                return (false, "訂單不存在");
            }

            if (booking.Status == "Completed")
            {
                return (false, "已完成的訂單無法取消");
            }

            if (booking.Status == "Cancelled")
            {
                return (false, "訂單已被取消");
            }

            // 還原庫存
            if (booking.BookingItems != null)
            {
                foreach (var item in booking.BookingItems)
                {
                    if (item.Trip != null)
                    {
                        item.Trip.AvailableSeats += item.Quantity;
                    }
                }
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            _logger.LogInformation($"訂單已取消: BookingId={bookingId}");

            return (true, "訂單已取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"取消訂單失敗: BookingId={bookingId}");
            return (false, "取消訂單失敗");
        }
    }

    /// <summary>
    /// 將 BookingEntity 轉換為 BookingDto
    /// </summary>
    private BookingDto MapToBookingDto(BookingEntity booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            CreatedAt = booking.CreatedAt,
            Total = booking.Total,
            Status = booking.Status,
            PaypalTransactionId = booking.PaypalTransactionId,
            PaymentCompletedAt = booking.PaymentCompletedAt,
            Items = booking.BookingItems?.Select(item => new BookingItemDto
            {
                Id = item.Id,
                TripId = item.TripId,
                TripName = item.Trip?.Name ?? "",
                Quantity = item.Quantity,
                Price = item.Price,
                Subtotal = item.Subtotal,
                Trip = item.Trip != null ? new TripSearchResultDto
                {
                    Id = item.Trip.Id,
                    Name = item.Trip.Name,
                    Description = item.Trip.Description,
                    Country = item.Trip.Country,
                    City = item.Trip.City,
                    Duration = item.Trip.Duration,
                    Keywords = item.Trip.Keywords,
                    Price = item.Trip.Price,
                    Image = item.Trip.Image,
                    StartDate = item.Trip.StartDate,
                    EndDate = item.Trip.EndDate,
                    AvailableSeats = item.Trip.AvailableSeats
                } : null
            }).ToList() ?? new List<BookingItemDto>(),
            User = booking.User != null ? new UserDto
            {
                Id = booking.User.Id,
                Email = booking.User.Email,
                Username = booking.User.Username,
                FirstName = booking.User.FirstName,
                LastName = booking.User.LastName
            } : null
        };
    }
}
