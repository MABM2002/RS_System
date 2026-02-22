using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public interface IDiezmoCalculoService
{
    /// <summary>Calcula el monto neto de un detalle: MontoEntregado - CambioEntregado.</summary>
    decimal CalcularMontoNeto(decimal montoEntregado, decimal cambioEntregado);

    /// <summary>
    /// Recalcula todos los totales del cierre a partir de sus detalles y salidas activos.
    /// Retorna el cierre con los valores actualizados (sin guardar en BD).
    /// </summary>
    DiezmoCierre RecalcularTotales(DiezmoCierre cierre);
}
