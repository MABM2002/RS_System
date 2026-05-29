using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Controllers;

[Authorize]
public class CategoriasMovimientoController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoriasMovimientoController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: CategoriasMovimiento - Muestra ambas categorías en pestañas
    public async Task<IActionResult> Index()
    {
        var viewModel = new CategoriasMovimientoViewModel
        {
            CategoriasIngreso = await _context.CategoriasIngreso
                .Include(c => c.CuentaContable)
                .OrderBy(c => c.Nombre)
                .ToListAsync(),
            CategoriasEgreso = await _context.CategoriasEgreso
                .Include(c => c.CuentaContable)
                .OrderBy(c => c.Nombre)
                .ToListAsync()
        };

        return View(viewModel);
    }

    #region Categorías de Ingreso

    // GET: CategoriasMovimiento/CreateIngreso
    public async Task<IActionResult> CreateIngreso()
    {
        await LoadCuentasContablesAsync();
        return View(new CategoriaIngreso { Activa = true });
    }

    // POST: CategoriasMovimiento/CreateIngreso
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIngreso([Bind("Nombre,Descripcion,Activa,CuentaContableId")] CategoriaIngreso categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await ExistsNombreIngresoAsync(categoria.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe una categoría de ingreso con ese nombre.");
                await LoadCuentasContablesAsync(categoria.CuentaContableId);
                return View(categoria);
            }

            categoria.FechaCreacion = DateTime.UtcNow;
            _context.CategoriasIngreso.Add(categoria);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Categoría de ingreso creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        await LoadCuentasContablesAsync(categoria.CuentaContableId);
        return View(categoria);
    }

    // GET: CategoriasMovimiento/EditIngreso/5
    public async Task<IActionResult> EditIngreso(long? id)
    {
        if (id == null) return NotFound();

        var categoria = await _context.CategoriasIngreso
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria == null) return NotFound();

        await LoadCuentasContablesAsync(categoria.CuentaContableId);
        return View(categoria);
    }

    // POST: CategoriasMovimiento/EditIngreso/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditIngreso(long id, [Bind("Id,Nombre,Descripcion,Activa,CuentaContableId")] CategoriaIngreso categoria)
    {
        if (id != categoria.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await ExistsNombreIngresoAsync(categoria.Nombre, id))
            {
                ModelState.AddModelError("Nombre", "Ya existe otra categoría de ingreso con ese nombre.");
                await LoadCuentasContablesAsync(categoria.CuentaContableId);
                return View(categoria);
            }

            try
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Categoría de ingreso actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CategoriaIngresoExistsAsync(categoria.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        await LoadCuentasContablesAsync(categoria.CuentaContableId);
        return View(categoria);
    }

    // POST: CategoriasMovimiento/DeleteIngreso/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteIngreso(long id)
    {
        var categoria = await _context.CategoriasIngreso.FindAsync(id);
        if (categoria != null)
        {
            var tieneMovimientos = await _context.MovimientosGenerales
                .AnyAsync(m => m.CategoriaIngresoId == id);

            if (tieneMovimientos)
            {
                TempData["ErrorMessage"] = "No se puede eliminar la categoría porque tiene movimientos asociados.";
                return RedirectToAction(nameof(Index));
            }

            _context.CategoriasIngreso.Remove(categoria);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Categoría de ingreso eliminada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se encontró la categoría de ingreso.";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Categorías de Egreso

    // GET: CategoriasMovimiento/CreateEgreso
    public async Task<IActionResult> CreateEgreso()
    {
        await LoadCuentasContablesAsync();
        return View(new CategoriaEgreso { Activa = true });
    }

    // POST: CategoriasMovimiento/CreateEgreso
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEgreso([Bind("Nombre,Descripcion,Activa,CuentaContableId")] CategoriaEgreso categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await ExistsNombreEgresoAsync(categoria.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe una categoría de egreso con ese nombre.");
                await LoadCuentasContablesAsync(categoria.CuentaContableId);
                return View(categoria);
            }

            categoria.FechaCreacion = DateTime.UtcNow;
            _context.CategoriasEgreso.Add(categoria);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Categoría de egreso creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        await LoadCuentasContablesAsync(categoria.CuentaContableId);
        return View(categoria);
    }

    // GET: CategoriasMovimiento/EditEgreso/5
    public async Task<IActionResult> EditEgreso(long? id)
    {
        if (id == null) return NotFound();

        var categoria = await _context.CategoriasEgreso
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria == null) return NotFound();

        await LoadCuentasContablesAsync(categoria.CuentaContableId);
        return View(categoria);
    }

    // POST: CategoriasMovimiento/EditEgreso/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEgreso(long id, [Bind("Id,Nombre,Descripcion,Activa,CuentaContableId")] CategoriaEgreso categoria)
    {
        if (id != categoria.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await ExistsNombreEgresoAsync(categoria.Nombre, id))
            {
                ModelState.AddModelError("Nombre", "Ya existe otra categoría de egreso con ese nombre.");
                await LoadCuentasContablesAsync(categoria.CuentaContableId);
                return View(categoria);
            }

            try
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Categoría de egreso actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CategoriaEgresoExistsAsync(categoria.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        await LoadCuentasContablesAsync(categoria.CuentaContableId);
        return View(categoria);
    }

    // POST: CategoriasMovimiento/DeleteEgreso/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEgreso(long id)
    {
        var categoria = await _context.CategoriasEgreso.FindAsync(id);
        if (categoria != null)
        {
            var tieneMovimientos = await _context.MovimientosGenerales
                .AnyAsync(m => m.CategoriaEgresoId == id);

            if (tieneMovimientos)
            {
                TempData["ErrorMessage"] = "No se puede eliminar la categoría porque tiene movimientos asociados.";
                return RedirectToAction(nameof(Index));
            }

            _context.CategoriasEgreso.Remove(categoria);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Categoría de egreso eliminada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se encontró la categoría de egreso.";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Helper Methods

    private async Task LoadCuentasContablesAsync(long? selectedCuentaId = null)
    {
        var cuentas = await _context.CuentasContables
            .Where(c => c.Activa)
            .OrderBy(c => c.Codigo)
            .Select(c => new { c.Id, Nombre = $"{c.Codigo} - {c.Nombre}" })
            .ToListAsync();

        ViewBag.CuentasContables = new SelectList(cuentas, "Id", "Nombre", selectedCuentaId);
    }

    private async Task<bool> ExistsNombreIngresoAsync(string nombre, long? excludeId = null)
    {
        var query = _context.CategoriasIngreso
            .Where(c => c.Nombre.ToLower() == nombre.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private async Task<bool> ExistsNombreEgresoAsync(string nombre, long? excludeId = null)
    {
        var query = _context.CategoriasEgreso
            .Where(c => c.Nombre.ToLower() == nombre.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private async Task<bool> CategoriaIngresoExistsAsync(long id)
    {
        return await _context.CategoriasIngreso.AnyAsync(c => c.Id == id);
    }

    private async Task<bool> CategoriaEgresoExistsAsync(long id)
    {
        return await _context.CategoriasEgreso.AnyAsync(c => c.Id == id);
    }

    #endregion
}
