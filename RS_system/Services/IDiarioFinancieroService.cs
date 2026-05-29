using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public interface IDiarioFinancieroService
{
    // ── Cabeceras ────────────────────────────────────
    Task<List<DiarioFinancieroIndexViewModel>> ListarCabecerasAsync(DateTime? fechaInicio, DateTime? fechaFin, string? estado);
    Task<DiarioFinancieroCabecera?> ObtenerCabeceraAsync(long id);
    Task<DiarioFinancieroCabecera> CrearCabeceraAsync(DateTime fecha, string? observaciones, string usuario);
    Task<bool> ActualizarObservacionesCabeceraAsync(long id, string? observaciones, string usuario);
    Task<bool> CerrarDiarioAsync(long id, string usuario);
    Task<bool> ReabrirDiarioAsync(long id, string usuario);
    Task<bool> EliminarCabeceraAsync(long id);

    // ── Detalles (Movimientos) ──────────────────────
    Task<DiarioFinancieroDetalle?> ObtenerDetalleAsync(long id);
    Task<DiarioFinancieroDetalle?> GuardarMovimientoAsync(DiarioMovimientoInput input, string usuario);
    Task<bool> EliminarMovimientoAsync(long id, string usuario);

    // ── Recalcular totales ──────────────────────────
    Task RecalcularTotalesAsync(long cabeceraId);

    // ── Adjuntos ────────────────────────────────────
    Task<List<DiarioFinancieroAdjunto>> ObtenerAdjuntosAsync(long detalleId);
    Task<DiarioFinancieroAdjunto?> CrearAdjuntoAsync(long detalleId, string nombreArchivo, string rutaArchivo, string? tipoContenido);
    Task<bool> EliminarAdjuntoAsync(long adjuntoId);

    // ── Catálogos ───────────────────────────────────
    Task<List<MetodoPago>> ObtenerMetodosPagoAsync();
    Task<List<CategoriaIngreso>> ObtenerCategoriasIngresoAsync();
    Task<List<CategoriaEgreso>> ObtenerCategoriasEgresoAsync();

    // ── Reporte ─────────────────────────────────────
    Task<DiarioFinancieroReporteViewModel> GenerarReporteAsync(DiarioFinancieroFiltroViewModel filtro);
}
