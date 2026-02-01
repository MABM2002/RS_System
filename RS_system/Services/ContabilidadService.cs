using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class ContabilidadService : IContabilidadService
{
    private readonly ApplicationDbContext _context;

    public ContabilidadService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContabilidadRegistro> CrearRegistroAsync(ContabilidadRegistro registro)
    {
        // Ensure Group exists
        var groupExists = await _context.GruposTrabajo.AnyAsync(g => g.Id == registro.GrupoTrabajoId);
        if (!groupExists)
        {
            throw new ArgumentException($"Grupo de trabajo con ID {registro.GrupoTrabajoId} no existe.");
        }

        _context.ContabilidadRegistros.Add(registro);
        await _context.SaveChangesAsync();
        return registro;
    }

    public async Task<IReadOnlyList<ContabilidadRegistro>> ObtenerRegistrosAsync(long grupoId, DateTime desde, DateTime hasta)
    {
        return await _context.ContabilidadRegistros
            .Include(c => c.GrupoTrabajo)
            .Where(c => c.GrupoTrabajoId == grupoId && c.Fecha.Date >= desde.Date && c.Fecha.Date <= hasta.Date)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();
    }

    public async Task<ReporteMensualContable?> ObtenerReporteMensualAsync(long grupoId, int mes, int anio)
    {
        return await _context.ReportesMensualesContables
            .Include(r => r.Registros)
            .FirstOrDefaultAsync(r => r.GrupoTrabajoId == grupoId && r.Mes == mes && r.Anio == anio);
    }

    public async Task<ReporteMensualContable> ObtenerOCrearReporteMensualAsync(long grupoId, int mes, int anio)
    {
        var reporte = await ObtenerReporteMensualAsync(grupoId, mes, anio);
        if (reporte != null) return reporte;

        // Calculate Saldo Inicial based on previous month
        decimal saldoInicial = 0;
        var prevMes = mes == 1 ? 12 : mes - 1;
        var prevAnio = mes == 1 ? anio - 1 : anio;
        
        var reportePrevio = await ObtenerReporteMensualAsync(grupoId, prevMes, prevAnio);
        if (reportePrevio != null)
        {
            saldoInicial = await CalcularSaldoActualAsync(reportePrevio.Id);
        }

        reporte = new ReporteMensualContable
        {
            GrupoTrabajoId = grupoId,
            Mes = mes,
            Anio = anio,
            SaldoInicial = saldoInicial,
            FechaCreacion = DateTime.UtcNow,
            Cerrado = false
        };

        _context.ReportesMensualesContables.Add(reporte);
        await _context.SaveChangesAsync();
        return reporte;
    }

    public async Task<List<ReporteMensualContable>> ListarReportesPorGrupoAsync(long grupoId)
    {
        return await _context.ReportesMensualesContables
            .Where(r => r.GrupoTrabajoId == grupoId)
            .OrderByDescending(r => r.Anio)
            .ThenByDescending(r => r.Mes)
            .ToListAsync();
    }

    public async Task<bool> GuardarRegistrosBulkAsync(long reporteId, List<ContabilidadRegistro> registros)
    {
        var reporte = await _context.ReportesMensualesContables
            .Include(r => r.Registros)
            .FirstOrDefaultAsync(r => r.Id == reporteId);

        if (reporte == null || reporte.Cerrado) return false;
        try
        {
            // Remove existing records for this report (or handle updates carefully)
            // For a simple bulk entry system, we might replace all or upsert by ID.
            // Let's go with UPSERT based on ID.
            
            var existingIds = reporte.Registros.Select(r => r.Id).ToList();
            var incomingIds = registros.Where(r => r.Id > 0).Select(r => r.Id).ToList();
            
            // Delete records that are no longer in the list
            var toDelete = reporte.Registros.Where(r => !incomingIds.Contains(r.Id)).ToList();
            _context.ContabilidadRegistros.RemoveRange(toDelete);

            foreach (var registro in registros)
            {
                if (registro.Id > 0)
                {
                    // Update
                    var existing = reporte.Registros.FirstOrDefault(r => r.Id == registro.Id);
                    if (existing != null)
                    {
                        existing.Tipo = registro.Tipo;
                        existing.Monto = registro.Monto;
                        existing.Fecha = registro.Fecha;
                        existing.Descripcion = registro.Descripcion;
                        _context.Entry(existing).State = EntityState.Modified;
                    }
                }
                else
                {
                    // Add
                    registro.ReporteMensualId = reporteId;
                    registro.GrupoTrabajoId = reporte.GrupoTrabajoId;
                    _context.ContabilidadRegistros.Add(registro);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<decimal> CalcularSaldoActualAsync(long reporteId)
    {
        var reporte = await _context.ReportesMensualesContables
            .Include(r => r.Registros)
            .FirstOrDefaultAsync(r => r.Id == reporteId);

        if (reporte == null) return 0;

        decimal ingresos = reporte.Registros
            .Where(r => r.Tipo == TipoMovimientoContable.Ingreso)
            .Sum(r => r.Monto);
            
        decimal egresos = reporte.Registros
            .Where(r => r.Tipo == TipoMovimientoContable.Egreso)
            .Sum(r => r.Monto);

        return reporte.SaldoInicial + ingresos - egresos;
    }

    public async Task<bool> CerrarReporteAsync(long reporteId)
    {
        var reporte = await _context.ReportesMensualesContables.FindAsync(reporteId);
        if (reporte == null || reporte.Cerrado) return false;

        reporte.Cerrado = true;
        _context.ReportesMensualesContables.Update(reporte);
        await _context.SaveChangesAsync();
        return true;
    }

}
