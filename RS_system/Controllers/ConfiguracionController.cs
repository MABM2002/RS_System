using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

using Microsoft.AspNetCore.Authorization;

namespace Rs_system.Controllers;

[Authorize]
public class ConfiguracionController : Controller
{
    private readonly ApplicationDbContext _context;

    public ConfiguracionController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Configuracion
    public async Task<IActionResult> Index(string? categoria)
    {
        var query = _context.Configuraciones.AsQueryable();

        if (!string.IsNullOrEmpty(categoria))
        {
            query = query.Where(c => c.Categoria == categoria);
        }

        var configuraciones = await query
            .OrderBy(c => c.Categoria)
            .ThenBy(c => c.Grupo)
            .ThenBy(c => c.Orden)
            .ToListAsync();

        ViewBag.Categorias = await _context.Configuraciones
            .Select(c => c.Categoria)
            .Distinct()
            .ToListAsync();
            
        ViewBag.SelectedCategoria = categoria;

        return View(configuraciones);
    }

    // GET: Configuracion/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var config = await _context.Configuraciones.FindAsync(id);
        if (config == null || !config.EsEditable) return NotFound();

        return View(config);
    }

    // POST: Configuracion/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Valor")] ConfiguracionSistema model)
    {
        if (id != model.Id) return NotFound();

        var config = await _context.Configuraciones.FindAsync(id);
        if (config == null || !config.EsEditable) return NotFound();

        try
        {
            config.Valor = model.Valor;
            config.ActualizadoEn = DateTime.UtcNow;
            
            _context.Update(config);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index), new { categoria = config.Categoria });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ConfiguracionExists(model.Id)) return NotFound();
            else throw;
        }
    }

    private bool ConfiguracionExists(int id)
    {
        return _context.Configuraciones.Any(e => e.Id == id);
    }
}
