using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Controllers;

[Authorize]
public class ModuloController : Controller
{
    private readonly ApplicationDbContext _context;

    public ModuloController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Modulo
    public async Task<IActionResult> Index()
    {
        var modulos = await _context.Modulos
            .Include(m => m.Parent)
            .OrderBy(m => m.Orden)
            .ToListAsync();
        return View(modulos);
    }

    // GET: Modulo/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.ModulosPadre = await _context.Modulos
            .Where(m => m.Activo)
            .OrderBy(m => m.Orden)
            .ToListAsync();
        return View();
    }

    // POST: Modulo/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Icono,Orden,Activo,ParentId")] Modulo modulo)
    {
        if (ModelState.IsValid)
        {
            modulo.CreadoEn = DateTime.UtcNow;
            _context.Add(modulo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.ModulosPadre = await _context.Modulos
            .Where(m => m.Activo)
            .OrderBy(m => m.Orden)
            .ToListAsync();
        return View(modulo);
    }

    // GET: Modulo/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var modulo = await _context.Modulos.FindAsync(id);
        if (modulo == null) return NotFound();
        
        // Exclude current module and its children from parent options
        ViewBag.ModulosPadre = await _context.Modulos
            .Where(m => m.Activo && m.Id != id)
            .OrderBy(m => m.Orden)
            .ToListAsync();
        return View(modulo);
    }

    // POST: Modulo/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Icono,Orden,Activo,ParentId")] Modulo modulo)
    {
        if (id != modulo.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(modulo);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ModuloExists(modulo.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.ModulosPadre = await _context.Modulos
            .Where(m => m.Activo && m.Id != id)
            .OrderBy(m => m.Orden)
            .ToListAsync();
        return View(modulo);
    }

    // POST: Modulo/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var modulo = await _context.Modulos.FindAsync(id);
        if (modulo != null)
        {
            var isUsed = await _context.Permisos.AnyAsync(p => p.ModuloId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "No se puede eliminar porque tiene permisos asociados.";
                return RedirectToAction(nameof(Index));
            }

            _context.Modulos.Remove(modulo);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ModuloExists(int id)
    {
        return _context.Modulos.Any(e => e.Id == id);
    }
}
