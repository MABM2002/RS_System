using Rs_system.Models;

namespace Rs_system.Services;

public class DiezmoCalculoService : IDiezmoCalculoService
{
    /// <inheritdoc/>
    public decimal CalcularMontoNeto(decimal montoEntregado, decimal cambioEntregado)
        => montoEntregado - cambioEntregado;

    /// <inheritdoc/>
    public DiezmoCierre RecalcularTotales(DiezmoCierre cierre)
    {
        var detallesActivos = cierre.Detalles.Where(d => !d.Eliminado).ToList();
        var salidasActivas  = cierre.Salidas.Where(s => !s.Eliminado).ToList();

        cierre.TotalRecibido = detallesActivos.Sum(d => d.MontoEntregado);
        cierre.TotalCambio   = detallesActivos.Sum(d => d.CambioEntregado);
        cierre.TotalNeto     = detallesActivos.Sum(d => d.MontoNeto);
        cierre.TotalSalidas  = salidasActivas.Sum(s => s.Monto);
        cierre.SaldoFinal    = cierre.TotalNeto - cierre.TotalSalidas;

        return cierre;
    }
}
