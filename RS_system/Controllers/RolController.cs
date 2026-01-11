using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Controllers;

[Authorize]
public class RolController : Controller
{
    private readonly ApplicationDbContext _context;

    public RolController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Rol
    public async Task<IActionResult> Index()
    {
        return View(await _context.RolesSistema
            .Include(r => r.RolesPermisos)
            .OrderBy(r => r.Nombre)
            .ToListAsync());
    }

    // GET: Rol/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Rol/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Codigo,Nombre,Descripcion")] RolSistema rol)
    {
        if (ModelState.IsValid)
        {
            if (await _context.RolesSistema.AnyAsync(r => r.Codigo == rol.Codigo))
            {
                ModelState.AddModelError("Codigo", "El código de rol ya existe.");
                return View(rol);
            }

            rol.CreadoEn = DateTime.UtcNow;
            _context.Add(rol);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(rol);
    }

    // GET: Rol/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var rol = await _context.RolesSistema.FindAsync(id);
        if (rol == null) return NotFound();
        return View(rol);
    }

    // POST: Rol/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Codigo,Nombre,Descripcion")] RolSistema rol)
    {
        if (id != rol.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(rol);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RolExists(rol.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(rol);
    }

    // GET: Rol/Permissions/5
    public async Task<IActionResult> Permissions(int? id)
    {
        if (id == null) return NotFound();

        var rol = await _context.RolesSistema
            .Include(r => r.RolesPermisos)
                .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rol == null) return NotFound();

        // Fetch all permissions from DB
        var permissions = await _context.Permisos
            .OrderBy(p => p.Modulo)
            .ThenBy(p => p.Orden)
            .ToListAsync();

        ViewBag.Rol = rol;
        ViewBag.AssignedControllerCodes = rol.RolesPermisos.Select(rp => rp.Permiso.Codigo).ToList();

        return View(permissions);
    }

    // POST: Rol/UpdatePermissions
    [HttpPost]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> UpdatePermissions(int rolId, string[] selectedControllers)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                var rol = await _context.RolesSistema
                    .Include(r => r.RolesPermisos)
                    .FirstOrDefaultAsync(r => r.Id == rolId);

                if (rol == null) throw new InvalidOperationException("Rol no encontrado");

                // Remove existing permissions
                _context.RolesPermisos.RemoveRange(rol.RolesPermisos);
                await _context.SaveChangesAsync();

                // Add new permissions
                if (selectedControllers != null)
                {
                    foreach (var controllerCode in selectedControllers)
                    {
                        var permiso = await _context.Permisos.FirstOrDefaultAsync(p => p.Codigo == controllerCode);
                        if (permiso != null)
                        {
                            _context.RolesPermisos.Add(new RolPermiso
                            {
                                RolId = rolId,
                                PermisoId = permiso.Id,
                                AsignadoEn = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            
            TempData["SuccessMessage"] = "Permisos actualizados correctamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ocurrió un error al actualizar los permisos: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Rol/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var rol = await _context.RolesSistema.FindAsync(id);
        if (rol != null)
        {
            // Check if it's being used by users
            var isUsed = await _context.RolesUsuario.AnyAsync(ru => ru.RolId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "No se puede eliminar el rol porque está asignado a uno o más usuarios.";
                return RedirectToAction(nameof(Index));
            }

            // Remove permissions first
            var permissions = await _context.RolesPermisos.Where(rp => rp.RolId == id).ToListAsync();
            _context.RolesPermisos.RemoveRange(permissions);

            _context.RolesSistema.Remove(rol);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool RolExists(int id)
    {
        return _context.RolesSistema.Any(e => e.Id == id);
    }
}
