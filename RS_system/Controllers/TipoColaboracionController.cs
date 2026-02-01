using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class TipoColaboracionController : Controller
{
    private readonly IColaboracionService _colaboracionService;
    private readonly ApplicationDbContext _context;
    
    public TipoColaboracionController(IColaboracionService colaboracionService, ApplicationDbContext context)
    {
        _colaboracionService = colaboracionService;
        _context = context;
    }
    
    // GET: TipoColaboracion
    public async Task<IActionResult> Index()
    {
        try
        {
            var tipos = await _context.TiposColaboracion
                .OrderBy(t => t.Orden)
                .ToListAsync();
            return View(tipos);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar tipos: {ex.Message}";
            return View(new List<TipoColaboracion>());
        }
    }
    
    // GET: TipoColaboracion/Create
    public IActionResult Create()
    {
        var model = new TipoColaboracion
        {
            MontoSugerido = 1.00m,
            Activo = true
        };
        return View(model);
    }
    
    // POST: TipoColaboracion/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TipoColaboracion model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                model.CreadoEn = DateTime.UtcNow;
                model.ActualizadoEn = DateTime.UtcNow;
                
                _context.TiposColaboracion.Add(model);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Tipo de colaboración creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear: {ex.Message}");
            }
        }
        
        return View(model);
    }
    
    // GET: TipoColaboracion/Edit/5
    public async Task<IActionResult> Edit(long id)
    {
        try
        {
            var tipo = await _context.TiposColaboracion.FindAsync(id);
            if (tipo == null)
            {
                TempData["Error"] = "Tipo de colaboración no encontrado";
                return RedirectToAction(nameof(Index));
            }
            
            return View(tipo);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar tipo: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
    
    // POST: TipoColaboracion/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, TipoColaboracion model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }
        
        if (ModelState.IsValid)
        {
            try
            {
                var tipo = await _context.TiposColaboracion.FindAsync(id);
                if (tipo == null)
                {
                    TempData["Error"] = "Tipo de colaboración no encontrado";
                    return RedirectToAction(nameof(Index));
                }
                
                tipo.Nombre = model.Nombre;
                tipo.Descripcion = model.Descripcion;
                tipo.MontoSugerido = model.MontoSugerido;
                tipo.Activo = model.Activo;
                tipo.Orden = model.Orden;
                tipo.ActualizadoEn = DateTime.UtcNow;
                _context.TiposColaboracion.Update(tipo);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Tipo de colaboración actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
            }
        }
        
        return View(model);
    }
    
    // POST: TipoColaboracion/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var tipo = await _context.TiposColaboracion.FindAsync(id);
            if (tipo == null)
            {
                TempData["Error"] = "Tipo de colaboración no encontrado";
                return RedirectToAction(nameof(Index));
            }
            
            // Soft delete - just deactivate
            tipo.Activo = false;
            tipo.ActualizadoEn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Tipo de colaboración desactivado exitosamente";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al desactivar: {ex.Message}";
        }
        
        return RedirectToAction(nameof(Index));
    }
}
