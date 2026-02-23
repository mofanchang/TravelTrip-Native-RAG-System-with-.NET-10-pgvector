using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;

namespace TravelTripApi.Attributes;

/// <summary>
/// 基於角色的授權屬性
/// 使用方式：[AuthorizeRole(AdminRole.Admin)]
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AuthorizeRoleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly AdminRole[] _roles;

    public AuthorizeRoleAttribute(params AdminRole[] roles)
    {
        _roles = roles;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            var httpContext = context.HttpContext;
            var user = httpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "未認證" });
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<TravelDbContext>();
            var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            
            if (!int.TryParse(userIdClaim?.Value, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "無法解析用戶ID" });
                return;
            }

            var dbUser = await dbContext.Users.FindAsync(userId);
            if (dbUser == null)
            {
                context.Result = new NotFoundObjectResult(new { success = false, message = "用戶不存在" });
                return;
            }

            if (dbUser.AdminRole == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var userRole = dbUser.AdminRole.Value;
            
            // ✅ 優化後的角色判斷邏輯 (Item 5)
            // 使用數字等級比較更安全清晰
            bool hasPermission = false;
            
            // 如果請求的角色列表包含當前角色，直接過度
            if (_roles.Contains(userRole))
            {
                hasPermission = true;
            }
            else
            {
                // 角色等級定義：SuperAdmin(1) < Admin(2) < Editor(3) < Viewer(4)
                // 注意：在 AdminRole.cs 中 SuperAdmin=1，這表示數字越小等級越高
                // 所以判斷邏輯應該是：如果用戶角色的數值 <= 要求的角色中數值最大的那個
                var minRequiredRoleValue = _roles.Select(r => (int)r).Min();
                if ((int)userRole <= minRequiredRoleValue)
                {
                    hasPermission = true;
                }
            }

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<AuthorizeRoleAttribute>)) as ILogger<AuthorizeRoleAttribute>;
            logger?.LogError(ex, "AuthorizeRole 檢查失敗");
            context.Result = new StatusCodeResult(500);
        }
    }
}
