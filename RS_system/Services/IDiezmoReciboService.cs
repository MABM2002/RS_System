using Rs_system.Models;

namespace Rs_system.Services;

public interface IDiezmoReciboService
{
    /// <summary>
    /// Genera (o recupera) el correlativo de recibo para una salida.
    /// Formato: RECDZ-{AAAA}-{id:D6}
    /// Persiste el numero_recibo en la tabla diezmo_salidas.
    /// </summary>
    Task<string> GenerarNumeroReciboAsync(long salidaId);

    /// <summary>Obtiene todos los datos necesarios para renderizar el recibo.</summary>
    Task<DiezmoSalida?> GetSalidaParaReciboAsync(long salidaId);
}
