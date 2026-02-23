using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TravelTripApi.Controllers;

public abstract class BaseController : ControllerBase
{
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim?.Value, out var userId))
            return userId;
        return null;
    }
}
