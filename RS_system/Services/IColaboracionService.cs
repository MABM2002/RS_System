using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public interface IColaboracionService
{
    // Tipos de colaboración
    Task<List<TipoColaboracion>> GetTiposActivosAsync();
    Task<TipoColaboracion?> GetTipoByIdAsync(long id);

    // Colaboraciones
    Task<Colaboracion> RegistrarColaboracionAsync(RegistrarColaboracionViewModel model, string registradoPor);
    Task<List<Colaboracion>> GetColaboracionesRecientesAsync(int cantidad = 50);
    Task<Colaboracion?> GetColaboracionByIdAsync(long id);

    // Colaboracion Heads (Master-Detail)
    Task<List<ColaboracionHeadIndexViewModel>> GetColaboracionHeadsRecientesAsync(int cantidad = 50);
    Task<ColaboracionHeadDetalleViewModel?> GetColaboracionHeadByIdAsync(long id);
    Task<ColaboracionHead?> GetOrCreateColaboracionHeadForDateAsync(DateTime fecha, string creadoPor);
    
    // Cierre Diario y Sincronización Contable
    Task<CierreDiarioResult> RealizarCierreDiarioAsync(long colaboracionHeadId, string cerradoPor);

    // Reportes
    Task<ReporteColaboracionesViewModel> GenerarReportePorFechasAsync(DateTime fechaInicio, DateTime fechaFin);
    Task<EstadoCuentaViewModel> GenerarEstadoCuentaAsync(long miembroId);
    Task<List<UltimoPagoViewModel>> GetUltimosPagosPorMiembroAsync(long miembroId);
}
