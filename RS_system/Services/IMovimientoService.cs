using Rs_system.Models;

namespace Rs_system.Services;

public interface IMovimientoService
{
    Task<IEnumerable<MovimientoInventario>> GetHistorialGeneralAsync(int limit = 100);
    Task<IEnumerable<MovimientoInventario>> GetHistorialPorArticuloAsync(int articuloId);
    
    // Legacy wrappers (Quantity = 1)
    Task<bool> RegistrarTrasladoAsync(int articuloId, int nuevaUbicacionId, string observacion, string usuario);
    Task<bool> RegistrarBajaAsync(int articuloId, string motivo, string usuario);

    // New Quantity-Aware Methods
    Task<bool> RegistrarTrasladoCantidadAsync(int articuloId, int nuevaUbicacionId, int cantidad, string observacion, string usuario);
    Task<bool> RegistrarBajaCantidadAsync(int articuloId, int cantidad, string motivo, string usuario);
    
    Task<bool> RegistrarCambioEstadoAsync(int articuloId, int nuevoEstadoId, string observacion, string usuario);
    Task<bool> RegistrarPrestamoAsync(int articuloId, int cantidad, string personaNombre, string? personaIdentificacion, DateTime? fechaDevolucionEstimada, string observacion, string usuario);
    Task<bool> RegistrarEntradaCantidadAsync(int articuloId, int cantidad, string observacion, string usuario);
}
