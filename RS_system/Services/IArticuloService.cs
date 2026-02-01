using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public interface IArticuloService
{
    Task<IEnumerable<ArticuloViewModel>> GetAllAsync(string? search = null, int? categoriaId = null, int? ubicacionId = null, int? estadoId = null);
    Task<ArticuloViewModel?> GetByIdAsync(int id);
    Task<bool> CreateAsync(ArticuloViewModel viewModel, string createdBy);
    Task<bool> UpdateAsync(ArticuloViewModel viewModel);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null);
    
    // Dropdown helpers
    Task<IEnumerable<(int Id, string Nombre)>> GetCategoriasAsync();
    Task<IEnumerable<(int Id, string Nombre, string Color)>> GetEstadosAsync();
    Task<IEnumerable<(int Id, string Nombre)>> GetUbicacionesAsync();
}
