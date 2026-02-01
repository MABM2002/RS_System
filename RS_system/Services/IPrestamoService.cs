using Rs_system.Models;

namespace Rs_system.Services;

public interface IPrestamoService
{
    Task<IEnumerable<Prestamo>> GetHistorialPrestamosAsync(int limit = 100);
    Task<IEnumerable<Prestamo>> GetPrestamosActivosAsync();
    Task<Prestamo?> GetPrestamoByIdAsync(long id);
    Task<bool> RegistrarPrestamoAsync(int articuloId, int cantidad, string personaNombre, string? personaIdentificacion, DateTime? fechaDevolucionEstimada, string observacion, string usuario);
    Task<bool> RegistrarDevolucionAsync(long prestamoId, string observacion, string usuario);
    Task<bool> RegistrarDevolucionParcialAsync(long prestamoId, List<string> codigosDevolucion, string observacion, string usuario);
}