using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Models;
using Rs_system.Services;
using Rs_system.Data;
using Microsoft.EntityFrameworkCore;

namespace Rs_system.Controllers;

[Authorize]
public class ContabilidadGeneralController : Controller
{
    private readonly IContabilidadGeneralService _contabilidadService;
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public ContabilidadGeneralController(IContabilidadGeneralService contabilidadService, ApplicationDbContext context, IFileStorageService fileStorageService)
    {
        _contabilidadService = contabilidadService;
        _context = context;
        _fileStorageService = fileStorageService;
    }

    // ==================== Vista Principal ====================

    [HttpGet]
    public async Task<IActionResult> Index(int? anio)
    {
        var anioActual = anio ?? DateTime.Now.Year;
        ViewBag.Anio = anioActual;
        
        // Generar lista de años disponibles
        var anios = Enumerable.Range(DateTime.Now.Year - 5, 10).Reverse();
        ViewBag.Anios = new SelectList(anios);

        var reportes = await _contabilidadService.ListarReportesAsync(anioActual);
        return View(reportes);
    }

    // ==================== Abrir/Crear Reporte Mensual ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AbrirMes(int mes, int anio)
    {
        try
        {
            var reporte = await _contabilidadService.ObtenerOCrearReporteMensualAsync(mes, anio);
            TempData["Success"] = $"Reporte de {reporte.NombreMes} {anio} abierto correctamente.";
            return RedirectToAction(nameof(RegistroMensual), new { id = reporte.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al abrir el mes: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // ==================== Registro Mensual (Excel-like) ====================

    [HttpGet]
    public async Task<IActionResult> RegistroMensual(long id)
    {
        var reporte = await _context.ReportesMensualesGenerales
            .Include(r => r.Movimientos)
            .ThenInclude(m => m.CategoriaIngreso)
            .Include(r => r.Movimientos)
            .ThenInclude(m => m.CategoriaEgreso)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reporte == null) return NotFound();

        ViewBag.SaldoActual = await _contabilidadService.CalcularSaldoActualAsync(id);
        ViewBag.CategoriasIngreso = await _contabilidadService.ObtenerCategoriasIngresoAsync();
        ViewBag.CategoriasEgreso = await _contabilidadService.ObtenerCategoriasEgresoAsync();

        return View(reporte);
    }

    // ==================== Guardar Movimientos Bulk (AJAX) ====================

    [HttpPost]
    public async Task<IActionResult> GuardarBulk([FromBody] BulkSaveRequest request)
    {
        if (request == null || request.ReporteId <= 0) 
            return BadRequest("Solicitud inválida.");

        var movimientos = request.Movimientos.Select(m => new MovimientoGeneral
        {
            Id = m.Id,
            Tipo = m.Tipo,
            CategoriaIngresoId = m.CategoriaIngresoId,
            CategoriaEgresoId = m.CategoriaEgresoId,
            Monto = m.Monto,
            Fecha = DateTime.SpecifyKind(m.Fecha, DateTimeKind.Utc),
            Descripcion = m.Descripcion ?? "",
            NumeroComprobante = m.NumeroComprobante
        }).ToList();

        var success = await _contabilidadService.GuardarMovimientosBulkAsync(request.ReporteId, movimientos);
        
        if (success)
        {
            var nuevoSaldo = await _contabilidadService.CalcularSaldoActualAsync(request.ReporteId);
            return Json(new { success = true, saldo = nuevoSaldo });
        }

        return Json(new { success = false, message = "Error al guardar los movimientos. Verifique que el mes no esté cerrado." });
    }

    // ==================== Sincronización Offline ====================

    [HttpPost]
    public async Task<IActionResult> SincronizarOffline([FromBody] List<BulkSaveRequest> transacciones)
    {
        if (transacciones == null || !transacciones.Any())
            return BadRequest("No hay transacciones para sincronizar.");

        var resultados = new List<object>();
        
        foreach (var request in transacciones)
        {
            try
            {
                if (request.ReporteId <= 0)
                {
                    resultados.Add(new { 
                        success = false, 
                        reporteId = request.ReporteId,
                        message = "ID de reporte inválido." 
                    });
                    continue;
                }

                var movimientos = request.Movimientos.Select(m => new MovimientoGeneral
                {
                    Id = m.Id,
                    Tipo = m.Tipo,
                    CategoriaIngresoId = m.CategoriaIngresoId,
                    CategoriaEgresoId = m.CategoriaEgresoId,
                    Monto = m.Monto,
                    Fecha = DateTime.SpecifyKind(m.Fecha, DateTimeKind.Utc),
                    Descripcion = m.Descripcion ?? "",
                    NumeroComprobante = m.NumeroComprobante
                }).ToList();

                var success = await _contabilidadService.GuardarMovimientosBulkAsync(request.ReporteId, movimientos);
                
                if (success)
                {
                    resultados.Add(new { 
                        success = true, 
                        reporteId = request.ReporteId,
                        message = "Sincronizado exitosamente" 
                    });
                }
                else
                {
                    resultados.Add(new { 
                        success = false, 
                        reporteId = request.ReporteId,
                        message = "Error al guardar. El mes puede estar cerrado." 
                    });
                }
            }
            catch (Exception ex)
            {
                resultados.Add(new { 
                    success = false, 
                    reporteId = request.ReporteId,
                    message = $"Error: {ex.Message}" 
                });
            }
        }

        var exitosos = resultados.Count(r => (bool)((dynamic)r).success);
        var fallidos = resultados.Count - exitosos;

        return Json(new { 
            success = exitosos > 0,
            total = transacciones.Count,
            exitosos = exitosos,
            fallidos = fallidos,
            resultados = resultados
        });
    }


    // ==================== Cerrar Mes ====================

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

    // ==================== Consolidado Mensual ====================

    [HttpGet]
    public async Task<IActionResult> Consolidado(long id)
    {
        var reporte = await _context.ReportesMensualesGenerales
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reporte == null) return NotFound();

        ViewBag.ConsolidadoIngresos = await _contabilidadService.ObtenerConsolidadoIngresosAsync(id);
        ViewBag.ConsolidadoEgresos = await _contabilidadService.ObtenerConsolidadoEgresosAsync(id);
        ViewBag.SaldoActual = await _contabilidadService.CalcularSaldoActualAsync(id);

        return View(reporte);
    }

    // ==================== Gestión de Categorías ====================

    [HttpGet]
    public async Task<IActionResult> GestionCategorias()
    {
        var categoriasIngreso = await _context.CategoriasIngreso
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        
        var categoriasEgreso = await _context.CategoriasEgreso
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        ViewBag.CategoriasIngreso = categoriasIngreso;
        ViewBag.CategoriasEgreso = categoriasEgreso;

        return View();
    }

    // ==================== CRUD Categorías Ingreso ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCategoriaIngreso(CategoriaIngreso categoria)
    {
        if (ModelState.IsValid)
        {
            await _contabilidadService.CrearCategoriaIngresoAsync(categoria);
            TempData["Success"] = "Categoría de ingreso creada exitosamente.";
        }
        else
        {
            TempData["Error"] = "Error al crear la categoría.";
        }
        return RedirectToAction(nameof(GestionCategorias));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCategoriaIngreso(CategoriaIngreso categoria)
    {
        if (ModelState.IsValid)
        {
            var success = await _contabilidadService.ActualizarCategoriaIngresoAsync(categoria);
            TempData[success ? "Success" : "Error"] = success 
                ? "Categoría actualizada exitosamente." 
                : "Error al actualizar la categoría.";
        }
        return RedirectToAction(nameof(GestionCategorias));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCategoriaIngreso(long id)
    {
        var success = await _contabilidadService.EliminarCategoriaIngresoAsync(id);
        TempData[success ? "Success" : "Error"] = success 
            ? "Categoría eliminada exitosamente." 
            : "Error al eliminar la categoría.";
        return RedirectToAction(nameof(GestionCategorias));
    }

    // ==================== CRUD Categorías Egreso ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCategoriaEgreso(CategoriaEgreso categoria)
    {
        if (ModelState.IsValid)
        {
            await _contabilidadService.CrearCategoriaEgresoAsync(categoria);
            TempData["Success"] = "Categoría de egreso creada exitosamente.";
        }
        else
        {
            TempData["Error"] = "Error al crear la categoría.";
        }
        return RedirectToAction(nameof(GestionCategorias));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCategoriaEgreso(CategoriaEgreso categoria)
    {
        if (ModelState.IsValid)
        {
            var success = await _contabilidadService.ActualizarCategoriaEgresoAsync(categoria);
            TempData[success ? "Success" : "Error"] = success 
                ? "Categoría actualizada exitosamente." 
                : "Error al actualizar la categoría.";
        }
        return RedirectToAction(nameof(GestionCategorias));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCategoriaEgreso(long id)
    {
        var success = await _contabilidadService.EliminarCategoriaEgresoAsync(id);
        TempData[success ? "Success" : "Error"] = success 
            ? "Categoría eliminada exitosamente." 
            : "Error al eliminar la categoría.";
        return RedirectToAction(nameof(GestionCategorias));
    }

    // ==================== Helper Classes for AJAX ====================

    public class BulkSaveRequest
    {
        public long ReporteId { get; set; }
        public List<MovimientoInput> Movimientos { get; set; } = new();
    }

    public class MovimientoInput
    {
        public long Id { get; set; }
        public int Tipo { get; set; }
        public long? CategoriaIngresoId { get; set; }
        public long? CategoriaEgresoId { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string? Descripcion { get; set; }
        public string? NumeroComprobante { get; set; }
    }

    // ==================== Adjuntos ====================

    [HttpGet]
    public async Task<IActionResult> ObtenerAdjuntos(long movimientoId)
    {
        var adjuntos = await _contabilidadService.ObtenerAdjuntosMovimientoAsync(movimientoId);
        return Json(adjuntos.Select(a => new {
            id = a.Id,
            nombre = a.NombreArchivo,
            url = _fileStorageService.GetFileUrl(a.RutaArchivo),
            tipo = a.TipoContenido,
            fecha = a.FechaSubida.ToLocalTime().ToString("g")
        }));
    }

    [HttpPost]
    public async Task<IActionResult> SubirAdjunto(long movimientoId, List<IFormFile> archivos)
    {
        if (movimientoId <= 0 || archivos == null || !archivos.Any())
            return BadRequest("Datos inválidos.");

        int count = 0;
        foreach (var archivo in archivos)
        {
            if (archivo.Length > 0)
            {
                // El usuario solicitó guardar en uploads/miembros
                var ruta = await _fileStorageService.SaveFileAsync(archivo, "miembros");
                if (!string.IsNullOrEmpty(ruta))
                {
                    await _contabilidadService.CrearAdjuntoAsync(movimientoId, archivo.FileName, ruta, archivo.ContentType);
                    count++;
                }
            }
        }

        return Json(new { success = true, count = count, message = $"{count} archivos subidos correctamente." });
    }

    [HttpPost]
    public async Task<IActionResult> EliminarAdjunto(long id)
    {
        // Primero obtener para borrar el archivo físico si es necesario (opcional, aquí solo borramos registro BD)
        // O idealmente el servicio se encarga. Por ahora solo borramos BD.
        var success = await _contabilidadService.EliminarAdjuntoAsync(id);
        return Json(new { success = success });
    }
}
