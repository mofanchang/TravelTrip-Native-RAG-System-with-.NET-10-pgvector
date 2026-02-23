using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TravelTripApi.Controllers;
using TravelTripApi.Models;
using TravelTripApi.Data;
using Microsoft.EntityFrameworkCore;

namespace TravelTripApi.Tests.Controllers;

public class AuthorizeRoleAttributeTests
{
    private readonly TravelDbContext _context;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public AuthorizeRoleAttributeTests()
    {
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_AuthAttr_{Guid.NewGuid()}")
            .Options;
        _context = new TravelDbContext(options);

        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(TravelDbContext))).Returns(_context);
        
        var mockLogger = new Mock<ILogger<AuthorizeRoleAttribute>>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<AuthorizeRoleAttribute>))).Returns(mockLogger.Object);
    }

    private AuthorizationFilterContext CreateContext(int? userId, bool isAuthenticated = true)
    {
        var httpContext = new DefaultHttpContext();
        if (isAuthenticated && userId.HasValue)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }
        else
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        httpContext.RequestServices = _mockServiceProvider.Object;

        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task OnAuthorizationAsync_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var attr = new AuthorizeRoleAttribute(AdminRole.Viewer);
        var context = CreateContext(null, false);

        // Act
        await attr.OnAuthorizationAsync(context);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var attr = new AuthorizeRoleAttribute(AdminRole.Viewer);
        var context = CreateContext(999);

        // Act
        await attr.OnAuthorizationAsync(context);

        // Assert
        Assert.IsType<NotFoundObjectResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_NotAdmin_ReturnsForbid()
    {
        // Arrange
        var user = new UserEntity { Id = 10, Email = "user@test.com", AdminRole = null, Password = "x" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var attr = new AuthorizeRoleAttribute(AdminRole.Viewer);
        var context = CreateContext(10);

        // Act
        await attr.OnAuthorizationAsync(context);

        // Assert
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_HigherRole_Allowed()
    {
        // Arrange
        var user = new UserEntity { Id = 1, Email = "sadmin@test.com", AdminRole = AdminRole.SuperAdmin, Password = "x" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var attr = new AuthorizeRoleAttribute(AdminRole.Editor);
        var context = CreateContext(1);

        // Act
        await attr.OnAuthorizationAsync(context);

        // Assert
        Assert.Null(context.Result); // Null means allowed
    }

    [Fact]
    public async Task OnAuthorizationAsync_InsufficientRole_ReturnsForbid()
    {
        // Arrange
        var user = new UserEntity { Id = 2, Email = "viewer@test.com", AdminRole = AdminRole.Viewer, Password = "x" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var attr = new AuthorizeRoleAttribute(AdminRole.Admin); // Requires Admin
        var context = CreateContext(2);

        // Act
        await attr.OnAuthorizationAsync(context);

        // Assert
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_ExactRole_Allowed()
    {
        // Arrange
        var user = new UserEntity { Id = 3, Email = "editor@test.com", AdminRole = AdminRole.Editor, Password = "x" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var attr = new AuthorizeRoleAttribute(AdminRole.Editor);
        var context = CreateContext(3);

        // Act
        await attr.OnAuthorizationAsync(context);

        // Assert
        Assert.Null(context.Result);
    }
}
