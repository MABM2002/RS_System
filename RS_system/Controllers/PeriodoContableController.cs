using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class PeriodoContableController : Controller
{
    private readonly IContabilidadPartidaDobleService _contabilidadService;

    public PeriodoContableController(IContabilidadPartidaDobleService contabilidadService)
    {
        _contabilidadService = contabilidadService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var periodos = await _contabilidadService.GetAllPeriodosAsync();
        return View(periodos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarPeriodo(long id)
    {
        try
        {
            var periodo = await _contabilidadService.CerrarPeriodoAsync(id, User.Identity?.Name ?? "Sistema");
            TempData["Success"] = $"Período {periodo.NombreMes} {periodo.Anio} cerrado exitosamente.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReabrirPeriodo(long id)
    {
        try
        {
            var periodo = await _contabilidadService.ReabrirPeriodoAsync(id);
            TempData["Success"] = $"Período {periodo.NombreMes} {periodo.Anio} reabierto exitosamente.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
