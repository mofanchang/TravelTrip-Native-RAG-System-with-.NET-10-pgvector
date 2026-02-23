using Xunit;
using TravelTripApi.Services;
using TravelTripApi.DTOs;
using TravelTripApi.Models;
using TravelTripApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TravelTripApi.Tests.Services;

/// <summary>
/// 購物車服務測試
/// </summary>
public class CartServiceTests
{
    [Fact]
    public async Task AddToCart_NewItem_AddsSuccessfully()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

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

        var request = new AddToCartRequest
        {
            TripId = trip.Id,
            Quantity = 2
        };

        // Act
        var (success, message, cart) = await service.AddToCartAsync(user.Id, request);

        // Assert
        Assert.True(success);
        Assert.Equal("已加入購物車", message);
        Assert.NotNull(cart);
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items[0].Quantity);
        Assert.Equal(20000m, cart.TotalPrice); // 10000 * 2
    }

    [Fact]
    public async Task AddToCart_ExistingItem_UpdatesQuantity()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

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

        // 先加入 2 個
        await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip.Id, Quantity = 2 });

        // Act - 再加入 3 個
        var (success, message, cart) = await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip.Id, Quantity = 3 });

        // Assert
        Assert.True(success);
        Assert.NotNull(cart);
        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items[0].Quantity); // 2 + 3
        Assert.Equal(50000m, cart.TotalPrice); // 10000 * 5
    }

    [Fact]
    public async Task AddToCart_InsufficientSeats_ReturnsFailed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

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
            Name = "測試行程",
            Price = 10000m,
            AvailableSeats = 3, // 只有 3 個座位
            Country = "台灣",
            City = "台北",
            Duration = 3,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(33)
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var request = new AddToCartRequest
        {
            TripId = trip.Id,
            Quantity = 5 // 要買 5 個
        };

        // Act
        var (success, message, cart) = await service.AddToCartAsync(user.Id, request);

        // Assert
        Assert.False(success);
        Assert.Contains("座位不足", message);
        Assert.Null(cart);
    }

    [Fact]
    public async Task AddToCart_ExceedMaxQuantity_ReturnsFailed()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

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
            Name = "測試行程",
            Price = 10000m,
            AvailableSeats = 20,
            Country = "台灣",
            City = "台北",
            Duration = 3,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(33)
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        // 先加入 8 個
        await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip.Id, Quantity = 8 });

        // Act - 再加入 3 個（總共 11 個，超過限制 10 個）
        var (success, message, cart) = await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip.Id, Quantity = 3 });

        // Assert
        Assert.False(success);
        Assert.Contains("最多只能加入 10 個", message);
    }

    [Fact]
    public async Task GetCart_EmptyCart_ReturnsEmptyDto()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

        var user = new UserEntity
        {
            Email = "test@test.com",
            Password = "hashed",
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var cart = await service.GetCartAsync(user.Id);

        // Assert
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.TotalPrice);
        Assert.Equal(0, cart.TotalItems);
    }

    [Fact]
    public async Task RemoveFromCart_ExistingItem_RemovesSuccessfully()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

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

        // 先加入商品
        var (_, _, cart) = await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip.Id, Quantity = 2 });
        var cartItemId = cart!.Items[0].Id;

        // Act - 移除商品
        var (success, message) = await service.RemoveFromCartAsync(user.Id, cartItemId);

        // Assert
        Assert.True(success);
        Assert.Equal("已從購物車移除", message);
        
        // 驗證購物車已空
        var updatedCart = await service.GetCartAsync(user.Id);
        Assert.Empty(updatedCart!.Items);
    }

    [Fact]
    public async Task UpdateCartItem_ValidQuantity_UpdatesSuccessfully()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

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

        // 先加入 2 個
        var (_, _, cart) = await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip.Id, Quantity = 2 });
        var cartItemId = cart!.Items[0].Id;

        // Act - 更新為 5 個
        var (success, message) = await service.UpdateCartItemAsync(user.Id, cartItemId, new UpdateCartItemRequest { Quantity = 5 });

        // Assert
        Assert.True(success);
        Assert.Equal("購物車項目已更新", message);
        
        // 驗證數量已更新
        var updatedCart = await service.GetCartAsync(user.Id);
        Assert.Equal(5, updatedCart!.Items[0].Quantity);
        Assert.Equal(50000m, updatedCart.TotalPrice);
    }

    [Fact]
    public async Task ClearCart_WithItems_ClearsAllItems()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

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
            Name = "行程1",
            Price = 10000m,
            AvailableSeats = 10,
            Country = "台灣",
            City = "台北",
            Duration = 3,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(33)
        };

        var trip2 = new TripEntity
        {
            Name = "行程2",
            Price = 20000m,
            AvailableSeats = 10,
            Country = "日本",
            City = "東京",
            Duration = 5,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(40),
            EndDate = DateTime.UtcNow.AddDays(45)
        };

        context.Trips.AddRange(trip1, trip2);
        await context.SaveChangesAsync();

        // 加入多個商品
        await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip1.Id, Quantity = 2 });
        await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip2.Id, Quantity = 3 });

        // Act - 清空購物車
        var (success, message) = await service.ClearCartAsync(user.Id);

        // Assert
        Assert.True(success);
        Assert.Equal("購物車已清空", message);
        
        // 驗證購物車已空
        var cart = await service.GetCartAsync(user.Id);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task GetCart_MultipleItems_CalculatesTotalCorrectly()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var logger = TestHelper.CreateMockLogger<CartService>();
        var service = new CartService(context, logger);

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
            Name = "行程1",
            Price = 10000m,
            AvailableSeats = 10,
            Country = "台灣",
            City = "台北",
            Duration = 3,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(33)
        };

        var trip2 = new TripEntity
        {
            Name = "行程2",
            Price = 25000m,
            AvailableSeats = 10,
            Country = "日本",
            City = "東京",
            Duration = 5,
            Description = "測試",
            StartDate = DateTime.UtcNow.AddDays(40),
            EndDate = DateTime.UtcNow.AddDays(45)
        };

        context.Trips.AddRange(trip1, trip2);
        await context.SaveChangesAsync();

        await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip1.Id, Quantity = 2 }); // 20000
        await service.AddToCartAsync(user.Id, new AddToCartRequest { TripId = trip2.Id, Quantity = 3 }); // 75000

        // Act
        var cart = await service.GetCartAsync(user.Id);

        // Assert
        Assert.NotNull(cart);
        Assert.Equal(2, cart.Items.Count);
        Assert.Equal(95000m, cart.TotalPrice); // 20000 + 75000
        Assert.Equal(5, cart.TotalItems); // 2 + 3
    }
}
