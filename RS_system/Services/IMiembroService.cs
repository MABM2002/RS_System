using Microsoft.AspNetCore.Http;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public interface IMiembroService
{
    /// <summary>
    /// Gets all active members with their work group information
    /// </summary>
    Task<IEnumerable<MiembroViewModel>> GetAllAsync();
    
    /// <summary>
    /// Gets a member by ID
    /// </summary>
    Task<MiembroViewModel?> GetByIdAsync(long id);
    
    /// <summary>
    /// Creates a new member
    /// </summary>
    Task<bool> CreateAsync(MiembroViewModel viewModel, string createdBy, IFormFile? fotoFile = null);
    
    /// <summary>
    /// Updates an existing member
    /// </summary>
    Task<bool> UpdateAsync(long id, MiembroViewModel viewModel, IFormFile? fotoFile = null);
    
    /// <summary>
    /// Soft deletes a member
    /// </summary>
    Task<bool> DeleteAsync(long id);
    
    /// <summary>
    /// Gets all active work groups for dropdown
    /// </summary>
    Task<IEnumerable<(long Id, string Nombre)>> GetGruposTrabajoAsync();
}
