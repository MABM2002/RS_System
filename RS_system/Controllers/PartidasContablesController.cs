using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class PartidasContablesController : Controller
{
    private readonly IContabilidadPartidaDobleService _contabilidadService;
    private readonly ApplicationDbContext _context;

    public PartidasContablesController(
        IContabilidadPartidaDobleService contabilidadService,
        ApplicationDbContext context)
    {
        _contabilidadService = contabilidadService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(long? periodoId)
    {
        var periodos = await _contabilidadService.GetAllPeriodosAsync();
        ViewBag.Periodos = new SelectList(
            periodos.Select(p => new { p.Id, Nombre = $"{p.NombreMes} {p.Anio}" }),
            "Id", "Nombre", periodoId ?? periodos.FirstOrDefault()?.Id);

        var selectedPeriodoId = periodoId ?? periodos.FirstOrDefault()?.Id;
        List<PartidaContable> partidas = new();
        
        if (selectedPeriodoId.HasValue)
        {
            partidas = await _contabilidadService.GetPartidasByPeriodoAsync(selectedPeriodoId.Value);
        }

        ViewBag.PeriodoId = selectedPeriodoId;
        return View(partidas);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var partida = await _contabilidadService.GetPartidaByIdAsync(id);
        if (partida == null) return NotFound();

        return View(partida);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var periodos = await _contabilidadService.GetAllPeriodosAsync();
        var periodoActual = periodos.FirstOrDefault(p => !p.Cerrado);

        ViewBag.Periodos = new SelectList(
            periodos.Where(p => !p.Cerrado)
                .Select(p => new { p.Id, Nombre = $"{p.NombreMes} {p.Anio}" }),
            "Id", "Nombre", periodoActual?.Id);

        var cuentas = await _contabilidadService.GetAllCuentasAsync();
        ViewBag.Cuentas = new SelectList(
            cuentas.Where(c => c.Activa)
                .Select(c => new { c.Id, Nombre = $"{c.Codigo} - {c.Nombre}" }),
            "Id", "Nombre");

        return View(new PartidaContableViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PartidaContableViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var periodos = await _contabilidadService.GetAllPeriodosAsync();
            ViewBag.Periodos = new SelectList(
                periodos.Where(p => !p.Cerrado)
                    .Select(p => new { p.Id, Nombre = $"{p.NombreMes} {p.Anio}" }),
                "Id", "Nombre", model.PeriodoContableId);

            var cuentas = await _contabilidadService.GetAllCuentasAsync();
            ViewBag.Cuentas = new SelectList(
                cuentas.Where(c => c.Activa)
                    .Select(c => new { c.Id, Nombre = $"{c.Codigo} - {c.Nombre}" }),
                "Id", "Nombre");

            return View(model);
        }

        // Validar que haya al menos 2 líneas de detalle
        if (model.Detalles == null || model.Detalles.Count(d => d.Debito.HasValue || d.Credito.HasValue) < 2)
        {
            ModelState.AddModelError("", "Debe agregar al menos 2 líneas de detalle (débito y crédito).");

            var periodos = await _contabilidadService.GetAllPeriodosAsync();
            ViewBag.Periodos = new SelectList(
                periodos.Where(p => !p.Cerrado)
                    .Select(p => new { p.Id, Nombre = $"{p.NombreMes} {p.Anio}" }),
                "Id", "Nombre", model.PeriodoContableId);

            var cuentas = await _contabilidadService.GetAllCuentasAsync();
            ViewBag.Cuentas = new SelectList(
                cuentas.Where(c => c.Activa)
                    .Select(c => new { c.Id, Nombre = $"{c.Codigo} - {c.Nombre}" }),
                "Id", "Nombre");

            return View(model);
        }

        try
        {
            var partida = new PartidaContable
            {
                Fecha = model.Fecha,
                Referencia = model.Referencia,
                Descripcion = model.Descripcion,
                PeriodoContableId = model.PeriodoContableId
            };

            var detalles = model.Detalles
                .Where(d => d.CuentaContableId.HasValue && (d.Debito.GetValueOrDefault() > 0 || d.Credito.GetValueOrDefault() > 0))
                .Select(d => new DetallePartidaContable
                {
                    CuentaContableId = d.CuentaContableId!.Value,
                    Debito = d.Debito.GetValueOrDefault(),
                    Credito = d.Credito.GetValueOrDefault(),
                    Descripcion = d.Descripcion
                })
                .ToList();

            await _contabilidadService.CreatePartidaAsync(partida, detalles);
            TempData["Success"] = "Partida contable registrada exitosamente.";
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

        var periodos2 = await _contabilidadService.GetAllPeriodosAsync();
        ViewBag.Periodos = new SelectList(
            periodos2.Where(p => !p.Cerrado)
                .Select(p => new { p.Id, Nombre = $"{p.NombreMes} {p.Anio}" }),
            "Id", "Nombre", model.PeriodoContableId);

        var cuentas2 = await _contabilidadService.GetAllCuentasAsync();
        ViewBag.Cuentas = new SelectList(
            cuentas2.Where(c => c.Activa)
                .Select(c => new { c.Id, Nombre = $"{c.Codigo} - {c.Nombre}" }),
            "Id", "Nombre");

        return View(model);
    }
}
