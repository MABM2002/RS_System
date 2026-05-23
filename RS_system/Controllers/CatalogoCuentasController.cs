using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class CatalogoCuentasController : Controller
{
    private readonly IContabilidadPartidaDobleService _contabilidadService;
    private readonly ApplicationDbContext _context;

    public CatalogoCuentasController(
        IContabilidadPartidaDobleService contabilidadService,
        ApplicationDbContext context)
    {
        _contabilidadService = contabilidadService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cuentas = await _contabilidadService.GetAllCuentasAsync();
        
        // Build tree structure in memory
        var arbol = BuildTree(cuentas);
        return View(arbol);
    }

    [HttpGet]
    public async Task<IActionResult> Create(long? padreId)
    {
        var tipos = await _contabilidadService.GetAllAccountTypesAsync();
        ViewBag.TiposCuenta = new SelectList(
            tipos.Where(t => t.Activo).Select(t => new { Value = t.Id, Text = t.Nombre }),
            "Value", "Text");

        var cuentas = await _contabilidadService.GetAllCuentasAsync();
        ViewBag.CuentasPadre = new SelectList(cuentas
            .Select(c => new { c.Id, Nombre = $"{c.CodigoFormateado} - {c.Nombre}" }),
            "Id", "Nombre", padreId);

        if (padreId.HasValue)
        {
            var cuentaPadre = await _contabilidadService.GetCuentaByIdAsync(padreId.Value);
            if (cuentaPadre != null)
            {
                ViewBag.TipoSugerido = cuentaPadre.AccountTypeId;
            }
        }

        return View(new CuentaContable
        {
            Activa = true,
            PadreId = padreId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CuentaContable cuenta)
    {
        if (!ModelState.IsValid)
            return View(cuenta);

        try
        {
            await _contabilidadService.CreateCuentaAsync(cuenta);
            TempData["Success"] = $"Cuenta '{cuenta.Codigo} - {cuenta.Nombre}' creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
        }

        var tipos = await _contabilidadService.GetAllAccountTypesAsync();
        ViewBag.TiposCuenta = new SelectList(
            tipos.Where(t => t.Activo).Select(t => new { Value = t.Id, Text = t.Nombre }),
            "Value", "Text", cuenta.AccountTypeId);

        var cuentas = await _contabilidadService.GetAllCuentasAsync();
        ViewBag.CuentasPadre = new SelectList(cuentas
            .Select(c => new { c.Id, Nombre = $"{c.CodigoFormateado} - {c.Nombre}" }),
            "Id", "Nombre", cuenta.PadreId);

        return View(cuenta);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var cuenta = await _contabilidadService.GetCuentaByIdAsync(id);
        if (cuenta == null) return NotFound();

        var tipos = await _contabilidadService.GetAllAccountTypesAsync();
        ViewBag.TiposCuenta = new SelectList(
            tipos.Where(t => t.Activo).Select(t => new { Value = t.Id, Text = t.Nombre }),
            "Value", "Text", cuenta.AccountTypeId);

        var cuentas = await _contabilidadService.GetAllCuentasAsync();
        ViewBag.CuentasPadre = new SelectList(cuentas
            .Where(c => c.Id != id)
            .Select(c => new { c.Id, Nombre = $"{c.Codigo} - {c.Nombre}" }),
            "Id", "Nombre", cuenta.PadreId);

        return View(cuenta);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, CuentaContable cuenta)
    {
        if (id != cuenta.Id) return BadRequest();

        if (!ModelState.IsValid)
            return View(cuenta);

        try
        {
            await _contabilidadService.UpdateCuentaAsync(cuenta);
            TempData["Success"] = $"Cuenta '{cuenta.Codigo} - {cuenta.Nombre}' actualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
        }

        var tipos = await _contabilidadService.GetAllAccountTypesAsync();
        ViewBag.TiposCuenta = new SelectList(
            tipos.Where(t => t.Activo).Select(t => new { Value = t.Id, Text = t.Nombre }),
            "Value", "Text", cuenta.AccountTypeId);

        var cuentas = await _contabilidadService.GetAllCuentasAsync();
        ViewBag.CuentasPadre = new SelectList(cuentas
            .Where(c => c.Id != id)
            .Select(c => new { c.Id, Nombre = $"{c.CodigoFormateado} - {c.Nombre}" }),
            "Id", "Nombre", cuenta.PadreId);

        return View(cuenta);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var success = await _contabilidadService.DeleteCuentaAsync(id);
            if (success)
            {
                TempData["Success"] = "Cuenta eliminada exitosamente.";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // ==================== Helpers ====================

    private List<CuentaContable> BuildTree(List<CuentaContable> todas)
    {
        var raices = todas.Where(c => c.PadreId == null)
            .OrderBy(c => c.Codigo)
            .ToList();

        foreach (var raiz in raices)
        {
            raiz.Hijas = GetHijas(raiz.Id, todas);
        }

        return raices;
    }

    private List<CuentaContable> GetHijas(long padreId, List<CuentaContable> todas)
    {
        var hijas = todas.Where(c => c.PadreId == padreId)
            .OrderBy(c => c.Codigo)
            .ToList();

        foreach (var hija in hijas)
        {
            hija.Hijas = GetHijas(hija.Id, todas);
        }

        return hijas;
    }
}
