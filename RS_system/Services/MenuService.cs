using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public interface IMenuService
{
    Task<MenuViewModel> GetUserMenuAsync(long userId, bool isRoot);
}

public class MenuService : IMenuService
{
    private readonly ApplicationDbContext _context;
    private readonly IQueryCacheService _cache;
    private readonly ILogger<MenuService> _logger;

    public MenuService(ApplicationDbContext context, IQueryCacheService cache, ILogger<MenuService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<MenuViewModel> GetUserMenuAsync(long userId, bool isRoot)
    {
        var cacheKey = $"menu_{userId}_{isRoot}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var userPermisoIds = await GetUserPermissionIdsAsync(userId, isRoot);
            var allModules = await GetAllActiveModulesAsync();
            
            return BuildMenuViewModel(allModules, userPermisoIds);
        }, TimeSpan.FromMinutes(15));
    }

    private async Task<List<int>> GetUserPermissionIdsAsync(long userId, bool isRoot)
    {
        if (isRoot)
        {
            return await _context.Permisos
                .AsNoTracking()
                .Where(p => p.EsMenu)
                .Select(p => p.Id)
                .ToListAsync();
        }

        return await _context.RolesUsuario
            .AsNoTracking()
            .Where(ru => ru.UsuarioId == userId)
            .Select(ru => ru.RolId)
            .Distinct()
            .Join(_context.RolesPermisos.AsNoTracking(),
                rolId => rolId,
                rp => rp.RolId,
                (rolId, rp) => rp.PermisoId)
            .Join(_context.Permisos.AsNoTracking(),
                permisoId => permisoId,
                p => p.Id,
                (permisoId, p) => new { permisoId, p.EsMenu })
            .Where(x => x.EsMenu)
            .Select(x => x.permisoId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<List<Modulo>> GetAllActiveModulesAsync()
    {
        return await _context.Modulos
            .AsNoTracking()
            .Include(m => m.Permisos.Where(p => p.EsMenu))
            .Where(m => m.Activo)
            .OrderBy(m => m.Orden)
            .ToListAsync();
    }

    private MenuViewModel BuildMenuViewModel(List<Modulo> allModules, List<int> userPermisoIds)
    {
        var menuViewModel = new MenuViewModel();
        
        // Build the tree starting from root modules (ParentId == null)
        var rootModules = allModules.Where(m => m.ParentId == null).OrderBy(m => m.Orden);

        foreach (var module in rootModules)
        {
            var menuItem = BuildModuleMenuItem(module, allModules, userPermisoIds);
            if (menuItem != null)
            {
                menuViewModel.Items.Add(menuItem);
            }
        }

        return menuViewModel;
    }

    private MenuItem? BuildModuleMenuItem(Modulo module, List<Modulo> allModules, List<int> userPermisoIds)
    {
        var item = new MenuItem
        {
            Title = module.Nombre,
            Icon = module.Icono,
            IsGroup = true,
            Order = module.Orden
        };

        // 1. Add Submodules
        var subModules = allModules.Where(m => m.ParentId == module.Id).OrderBy(m => m.Orden);
        foreach (var sub in subModules)
        {
            var subItem = BuildModuleMenuItem(sub, allModules, userPermisoIds);
            if (subItem != null)
            {
                item.Children.Add(subItem);
            }
        }

        // 2. Add Direct Permissions (Menu Items)
        var permissions = module.Permisos
            .Where(p => userPermisoIds.Contains(p.Id))
            .OrderBy(p => p.Orden);

        foreach (var p in permissions)
        {
            item.Children.Add(new MenuItem
            {
                Title = p.Nombre,
                Icon = p.Icono,
                Url = p.Url,
                IsGroup = false,
                Order = p.Orden
            });
        }

        // Only return the item if it has children (permissions or submodules with permissions)
        return item.Children.Any() ? item : null;
    }
}