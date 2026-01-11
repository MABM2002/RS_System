using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public interface IAuthService
{
    /// <summary>
    /// Validates user credentials and returns the user if valid
    /// </summary>
    Task<Usuario?> ValidateUserAsync(string username, string password);
    
    /// <summary>
    /// Registers a new user
    /// </summary>
    Task<(bool Success, string Message, Usuario? User)> RegisterUserAsync(RegisterViewModel model);
    
    /// <summary>
    /// Gets the roles for a user
    /// </summary>
    Task<List<string>> GetUserRolesAsync(long userId);
    
    /// <summary>
    /// Updates the last login timestamp
    /// </summary>
    Task UpdateLastLoginAsync(long userId);

    /// <summary>
    /// Checks if a user has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, string permissionCode);
}
