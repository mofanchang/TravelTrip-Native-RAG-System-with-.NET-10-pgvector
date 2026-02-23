using Xunit;
using TravelTripApi.Services;
using TravelTripApi.DTOs;
using TravelTripApi.Models;
using TravelTripApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TravelTripApi.Tests.Services;

/// <summary>
/// 訂單服務測試
/// </summary>
public class BookingServiceTests
{
    [Fact]
    public async Task CreateBooking_ValidRequest_ReturnsSuccessAndUpdatesInventory()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<BookingService>();
        var service = new BookingService(context, logger);

        // 建立測試使用者
        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);

        // 建立測試行程
        var trip = new TripEntity
        {
            Name = "日本東京5日遊",
            Price = 30000m,
            AvailableSeats = 10,
            Country = "日本",
            City = "東京",
            Duration = 5,
            Description = "測試行程",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(35)
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new BookingItemRequest { TripId = trip.Id, Quantity = 2 }
            }
        };

        // Act
        var (success, message, booking) = await service.CreateBookingAsync(user.Id, request);

        // Assert
        Assert.True(success);
        Assert.Equal("訂單建立成功", message);
        Assert.NotNull(booking);
        Assert.Equal(60000m, booking.Total); // 30000 * 2
        Assert.Equal("Pending", booking.Status);
        Assert.Single(booking.Items);
        
        // 驗證庫存已扣除
        var updatedTrip = await context.Trips.FindAsync(trip.Id);
        Assert.NotNull(updatedTrip);
        Assert.Equal(8, updatedTrip.AvailableSeats); // 10 - 2
    }

    [Fact]
    public async Task CreateBooking_InsufficientSeats_ReturnsFailed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<BookingService>();
        var service = new BookingService(context, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);

        var trip = new TripEntity
        {
            Name = "日本東京5日遊",
            Price = 30000m,
            AvailableSeats = 3, // 只有 3 個座位
            Country = "日本",
            City = "東京",
            Duration = 5,
            Description = "測試行程",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(35)
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new BookingItemRequest { TripId = trip.Id, Quantity = 5 } // 要買 5 個
            }
        };

        // Act
        var (success, message, booking) = await service.CreateBookingAsync(user.Id, request);

        // Assert
        Assert.False(success);
        Assert.Contains("座位不足", message);
        Assert.Null(booking);
        
        // 驗證庫存未變動
        var updatedTrip = await context.Trips.FindAsync(trip.Id);
        Assert.NotNull(updatedTrip);
        Assert.Equal(3, updatedTrip.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_NonExistentTrip_ReturnsFailed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<BookingService>();
        var service = new BookingService(context, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new BookingItemRequest { TripId = 999, Quantity = 1 } // 不存在的行程
            }
        };

        // Act
        var (success, message, booking) = await service.CreateBookingAsync(user.Id, request);

        // Assert
        Assert.False(success);
        Assert.Equal("部分行程不存在", message);
        Assert.Null(booking);
    }

    [Fact]
    public async Task CreateBooking_MultipleItems_CalculatesTotalCorrectly()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<BookingService>();
        var service = new BookingService(context, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);

        var trip1 = new TripEntity
        {
            Name = "東京行程",
            Price = 30000m,
            AvailableSeats = 10,
            Country = "日本",
            City = "東京",
            Duration = 5,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(35)
        };

        var trip2 = new TripEntity
        {
            Name = "大阪行程",
            Price = 25000m,
            AvailableSeats = 10,
            Country = "日本",
            City = "大阪",
            Duration = 4,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(40),
            EndDate = DateTime.UtcNow.AddDays(44)
        };

        context.Trips.AddRange(trip1, trip2);
        await context.SaveChangesAsync();

        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new BookingItemRequest { TripId = trip1.Id, Quantity = 2 }, // 60000
                new BookingItemRequest { TripId = trip2.Id, Quantity = 3 }  // 75000
            }
        };

        // Act
        var (success, message, booking) = await service.CreateBookingAsync(user.Id, request);

        // Assert
        Assert.True(success);
        Assert.NotNull(booking);
        Assert.Equal(135000m, booking.Total); // 60000 + 75000
        Assert.Equal(2, booking.Items.Count);
        
        // 驗證庫存都已扣除
        var updatedTrip1 = await context.Trips.FindAsync(trip1.Id);
        var updatedTrip2 = await context.Trips.FindAsync(trip2.Id);
        Assert.Equal(8, updatedTrip1!.AvailableSeats);
        Assert.Equal(7, updatedTrip2!.AvailableSeats);
    }

    [Fact]
    public async Task GetUserBookings_ReturnsUserBookingsOnly()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<BookingService>();
        var service = new BookingService(context, logger);

        var user1 = new UserEntity { Email = "user1@test.com", Password = "hashed", IsActive = true, DateJoined = DateTime.UtcNow };
        var user2 = new UserEntity { Email = "user2@test.com", Password = "hashed", IsActive = true, DateJoined = DateTime.UtcNow };
        context.Users.AddRange(user1, user2);

        var trip = new TripEntity
        {
            Name = "測試行程",
            Price = 10000m,
            AvailableSeats = 10,
            Country = "台灣",
            City = "台北",
            Duration = 3,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(33)
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        // User1 有 2 筆訂單
        var booking1 = new BookingEntity { UserId = user1.Id, Total = 10000m, Status = "Pending", CreatedAt = DateTime.UtcNow };
        var booking2 = new BookingEntity { UserId = user1.Id, Total = 20000m, Status = "Completed", CreatedAt = DateTime.UtcNow };
        
        // User2 有 1 筆訂單
        var booking3 = new BookingEntity { UserId = user2.Id, Total = 30000m, Status = "Pending", CreatedAt = DateTime.UtcNow };
        
        context.Bookings.AddRange(booking1, booking2, booking3);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserBookingsAsync(user1.Id);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Bookings.Count);
        Assert.All(result.Bookings, b => Assert.Equal(user1.Id, b.UserId));
    }

    [Fact]
    public async Task CancelBooking_PendingOrder_RestoresInventory()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<BookingService>();
        var service = new BookingService(context, logger);

        var user = new UserEntity { Email = "test@test.com", Password = "hashed", IsActive = true, DateJoined = DateTime.UtcNow };
        context.Users.Add(user);

        var trip = new TripEntity
        {
            Name = "測試行程",
            Price = 10000m,
            AvailableSeats = 5, // 原本 10 個，被訂走 5 個後剩 5 個
            Country = "台灣",
            City = "台北",
            Duration = 3,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(33)
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var booking = new BookingEntity
        {
            UserId = user.Id,
            Total = 50000m,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            BookingItems = new List<BookingItemEntity>
            {
                new BookingItemEntity
                {
                    TripId = trip.Id,
                    Quantity = 5,
                    Price = 10000m,
                    Subtotal = 50000m
                }
            }
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        // Act
        var (success, message) = await service.CancelBookingAsync(booking.Id, user.Id);

        // Assert
        Assert.True(success);
        Assert.Equal("訂單已取消", message);
        
        // 驗證庫存已還原
        var updatedTrip = await context.Trips.FindAsync(trip.Id);
        Assert.NotNull(updatedTrip);
        Assert.Equal(10, updatedTrip.AvailableSeats); // 5 + 5 = 10
        
        // 驗證訂單狀態
        var updatedBooking = await context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updatedBooking);
        Assert.Equal("Cancelled", updatedBooking.Status);
    }

    [Fact]
    public async Task CancelBooking_CompletedOrder_ReturnsFailed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<BookingService>();
        var service = new BookingService(context, logger);

        var user = new UserEntity { Email = "test@test.com", Password = "hashed", IsActive = true, DateJoined = DateTime.UtcNow };
        context.Users.Add(user);

        var booking = new BookingEntity
        {
            UserId = user.Id,
            Total = 10000m,
            Status = "Completed", // 已完成
            CreatedAt = DateTime.UtcNow
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        // Act
        var (success, message) = await service.CancelBookingAsync(booking.Id, user.Id);

        // Assert
        Assert.False(success);
        Assert.Equal("已完成的訂單無法取消", message);
    }

    [Fact]
    public async Task UpdateBookingStatus_ValidRequest_UpdatesStatus()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<BookingService>();
        var service = new BookingService(context, logger);

        var user = new UserEntity { Email = "test@test.com", Password = "hashed", IsActive = true, DateJoined = DateTime.UtcNow };
        context.Users.Add(user);

        var booking = new BookingEntity
        {
            UserId = user.Id,
            Total = 10000m,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var request = new UpdateBookingStatusRequest
        {
            Status = "Completed",
            PaypalTransactionId = "PAYPAL123"
        };

        // Act
        var (success, message) = await service.UpdateBookingStatusAsync(booking.Id, user.Id, request);

        // Assert
        Assert.True(success);
        Assert.Equal("訂單狀態更新成功", message);
        
        var updatedBooking = await context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updatedBooking);
        Assert.Equal("Completed", updatedBooking.Status);
        Assert.Equal("PAYPAL123", updatedBooking.PaypalTransactionId);
        Assert.NotNull(updatedBooking.PaymentCompletedAt);
    }
}
