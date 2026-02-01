using Rs_system.Models;

namespace Rs_system.Services;

public interface IEstadoArticuloService
{
    Task<IEnumerable<EstadoArticulo>> GetAllAsync();
    Task<EstadoArticulo?> GetByIdAsync(int id);
    Task<bool> CreateAsync(EstadoArticulo estado);
    Task<bool> UpdateAsync(EstadoArticulo estado);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(string nombre, int? excludeId = null);
}
