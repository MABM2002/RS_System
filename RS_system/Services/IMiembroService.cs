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

    /// <summary>
    /// Imports members from a CSV stream
    /// </summary>
    /// <param name="csvStream">The stream of the CSV file</param>
    /// <param name="createdBy">The user creating the members</param>
    /// <returns>A tuple with success count and a list of error messages</returns>
    Task<(int SuccessCount, List<string> Errors)> ImportarMiembrosAsync(Stream csvStream, string createdBy);

    /// <summary>
    /// Gets paginated members with optional search
    /// </summary>
    /// <param name="page">Current page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Optional search query to filter by name</param>
    /// <returns>Paginated result with members</returns>
    Task<PaginatedViewModel<MiembroViewModel>> GetPaginatedAsync(int page, int pageSize, string? searchQuery = null);
}
