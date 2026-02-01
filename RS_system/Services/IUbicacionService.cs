using Rs_system.Models;

namespace Rs_system.Services;

public interface IUbicacionService
{
    Task<IEnumerable<Ubicacion>> GetAllAsync();
    Task<Ubicacion?> GetByIdAsync(int id);
    Task<bool> CreateAsync(Ubicacion ubicacion);
    Task<bool> UpdateAsync(Ubicacion ubicacion);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(string nombre, int? excludeId = null);
}
