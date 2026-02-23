using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelTripApi.Controllers;
using TravelTripApi.Services;
using TravelTripApi.Data;
using TravelTripApi.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TravelTripApi.Tests.Controllers;

/// <summary>
/// BookingsController 測試
/// 測試訂單相關 API：建立、查詢、取消訂單
/// </summary>
public class BookingsControllerTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly BookingService _bookingService;
    private readonly Mock<ILogger<BookingsController>> _mockControllerLogger;
    private readonly Mock<ILogger<BookingService>> _mockServiceLogger;
    private readonly BookingsController _controller;
    private const int TestUserId = 1;

    public BookingsControllerTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Bookings_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);
        
        // 創建真實的 BookingService 實例
        _mockServiceLogger = new Mock<ILogger<BookingService>>();
        _bookingService = new BookingService(_context, _mockServiceLogger.Object);
        
        _mockControllerLogger = new Mock<ILogger<BookingsController>>();
        _controller = new BookingsController(_bookingService, _mockControllerLogger.Object);

        // 設置模擬的 User Claims（模擬已登入用戶）
        SetupAuthenticatedUser();
        
        // 添加測試數據
        SeedTestData();
    }

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SeedTestData()
    {
        // 添加測試用戶
        var user = new TravelTripApi.Models.UserEntity
        {
            Id = TestUserId,
            Email = "test@example.com",
            Username = "testuser",
            FirstName = "Test",
            LastName = "User",
            Password = "hashedpassword",
            DateJoined = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);

        // 添加測試行程
        var trip = new TravelTripApi.Models.TripEntity
        {
            Id = 1,
            Name = "東京5日遊",
            Country = "日本",
            City = "東京",
            Duration = 5,
            Price = 38000,
            AvailableSeats = 20,
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(35)
        };
        _context.Trips.Add(trip);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    // ─── CreateBooking 測試 ────────────────────────────────

    [Fact]
    public async Task CreateBooking_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new() { TripId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _controller.CreateBooking(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var booking = Assert.IsType<BookingDto>(createdResult.Value);
        Assert.Equal(TestUserId, booking.UserId);
        Assert.Equal("Pending", booking.Status);
    }

    [Fact]
    public async Task CreateBooking_EmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>()
        };

        // Act
        var result = await _controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateBooking_NullItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            Items = null!
        };

        // Act
        var result = await _controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateBooking_InvalidQuantity_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new() { TripId = 1, Quantity = 0 }
            }
        };

        // Act
        var result = await _controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateBooking_NegativeQuantity_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new() { TripId = 1, Quantity = -1 }
            }
        };

        // Act
        var result = await _controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateBooking_NonExistentTrip_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new() { TripId = 999, Quantity = 1 }
            }
        };

        // Act
        var result = await _controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ─── GetBooking 測試 ───────────────────────────────────

    [Fact]
    public async Task GetBooking_ValidId_ReturnsBooking()
    {
        // Arrange - 先建立一個訂單
        var createRequest = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new() { TripId = 1, Quantity = 1 }
            }
        };
        var createResult = await _controller.CreateBooking(createRequest);
        var createdBooking = (createResult.Result as CreatedAtActionResult)?.Value as BookingDto;

        // Act
        var result = await _controller.GetBooking(createdBooking!.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var booking = Assert.IsType<BookingDto>(okResult.Value);
        Assert.Equal(createdBooking.Id, booking.Id);
    }

    [Fact]
    public async Task GetBooking_InvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetBooking(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ─── GetMyBookings 測試 ────────────────────────────────

    [Fact]
    public async Task GetMyBookings_ReturnsBookingList()
    {
        // Arrange - 建立兩個訂單
        await _controller.CreateBooking(new CreateBookingRequest
        {
            Items = new List<BookingItemRequest> { new() { TripId = 1, Quantity = 1 } }
        });
        await _controller.CreateBooking(new CreateBookingRequest
        {
            Items = new List<BookingItemRequest> { new() { TripId = 1, Quantity = 2 } }
        });

        // Act
        var result = await _controller.GetMyBookings(0, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bookingList = Assert.IsType<BookingListResponse>(okResult.Value);
        Assert.True(bookingList.TotalCount >= 2);
    }

    // ─── CancelBooking 測試 ────────────────────────────────

    [Fact]
    public async Task CancelBooking_ValidBooking_ReturnsOk()
    {
        // Arrange - 建立訂單
        var createRequest = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new() { TripId = 1, Quantity = 1 }
            }
        };
        var createResult = await _controller.CreateBooking(createRequest);
        var booking = (createResult.Result as CreatedAtActionResult)?.Value as BookingDto;

        // Act
        var result = await _controller.CancelBooking(booking!.Id);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CancelBooking_InvalidId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.CancelBooking(999);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Health Check 測試 ─────────────────────────────────

    [Fact]
    public void Health_ReturnsOk()
    {
        // Act
        var result = _controller.Health();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    // ─── 認證測試 ──────────────────────────────────────────

    [Fact]
    public async Task CreateBooking_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - 創建沒有認證的 Controller
        var unauthController = new BookingsController(_bookingService, _mockControllerLogger.Object);
        unauthController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new CreateBookingRequest
        {
            Items = new List<BookingItemRequest>
            {
                new() { TripId = 1, Quantity = 1 }
            }
        };

        // Act
        var result = await unauthController.CreateBooking(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
