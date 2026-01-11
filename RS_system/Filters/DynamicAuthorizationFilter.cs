using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rs_system.Filters;

public class DynamicAuthorizationFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip if user is not authenticated
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Get the controller action descriptor
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return;
        }

        // Allow access to Account and Home controllers by default for authenticated users
        var controllerName = descriptor.ControllerName;
        if (controllerName.Equals("Account", StringComparison.OrdinalIgnoreCase) ||
            controllerName.Equals("Home", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        
        // Check for AllowAnonymous attribute
        if (descriptor.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any() ||
            descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any())
        {
            return;
        }

        var user = context.HttpContext.User;

        // ROOT role always has access
        if (user.IsInRole("ROOT"))
        {
            return;
        }

        // Check if user has permission for this controller
        // The permission code is expected to match the Controller Name (e.g., "Usuario", "Rol", "Colaborador")
        // In AccountController, we added claims of type "Permission" with the permission code
        var hasPermission = user.HasClaim(c => c.Type == "Permission" && 
                                             c.Value.ToUpperInvariant().Equals(controllerName.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
        
        await Task.CompletedTask;
    }
}
