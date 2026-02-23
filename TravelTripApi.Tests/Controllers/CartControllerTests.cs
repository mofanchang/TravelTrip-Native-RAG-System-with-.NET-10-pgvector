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
/// CartController 測試
/// 測試購物車相關 API：查看、新增、更新、刪除、結帳
/// </summary>
public class CartControllerTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly CartService _cartService;
    private readonly BookingService _bookingService;
    private readonly Mock<ILogger<CartController>> _mockControllerLogger;
    private readonly CartController _controller;
    private const int TestUserId = 1;

    public CartControllerTests()
    {
        // 使用 In-Memory Database
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Cart_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);
        
        // 創建真實的 Service 實例
        var mockCartLogger = new Mock<ILogger<CartService>>();
        var mockBookingLogger = new Mock<ILogger<BookingService>>();
        _cartService = new CartService(_context, mockCartLogger.Object);
        _bookingService = new BookingService(_context, mockBookingLogger.Object);
        
        _mockControllerLogger = new Mock<ILogger<CartController>>();
        _controller = new CartController(
            _cartService,
            _bookingService,
            _mockControllerLogger.Object);

        // 設置認證
        SetupAuthenticatedUser();
        
        // 添加測試數據
        SeedTestData();
    }

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com")
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

    // ─── GetCart 測試 ──────────────────────────────────────

    [Fact]
    public async Task GetCart_EmptyCart_ReturnsEmptyCart()
    {
        // Act
        var result = await _controller.GetCart();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var cart = Assert.IsType<CartDto>(okResult.Value);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.TotalPrice);
    }

    [Fact]
    public async Task GetCart_WithItems_ReturnsCart()
    {
        // Arrange - 先添加商品到購物車
        await _controller.AddToCart(new AddToCartRequest { TripId = 1, Quantity = 2 });

        // Act
        var result = await _controller.GetCart();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var cart = Assert.IsType<CartDto>(okResult.Value);
        Assert.Single(cart.Items);
        Assert.Equal(TestUserId, cart.UserId);
    }

    // ─── AddToCart 測試 ────────────────────────────────────

    [Fact]
    public async Task AddToCart_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new AddToCartRequest
        {
            TripId = 1,
            Quantity = 2
        };

        // Act
        var result = await _controller.AddToCart(request);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddToCart_InvalidTripId_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddToCartRequest
        {
            TripId = 0,
            Quantity = 1
        };

        // Act
        var result = await _controller.AddToCart(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddToCart_QuantityTooLow_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddToCartRequest
        {
            TripId = 1,
            Quantity = 0
        };

        // Act
        var result = await _controller.AddToCart(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddToCart_QuantityTooHigh_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddToCartRequest
        {
            TripId = 1,
            Quantity = 11
        };

        // Act
        var result = await _controller.AddToCart(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task AddToCart_ValidQuantities_ReturnsOk(int quantity)
    {
        // Arrange
        var request = new AddToCartRequest
        {
            TripId = 1,
            Quantity = quantity
        };

        // Act
        var result = await _controller.AddToCart(request);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ─── UpdateCartItem 測試 ───────────────────────────────

    [Fact]
    public async Task UpdateCartItem_QuantityTooLow_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateCartItemRequest { Quantity = 0 };

        // Act
        var result = await _controller.UpdateCartItem(1, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateCartItem_QuantityTooHigh_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateCartItemRequest { Quantity = 11 };

        // Act
        var result = await _controller.UpdateCartItem(1, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── RemoveFromCart 測試 ───────────────────────────────

    [Fact]
    public async Task RemoveFromCart_NonExistentItem_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.RemoveFromCart(999);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── ClearCart 測試 ────────────────────────────────────

    [Fact]
    public async Task ClearCart_Success_ReturnsOk()
    {
        // Arrange - 先添加商品
        await _controller.AddToCart(new AddToCartRequest { TripId = 1, Quantity = 1 });

        // Act
        var result = await _controller.ClearCart();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ClearCart_EmptyCart_ReturnsOk()
    {
        // Act
        var result = await _controller.ClearCart();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── Checkout 測試 ─────────────────────────────────────

    [Fact]
    public async Task Checkout_WithItems_ReturnsOk()
    {
        // Arrange - 添加商品到購物車
        await _controller.AddToCart(new AddToCartRequest { TripId = 1, Quantity = 2 });

        // Act
        var result = await _controller.Checkout();

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Checkout_EmptyCart_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Checkout();

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
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
    public async Task GetCart_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var unauthController = new CartController(
            _cartService,
            _bookingService,
            _mockControllerLogger.Object);
        unauthController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await unauthController.GetCart();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
