using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TravelTripApi.Controllers;
using TravelTripApi.Services;
using TravelTripApi.Data;
using TravelTripApi.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Hosting;

namespace TravelTripApi.Tests.Controllers;

public class AdminV2ControllerTests : IDisposable
{
    private readonly TravelDbContext _context;
    private readonly Mock<ILogger<AdminV2Controller>> _mockLogger;
    private readonly Mock<ILogger<AuditService>> _mockAuditLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<FileUploadService>> _mockFileUploadLogger;
    private readonly AuditService _auditService;
    private readonly FileUploadService _fileUploadService;
    private readonly AdminV2Controller _controller;

    public AdminV2ControllerTests()
    {
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_AdminV2_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);
        _mockLogger = new Mock<ILogger<AdminV2Controller>>();
        _mockAuditLogger = new Mock<ILogger<AuditService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockFileUploadLogger = new Mock<ILogger<FileUploadService>>();

        _auditService = new AuditService(_context, _mockAuditLogger.Object, _mockHttpContextAccessor.Object);
        _fileUploadService = new FileUploadService(_mockEnv.Object, _mockFileUploadLogger.Object);

        _controller = new AdminV2Controller(_context, _auditService, _fileUploadService, _mockLogger.Object);

        // Setup mock user for controller
        SetupUser(1, AdminRole.SuperAdmin);
    }

    private void SetupUser(int userId, AdminRole? role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "admin@test.com")
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext();
        httpContext.User = claimsPrincipal;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        // Ensure user exists in DB if needed
        if (!_context.Users.Any(u => u.Id == userId))
        {
            _context.Users.Add(new UserEntity 
            { 
                Id = userId, 
                Email = "admin@test.com", 
                AdminRole = role,
                Password = "hashedpassword",
                FirstName = "Admin",
                LastName = "User",
                IsActive = true
            });
            _context.SaveChanges();
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task GetCurrentAdmin_Authenticated_ReturnsOk()
    {
        // Act
        var result = await _controller.GetCurrentAdmin();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAllTrips_Authenticated_ReturnsOk()
    {
        // Arrange
        _context.Trips.Add(new TripEntity { Name = "Test Trip", Price = 100 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllTrips();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CreateTrip_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new CreateTripRequest
        {
            Name = "New Trip",
            Price = 500,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            AvailableSeats = 10,
            Location = "Test place",
            Duration = 7,
            Country = "Test Country",
            City = "Test City"
        };

        // Act
        var result = await _controller.CreateTrip(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.True(_context.Trips.Any(t => t.Name == "New Trip"));
    }

    [Fact]
    public async Task UpdateTrip_ValidRequest_ReturnsOk()
    {
        // Arrange
        var trip = new TripEntity { Name = "Old Name", Price = 100 };
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        var request = new UpdateTripRequest
        {
            Name = "New Name",
            Price = 200
        };

        // Act
        var result = await _controller.UpdateTrip(trip.Id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedTrip = await _context.Trips.FindAsync(trip.Id);
        Assert.NotNull(updatedTrip);
        Assert.Equal("New Name", updatedTrip.Name);
        Assert.Equal(200, updatedTrip.Price);
    }

    [Fact]
    public async Task DeleteTrip_Existing_ReturnsOk()
    {
        // Arrange
        var trip = new TripEntity { Name = "Delete Me", Price = 100 };
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteTrip(trip.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.False(_context.Trips.Any(t => t.Id == trip.Id));
    }

    [Fact]
    public async Task GetAllUsers_Admin_ReturnsOk()
    {
        // Arrange
        _context.Users.Add(new UserEntity { Email = "user@test.com", FirstName = "User", LastName = "One", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ToggleUserActive_ExistingUser_ReturnsOk()
    {
        // Arrange
        var user = new UserEntity { Email = "toggle@test.com", IsActive = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ToggleUserActive(user.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.False(updatedUser.IsActive);
    }

    [Fact]
    public async Task GetAuditLogs_Admin_ReturnsOk()
    {
        // Act
        var result = await _controller.GetAuditLogs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetStats_Admin_ReturnsOk()
    {
        // Act
        var result = await _controller.GetStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    // ─── Edge Case Tests ─────────────────────────────────

    [Fact]
    public async Task UpdateTrip_NonExistent_ReturnsNotFound()
    {
        // Act
        var result = await _controller.UpdateTrip(999, new UpdateTripRequest { Name = "Fail" });

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteTrip_NonExistent_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteTrip(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ToggleUserActive_NonExistent_ReturnsNotFound()
    {
        // Act
        var result = await _controller.ToggleUserActive(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ToggleUserActive_AdminUser_ReturnsBadRequest()
    {
        // Arrange
        var admin = new UserEntity { Id = 50, Email = "another_admin@test.com", AdminRole = AdminRole.Editor };
        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ToggleUserActive(50);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateTrip_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Required");
        var request = new CreateTripRequest();

        // Act
        var result = await _controller.CreateTrip(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
