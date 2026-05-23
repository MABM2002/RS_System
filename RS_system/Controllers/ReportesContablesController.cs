using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Models;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class ReportesContablesController : Controller
{
    private readonly IContabilidadPartidaDobleService _contabilidadService;

    public ReportesContablesController(IContabilidadPartidaDobleService contabilidadService)
    {
        _contabilidadService = contabilidadService;
    }

    [HttpGet]
    public async Task<IActionResult> BalanceGeneral(DateTime? fechaCorte)
    {
        var corte = fechaCorte ?? DateTime.Today;
        ViewBag.FechaCorte = corte;

        try
        {
            var resultado = await _contabilidadService.GetBalanceGeneralAsync(corte);
            return View(resultado);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al calcular el Balance General: {ex.Message}";
            return View(new BalanceGeneralResult { FechaCorte = corte });
        }
    }

    [HttpGet]
    public async Task<IActionResult> EstadoResultados(int? mes, int? anio)
    {
        var mesActual = mes ?? DateTime.Now.Month;
        var anioActual = anio ?? DateTime.Now.Year;

        ViewBag.Mes = mesActual;
        ViewBag.Anio = anioActual;
        ViewBag.Meses = new SelectList(Enumerable.Range(1, 12)
            .Select(m => new
            {
                Value = m,
                Text = new DateTime(anioActual, m, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"))
            }), "Value", "Text", mesActual);

        var aniosDisponibles = Enumerable.Range(DateTime.Now.Year - 5, 10).Reverse();
        ViewBag.Anios = new SelectList(aniosDisponibles, anioActual);

        try
        {
            var resultado = await _contabilidadService.GetEstadoResultadosAsync(mesActual, anioActual);
            return View(resultado);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al calcular el Estado de Resultados: {ex.Message}";
            return View(new EstadoResultadosResult { Mes = mesActual, Anio = anioActual });
        }
    }
}
