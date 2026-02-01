using Rs_system.Models;

namespace Rs_system.Services;

public interface IContabilidadGeneralService
{
    // Categorías de Ingreso
    Task<List<CategoriaIngreso>> ObtenerCategoriasIngresoAsync();
    Task<CategoriaIngreso?> ObtenerCategoriaIngresoPorIdAsync(long id);
    Task<CategoriaIngreso> CrearCategoriaIngresoAsync(CategoriaIngreso categoria);
    Task<bool> ActualizarCategoriaIngresoAsync(CategoriaIngreso categoria);
    Task<bool> EliminarCategoriaIngresoAsync(long id);

    // Categorías de Egreso
    Task<List<CategoriaEgreso>> ObtenerCategoriasEgresoAsync();
    Task<CategoriaEgreso?> ObtenerCategoriaEgresoPorIdAsync(long id);
    Task<CategoriaEgreso> CrearCategoriaEgresoAsync(CategoriaEgreso categoria);
    Task<bool> ActualizarCategoriaEgresoAsync(CategoriaEgreso categoria);
    Task<bool> EliminarCategoriaEgresoAsync(long id);

    // Reportes Mensuales
    Task<ReporteMensualGeneral?> ObtenerReporteMensualAsync(int mes, int anio);
    Task<ReporteMensualGeneral> ObtenerOCrearReporteMensualAsync(int mes, int anio);
    Task<List<ReporteMensualGeneral>> ListarReportesAsync(int? anio = null);
    Task<bool> CerrarReporteAsync(long reporteId);

    // Movimientos
    Task<bool> GuardarMovimientosBulkAsync(long reporteId, List<MovimientoGeneral> movimientos);
    Task<decimal> CalcularSaldoActualAsync(long reporteId);
    
    // Consolidados
    Task<Dictionary<string, decimal>> ObtenerConsolidadoIngresosAsync(long reporteId);
    Task<Dictionary<string, decimal>> ObtenerConsolidadoEgresosAsync(long reporteId);

    // Adjuntos
    Task<List<MovimientoGeneralAdjunto>> ObtenerAdjuntosMovimientoAsync(long movimientoId);
    Task<MovimientoGeneralAdjunto?> CrearAdjuntoAsync(long movimientoId, string nombreArchivo, string rutaArchivo, string tipoContenido);
    Task<bool> EliminarAdjuntoAsync(long adjuntoId);
}
