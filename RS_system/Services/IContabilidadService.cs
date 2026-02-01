using Rs_system.Models;

namespace Rs_system.Services;

public interface IContabilidadService
{
    Task<ContabilidadRegistro> CrearRegistroAsync(ContabilidadRegistro registro);
    Task<IReadOnlyList<ContabilidadRegistro>> ObtenerRegistrosAsync(long grupoId, DateTime desde, DateTime hasta);

    // Monthly Report Methods
    Task<ReporteMensualContable?> ObtenerReporteMensualAsync(long grupoId, int mes, int anio);
    Task<ReporteMensualContable> ObtenerOCrearReporteMensualAsync(long grupoId, int mes, int anio);
    Task<List<ReporteMensualContable>> ListarReportesPorGrupoAsync(long grupoId);
    Task<bool> GuardarRegistrosBulkAsync(long reporteId, List<ContabilidadRegistro> registros);
    Task<decimal> CalcularSaldoActualAsync(long reporteId);
    Task<bool> CerrarReporteAsync(long reporteId);

}
