using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Controllers;

[Authorize]
public class PermisoController : Controller
{
    private readonly ApplicationDbContext _context;

    public PermisoController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Permiso
    public async Task<IActionResult> Index()
    {
        var permisos = await _context.Permisos
            .Include(p => p.Modulo)
            .OrderBy(p => p.Modulo!.Orden)
            .ThenBy(p => p.Orden)
            .ToListAsync();
        return View(permisos);
    }

    // GET: Permiso/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Modulos = await _context.Modulos.OrderBy(m => m.Orden).ToListAsync();
        return View();
    }

    // POST: Permiso/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ModuloId,Codigo,Nombre,Descripcion,Url,Icono,Orden,EsMenu")] Permiso permiso)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Permisos.AnyAsync(p => p.Codigo == permiso.Codigo))
            {
                ModelState.AddModelError("Codigo", "El código ya existe.");
                ViewBag.Modulos = await _context.Modulos.OrderBy(m => m.Orden).ToListAsync();
                return View(permiso);
            }

            permiso.CreadoEn = DateTime.UtcNow;
            _context.Add(permiso);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Modulos = await _context.Modulos.OrderBy(m => m.Orden).ToListAsync();
        return View(permiso);
    }

    // GET: Permiso/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var permiso = await _context.Permisos.FindAsync(id);
        if (permiso == null) return NotFound();
        
        ViewBag.Modulos = await _context.Modulos.OrderBy(m => m.Orden).ToListAsync();
        return View(permiso);
    }

    // POST: Permiso/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ModuloId,Codigo,Nombre,Descripcion,Url,Icono,Orden,EsMenu")] Permiso permiso)
    {
        if (id != permiso.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(permiso);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PermisoExists(permiso.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Modulos = await _context.Modulos.OrderBy(m => m.Orden).ToListAsync();
        return View(permiso);
    }

    // POST: Permiso/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var permiso = await _context.Permisos.FindAsync(id);
        if (permiso != null)
        {
            var isUsed = await _context.RolesPermisos.AnyAsync(rp => rp.PermisoId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "No se puede eliminar porque está asignado a roles.";
                return RedirectToAction(nameof(Index));
            }

            _context.Permisos.Remove(permiso);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool PermisoExists(int id)
    {
        return _context.Permisos.Any(e => e.Id == id);
    }
}
