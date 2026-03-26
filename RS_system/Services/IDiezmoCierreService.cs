using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public interface IDiezmoCierreService
{
    // ── Catálogos ──
    Task<List<DiezmoTipoSalida>>   GetTiposSalidaActivosAsync();
    Task<List<DiezmoBeneficiario>> GetBeneficiariosActivosAsync();

    // ── Cierres ──
    Task<List<DiezmoCierre>> GetCierresAsync(int? anio = null);
    Task<DiezmoCierre?>      GetCierreByIdAsync(long id);
    Task<DiezmoCierre>       CrearCierreAsync(DateOnly fecha, string? observaciones, string creadoPor);

    // ── Detalles ──
    Task AgregarDetalleAsync(long cierreId, DiezmoDetalleFormViewModel vm, string usuario);
    Task EliminarDetalleAsync(long detalleId, string usuario);

    // ── Salidas ──
    Task AgregarSalidaAsync(long cierreId, DiezmoSalidaFormViewModel vm, string usuario);
    Task EliminarSalidaAsync(long salidaId, string usuario);

    // ── Flujo de cierre ──
    Task CerrarCierreAsync(long cierreId, string usuario);
    Task RecalcularCierreAsync(long cierreId, string usuario);
    Task ReabrirCierreAsync(long cierreId, string usuario);

    // ── Totales ──
    Task RecalcularTotalesAsync(long cierreId);
}
