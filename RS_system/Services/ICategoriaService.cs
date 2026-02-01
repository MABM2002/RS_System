using Rs_system.Models;

namespace Rs_system.Services;

public interface ICategoriaService
{
    Task<IEnumerable<Categoria>> GetAllAsync();
    Task<Categoria?> GetByIdAsync(int id);
    Task<bool> CreateAsync(Categoria categoria);
    Task<bool> UpdateAsync(Categoria categoria);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(string nombre, int? excludeId = null);
}
