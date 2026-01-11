using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rs_system.Services;
using System.Security.Claims;

namespace Rs_system.Filters;

public class PermissionAttribute : TypeFilterAttribute
{
    public PermissionAttribute(string permissionCode) : base(typeof(PermissionFilter))
    {
        Arguments = new object[] { permissionCode };
    }
}

public class PermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _permissionCode;
    private readonly IAuthService _authService;

    public PermissionFilter(string permissionCode, IAuthService authService)
    {
        _permissionCode = permissionCode;
        _authService = authService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var hasPermission = await _authService.HasPermissionAsync(userId, _permissionCode);
        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
