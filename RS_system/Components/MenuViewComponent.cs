using Microsoft.AspNetCore.Mvc;
using Rs_system.Services;
using System.Security.Claims;

namespace Rs_system.Components;

public class MenuViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;

    public MenuViewComponent(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return View(new Rs_system.Models.ViewModels.MenuViewModel());
        }

        var isRoot = HttpContext.User.IsInRole("ROOT");
        var menuViewModel = await _menuService.GetUserMenuAsync(userId, isRoot);

        return View(menuViewModel);
    }
}
