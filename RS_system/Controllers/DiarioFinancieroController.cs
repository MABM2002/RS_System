using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reports.PdfEngine.Abstractions;
using Rs_system.Models;
using Rs_system.Models.ViewModels;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class DiarioFinancieroController : Controller
{
    private readonly IDiarioFinancieroService _diarioService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IReportGenerator _reportGenerator;

    public DiarioFinancieroController(
        IDiarioFinancieroService diarioService,
        IFileStorageService fileStorageService,
        IReportGenerator reportGenerator)
    {
        _diarioService = diarioService;
        _fileStorageService = fileStorageService;
        _reportGenerator = reportGenerator;
    }

    private string UsuarioActual => User.Identity?.Name ?? "Sistema";

    // ==================== Lista de Diarios ====================

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, string? estado)
    {
        ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
        ViewBag.Estado = estado;

        var cabeceras = await _diarioService.ListarCabecerasAsync(fechaInicio, fechaFin, estado);
        return View(cabeceras);
    }

    // ==================== Crear Cabecera ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(DateTime fecha, string? observaciones)
    {
        try
        {
            var cabecera = await _diarioService.CrearCabeceraAsync(fecha, observaciones, UsuarioActual);
            TempData["Success"] = $"Diario del {cabecera.Fecha:dd/MM/yyyy} creado correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = cabecera.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // ==================== Detalle (Cabecera + Movimientos) ====================

    [HttpGet]
    public async Task<IActionResult> Detalle(long id)
    {
        var cabecera = await _diarioService.ObtenerCabeceraAsync(id);
        if (cabecera == null) return NotFound();

        var vm = new DiarioFinancieroDetalleViewModel
        {
            Cabecera = cabecera,
            CategoriasIngreso = await _diarioService.ObtenerCategoriasIngresoAsync(),
            CategoriasEgreso = await _diarioService.ObtenerCategoriasEgresoAsync(),
            MetodosPago = await _diarioService.ObtenerMetodosPagoAsync()
        };

        return View(vm);
    }

    // ==================== Guardar Movimiento (AJAX) ====================

    [HttpPost]
    public async Task<IActionResult> GuardarMovimiento([FromBody] DiarioMovimientoInput input)
    {
        if (input == null || input.CabeceraId <= 0)
            return BadRequest(new { success = false, message = "Datos inválidos." });

        if (string.IsNullOrWhiteSpace(input.Descripcion))
            return BadRequest(new { success = false, message = "La descripción es obligatoria." });

        if (input.Monto <= 0)
            return BadRequest(new { success = false, message = "El monto debe ser mayor a cero." });

        var detalle = await _diarioService.GuardarMovimientoAsync(input, UsuarioActual);
        if (detalle == null)
            return Json(new { success = false, message = "No se pudo guardar. El diario puede estar cerrado." });

        // Get updated header totals
        var cabecera = await _diarioService.ObtenerCabeceraAsync(input.CabeceraId);

        return Json(new
        {
            success = true,
            detalleId = detalle.Id,
            totalIngresos = cabecera?.TotalIngresos ?? 0,
            totalEgresos = cabecera?.TotalEgresos ?? 0,
            saldoDia = cabecera?.SaldoDia ?? 0,
            message = input.Id > 0 ? "Movimiento actualizado." : "Movimiento registrado."
        });
    }

    // ==================== Eliminar Movimiento (AJAX) ====================

    [HttpPost]
    public async Task<IActionResult> EliminarMovimiento(long id, long cabeceraId)
    {
        var success = await _diarioService.EliminarMovimientoAsync(id, UsuarioActual);
        if (!success)
            return Json(new { success = false, message = "No se pudo eliminar. El diario puede estar cerrado." });

        var cabecera = await _diarioService.ObtenerCabeceraAsync(cabeceraId);
        return Json(new
        {
            success = true,
            totalIngresos = cabecera?.TotalIngresos ?? 0,
            totalEgresos = cabecera?.TotalEgresos ?? 0,
            saldoDia = cabecera?.SaldoDia ?? 0,
            message = "Movimiento eliminado."
        });
    }

    // ==================== Cerrar / Reabrir Diario ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarDiario(long id)
    {
        var success = await _diarioService.CerrarDiarioAsync(id, UsuarioActual);
        TempData[success ? "Success" : "Error"] = success
            ? "Diario cerrado. Ya no se permiten modificaciones."
            : "No se pudo cerrar el diario.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReabrirDiario(long id)
    {
        var success = await _diarioService.ReabrirDiarioAsync(id, UsuarioActual);
        TempData[success ? "Success" : "Error"] = success
            ? "Diario reabierto. Se permiten modificaciones."
            : "No se pudo reabrir el diario.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ==================== Eliminar Cabecera ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCabecera(long id)
    {
        var success = await _diarioService.EliminarCabeceraAsync(id);
        TempData[success ? "Success" : "Error"] = success
            ? "Diario eliminado correctamente."
            : "No se pudo eliminar. El diario puede estar cerrado.";
        return RedirectToAction(nameof(Index));
    }

    // ==================== Actualizar Observaciones ====================

    [HttpPost]
    public async Task<IActionResult> ActualizarObservaciones([FromBody] ActualizarObservacionesInput input)
    {
        if (input == null || input.Id <= 0)
            return BadRequest(new { success = false });

        var success = await _diarioService.ActualizarObservacionesCabeceraAsync(input.Id, input.Observaciones, UsuarioActual);
        return Json(new { success });
    }

    public class ActualizarObservacionesInput
    {
        public long Id { get; set; }
        public string? Observaciones { get; set; }
    }

    // ==================== Adjuntos ====================

    [HttpGet]
    public async Task<IActionResult> ObtenerAdjuntos(long detalleId)
    {
        var adjuntos = await _diarioService.ObtenerAdjuntosAsync(detalleId);
        return Json(adjuntos.Select(a => new
        {
            id = a.Id,
            nombre = a.NombreArchivo,
            url = _fileStorageService.GetFileUrl(a.RutaArchivo),
            tipo = a.TipoContenido,
            fecha = a.FechaSubida.ToLocalTime().ToString("g")
        }));
    }

    [HttpPost]
    public async Task<IActionResult> SubirAdjunto(long detalleId, List<IFormFile> archivos)
    {
        if (detalleId <= 0 || archivos == null || !archivos.Any())
            return BadRequest(new { success = false, message = "Datos inválidos." });

        int count = 0;
        foreach (var archivo in archivos)
        {
            if (archivo.Length > 0)
            {
                var ruta = await _fileStorageService.SaveFileAsync(archivo, "diario_financiero");
                if (!string.IsNullOrEmpty(ruta))
                {
                    await _diarioService.CrearAdjuntoAsync(detalleId, archivo.FileName, ruta, archivo.ContentType);
                    count++;
                }
            }
        }

        return Json(new { success = true, count, message = $"{count} archivo(s) subido(s) correctamente." });
    }

    [HttpPost]
    public async Task<IActionResult> EliminarAdjunto(long id)
    {
        var success = await _diarioService.EliminarAdjuntoAsync(id);
        return Json(new { success });
    }

    // ==================== Reporte ====================

    [HttpGet]
    public async Task<IActionResult> Reporte(DiarioFinancieroFiltroViewModel filtro)
    {
        var vm = await _diarioService.GenerarReporteAsync(filtro);
        return View(vm);
    }

    // ==================== Descargar PDF ====================

    [HttpGet]
    public async Task<IActionResult> DescargarPdf(DiarioFinancieroFiltroViewModel filtro)
    {
        try
        {
            var vm = await _diarioService.GenerarReporteAsync(filtro);
            byte[] pdfBytes = await _reportGenerator.GenerateAsync("DiarioFinanciero", vm);
            var fileName = $"DiarioFinanciero_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar el PDF: {ex.Message}";
            return RedirectToAction(nameof(Reporte), filtro);
        }
    }
}
