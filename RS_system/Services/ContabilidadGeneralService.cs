using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class ContabilidadGeneralService : IContabilidadGeneralService
{
    private readonly ApplicationDbContext _context;

    public ContabilidadGeneralService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==================== Categorías de Ingreso ====================

    public async Task<List<CategoriaIngreso>> ObtenerCategoriasIngresoAsync()
    {
        return await _context.CategoriasIngreso
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<CategoriaIngreso?> ObtenerCategoriaIngresoPorIdAsync(long id)
    {
        return await _context.CategoriasIngreso.FindAsync(id);
    }

    public async Task<CategoriaIngreso> CrearCategoriaIngresoAsync(CategoriaIngreso categoria)
    {
        categoria.FechaCreacion = DateTime.UtcNow;
        _context.CategoriasIngreso.Add(categoria);
        await _context.SaveChangesAsync();
        return categoria;
    }

    public async Task<bool> ActualizarCategoriaIngresoAsync(CategoriaIngreso categoria)
    {
        try
        {
            _context.CategoriasIngreso.Update(categoria);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> EliminarCategoriaIngresoAsync(long id)
    {
        try
        {
            var categoria = await _context.CategoriasIngreso.FindAsync(id);
            if (categoria == null) return false;

            // Soft delete - marcar como inactiva en lugar de eliminar
            categoria.Activa = false;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ==================== Categorías de Egreso ====================

    public async Task<List<CategoriaEgreso>> ObtenerCategoriasEgresoAsync()
    {
        return await _context.CategoriasEgreso
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<CategoriaEgreso?> ObtenerCategoriaEgresoPorIdAsync(long id)
    {
        return await _context.CategoriasEgreso.FindAsync(id);
    }

    public async Task<CategoriaEgreso> CrearCategoriaEgresoAsync(CategoriaEgreso categoria)
    {
        categoria.FechaCreacion = DateTime.UtcNow;
        _context.CategoriasEgreso.Add(categoria);
        await _context.SaveChangesAsync();
        return categoria;
    }

    public async Task<bool> ActualizarCategoriaEgresoAsync(CategoriaEgreso categoria)
    {
        try
        {
            _context.CategoriasEgreso.Update(categoria);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> EliminarCategoriaEgresoAsync(long id)
    {
        try
        {
            var categoria = await _context.CategoriasEgreso.FindAsync(id);
            if (categoria == null) return false;

            // Soft delete - marcar como inactiva en lugar de eliminar
            categoria.Activa = false;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ==================== Reportes Mensuales ====================

    public async Task<ReporteMensualGeneral?> ObtenerReporteMensualAsync(int mes, int anio)
    {
        return await _context.ReportesMensualesGenerales
            .Include(r => r.Movimientos)
            .ThenInclude(m => m.CategoriaIngreso)
            .Include(r => r.Movimientos)
            .ThenInclude(m => m.CategoriaEgreso)
            .FirstOrDefaultAsync(r => r.Mes == mes && r.Anio == anio);
    }

    public async Task<ReporteMensualGeneral> ObtenerOCrearReporteMensualAsync(int mes, int anio)
    {
        var reporteExistente = await ObtenerReporteMensualAsync(mes, anio);
        if (reporteExistente != null)
            return reporteExistente;

        // Obtener el saldo final del mes anterior
        var mesAnterior = mes == 1 ? 12 : mes - 1;
        var anioAnterior = mes == 1 ? anio - 1 : anio;

        var reporteAnterior = await ObtenerReporteMensualAsync(mesAnterior, anioAnterior);
        var saldoInicial = reporteAnterior != null 
            ? await CalcularSaldoActualAsync(reporteAnterior.Id) 
            : 0;

        var nuevoReporte = new ReporteMensualGeneral
        {
            Mes = mes,
            Anio = anio,
            SaldoInicial = saldoInicial,
            FechaCreacion = DateTime.UtcNow,
            Cerrado = false
        };

        _context.ReportesMensualesGenerales.Add(nuevoReporte);
        await _context.SaveChangesAsync();

        return nuevoReporte;
    }

    public async Task<List<ReporteMensualGeneral>> ListarReportesAsync(int? anio = null)
    {
        var query = _context.ReportesMensualesGenerales.AsQueryable();

        if (anio.HasValue)
            query = query.Where(r => r.Anio == anio.Value);

        return await query
            .OrderByDescending(r => r.Anio)
            .ThenByDescending(r => r.Mes)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> CerrarReporteAsync(long reporteId)
    {
        try
        {
            var reporte = await _context.ReportesMensualesGenerales.FindAsync(reporteId);
            if (reporte == null) return false;

            _context.Entry(reporte).Property(x => x.Cerrado).CurrentValue = true;
            _context.Entry(reporte).Property(x => x.Cerrado).IsModified = true;

            await _context.SaveChangesAsync();
            return true;
        }
        catch(Exception ex)
        {
            return false;
        }
    }

    // ==================== Movimientos ====================

    public async Task<bool> GuardarMovimientosBulkAsync(long reporteId, List<MovimientoGeneral> movimientos)
    {
        try
        {
            var reporte = await _context.ReportesMensualesGenerales.FindAsync(reporteId);
            if (reporte == null || reporte.Cerrado)
                return false;

            foreach (var movimiento in movimientos)
            {
                movimiento.ReporteMensualGeneralId = reporteId;
                movimiento.Fecha = DateTime.SpecifyKind(movimiento.Fecha, DateTimeKind.Utc);

                if (movimiento.Id > 0)
                {
                    // Update existing
                    var existente = await _context.MovimientosGenerales.FindAsync(movimiento.Id);
                    if (existente != null)
                    {
                        existente.Tipo = movimiento.Tipo;
                        existente.CategoriaIngresoId = movimiento.CategoriaIngresoId;
                        existente.CategoriaEgresoId = movimiento.CategoriaEgresoId;
                        existente.Monto = movimiento.Monto;
                        existente.Fecha = movimiento.Fecha;
                        existente.Descripcion = movimiento.Descripcion;
                        existente.NumeroComprobante = movimiento.NumeroComprobante;
                    }
                }
                else
                {
                    // Insert new
                    _context.MovimientosGenerales.Add(movimiento);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch(Exception ex)
        {
            return false;
        }
    }

    public async Task<decimal> CalcularSaldoActualAsync(long reporteId)
    {
        var reporte = await _context.ReportesMensualesGenerales
            .Include(r => r.Movimientos)
            .FirstOrDefaultAsync(r => r.Id == reporteId);

        if (reporte == null) return 0;

        var totalIngresos = reporte.Movimientos
            .Where(m => m.Tipo == (int) TipoMovimientoGeneral.Ingreso)
            .Sum(m => m.Monto);

        var totalEgresos = reporte.Movimientos
            .Where(m => m.Tipo == (int)TipoMovimientoGeneral.Egreso)
            .Sum(m => m.Monto);

        return reporte.SaldoInicial + totalIngresos - totalEgresos;
    }

    // ==================== Consolidados ====================

    public async Task<Dictionary<string, decimal>> ObtenerConsolidadoIngresosAsync(long reporteId)
    {
        var movimientos = await _context.MovimientosGenerales
            .Include(m => m.CategoriaIngreso)
            .Where(m => m.ReporteMensualGeneralId == reporteId 
                     && m.Tipo == (int)TipoMovimientoGeneral.Ingreso)
            .AsNoTracking()
            .ToListAsync();

        return movimientos
            .GroupBy(m => m.CategoriaIngreso?.Nombre ?? "Sin Categoría")
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Monto));
    }

    public async Task<Dictionary<string, decimal>> ObtenerConsolidadoEgresosAsync(long reporteId)
    {
        var movimientos = await _context.MovimientosGenerales
            .Include(m => m.CategoriaEgreso)
            .Where(m => m.ReporteMensualGeneralId == reporteId 
                     && m.Tipo == (int)TipoMovimientoGeneral.Egreso)
            .AsNoTracking()
            .ToListAsync();

        return movimientos
            .GroupBy(m => m.CategoriaEgreso?.Nombre ?? "Sin Categoría")
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Monto));
    }

    // ==================== Adjuntos ====================

    public async Task<List<MovimientoGeneralAdjunto>> ObtenerAdjuntosMovimientoAsync(long movimientoId)
    {
        return await _context.MovimientosGeneralesAdjuntos
            .Where(a => a.MovimientoGeneralId == movimientoId)
            .OrderByDescending(a => a.FechaSubida)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<MovimientoGeneralAdjunto?> CrearAdjuntoAsync(long movimientoId, string nombreArchivo, string rutaArchivo, string tipoContenido)
    {
        var movimiento = await _context.MovimientosGenerales.FindAsync(movimientoId);
        if (movimiento == null) return null;

        var adjunto = new MovimientoGeneralAdjunto
        {
            MovimientoGeneralId = movimientoId,
            NombreArchivo = nombreArchivo,
            RutaArchivo = rutaArchivo,
            TipoContenido = tipoContenido,
            FechaSubida = DateTime.UtcNow
        };

        _context.MovimientosGeneralesAdjuntos.Add(adjunto);
        await _context.SaveChangesAsync();
        return adjunto;
    }

    public async Task<bool> EliminarAdjuntoAsync(long adjuntoId)
    {
        var adjunto = await _context.MovimientosGeneralesAdjuntos.FindAsync(adjuntoId);
        if (adjunto == null) return false;

        _context.MovimientosGeneralesAdjuntos.Remove(adjunto);
        await _context.SaveChangesAsync();
        return true;
    }
}
