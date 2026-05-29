using Rs_system.Models;

namespace Rs_system.Services;

/// <summary>
/// Interfaz para la integración automática entre módulos operativos y contabilidad.
/// </summary>
public interface IAccountingIntegrationService
{
    /// <summary>
    /// Procesa el cierre de una cabecera de diario financiero, generando las partidas contables correspondientes.
    /// Agrupa ingresos y egresos para generar asientos balanceados.
    /// </summary>
    /// <param name="cabeceraId">ID de la cabecera del diario a procesar.</param>
    /// <param name="usuario">Usuario que realiza la acción.</param>
    /// <returns>Lista de partidas contables generadas.</returns>
    Task<List<PartidaContable>> ProcesarCierreDiarioAsync(long cabeceraId, string usuario);
}
