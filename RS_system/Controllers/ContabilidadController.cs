using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Models;
using Rs_system.Services;
using Rs_system.Data;
using Microsoft.EntityFrameworkCore;

namespace Rs_system.Controllers;

[Authorize]
public class ContabilidadController : Controller
{
    private readonly IContabilidadService _contabilidadService;
    private readonly IMiembroService _miembroService;
    private readonly ApplicationDbContext _context;

    public ContabilidadController(IContabilidadService contabilidadService, IMiembroService miembroService, ApplicationDbContext context)
    {
        _contabilidadService = contabilidadService;
        _miembroService = miembroService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(long? grupoId)
    {
        var grupos = await _miembroService.GetGruposTrabajoAsync();
        ViewBag.Grupos = new SelectList(grupos.Select(g => new { g.Id, g.Nombre }), "Id", "Nombre", grupoId);

        List<ReporteMensualContable> reportes = new();
        if (grupoId.HasValue)
        {
            reportes = await _contabilidadService.ListarReportesPorGrupoAsync(grupoId.Value);
        }

        ViewBag.GrupoId = grupoId;
        return View(reportes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AbrirMes(long grupoId, int mes, int anio)
    {
        try
        {
            var reporte = await _contabilidadService.ObtenerOCrearReporteMensualAsync(grupoId, mes, anio);
            TempData["Success"] = $"Reporte de {reporte.NombreMes} {anio} abierto correctamente.";
            return RedirectToAction(nameof(RegistroMensual), new { id = reporte.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al abrir el mes: " + ex.Message;
            return RedirectToAction(nameof(Index), new { grupoId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> RegistroMensual(long id)
    {
        var reporte = await _context.ReportesMensualesContables
            .Include(r => r.GrupoTrabajo)
            .Include(r => r.Registros)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reporte == null) return NotFound();

        ViewBag.SaldoActual = await _contabilidadService.CalcularSaldoActualAsync(id);
        return View(reporte);
    }

    [HttpPost]
    public async Task<IActionResult> GuardarBulk([FromBody] BulkSaveRequest request)
    {
        if (request == null || request.ReporteId <= 0) return BadRequest("Solicitud inválida.");

        var registros = request.Registros.Select(r => new ContabilidadRegistro
        {
            Id = r.Id,
            Tipo = r.Tipo,
            Monto = r.Monto,
            Fecha = DateTime.SpecifyKind(r.Fecha, DateTimeKind.Utc),
            Descripcion = r.Descripcion ?? ""
        }).ToList();

        var success = await _contabilidadService.GuardarRegistrosBulkAsync(request.ReporteId, registros);
        if (success)
        {
            var nuevoSaldo = await _contabilidadService.CalcularSaldoActualAsync(request.ReporteId);
            return Json(new { success = true, saldo = nuevoSaldo });
        }

        return Json(new { success = false, message = "Error al guardar los registros. Verifique que el mes no esté cerrado." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarMes(long id)
    {
        var success = await _contabilidadService.CerrarReporteAsync(id);
        if (success)
        {
            TempData["Success"] = "El reporte ha sido cerrado. Ya no se pueden realizar cambios.";
        }
        else
        {
            TempData["Error"] = "No se pudo cerrar el reporte.";
        }
        return RedirectToAction(nameof(RegistroMensual), new { id });
    }

    // Helper classes for AJAX
    public class BulkSaveRequest
    {
        public long ReporteId { get; set; }
        public List<RegistroInput> Registros { get; set; } = new();
    }

    public class RegistroInput
    {
        public long Id { get; set; }
        public TipoMovimientoContable Tipo { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string? Descripcion { get; set; }
    }
}
