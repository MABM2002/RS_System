using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;
using BC = BCrypt.Net.BCrypt;

namespace Rs_system.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<Usuario?> ValidateUserAsync(string username, string password)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Include(u => u.Persona)
            .Include(u => u.RolesUsuario)
                .ThenInclude(ru => ru.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == username && u.Activo);
        
        if (usuario == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Username}", username);
            return null;
        }
        
        // Verify password using BCrypt
        try
        {
            if (!BC.Verify(password, usuario.HashContrasena))
            {
                _logger.LogWarning("Invalid password for user: {Username}", username);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password for user: {Username}", username);
            return null;
        }
        
        _logger.LogInformation("User {Username} logged in successfully", username);
        return usuario;
    }
    
    public async Task<(bool Success, string Message, Usuario? User)> RegisterUserAsync(RegisterViewModel model)
    {
        // Check if username already exists
        if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == model.NombreUsuario))
        {
            return (false, "El nombre de usuario ya existe", null);
        }
        
        // Check if email already exists
        if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
        {
            return (false, "El correo electrónico ya está registrado", null);
        }
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Create persona first
            var persona = new Persona
            {
                Nombres = model.Nombres,
                Apellidos = model.Apellidos,
                Email = model.Email,
                Activo = true,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };
            
            _context.Personas.Add(persona);
            await _context.SaveChangesAsync();
            
            // Create user with hashed password
            var usuario = new Usuario
            {
                PersonaId = persona.Id,
                NombreUsuario = model.NombreUsuario,
                Email = model.Email,
                HashContrasena = BC.HashPassword(model.Contrasena),
                Activo = true,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };
            
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            
            // Assign default role (LECTOR - reader)
            var defaultRole = await _context.RolesSistema
                .FirstOrDefaultAsync(r => r.Codigo == "LECTOR");
            
            if (defaultRole != null)
            {
                var rolUsuario = new RolUsuario
                {
                    UsuarioId = usuario.Id,
                    RolId = defaultRole.Id,
                    AsignadoEn = DateTime.UtcNow
                };
                
                _context.RolesUsuario.Add(rolUsuario);
                await _context.SaveChangesAsync();
            }
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("New user registered: {Username}", model.NombreUsuario);
            return (true, "Usuario registrado exitosamente", usuario);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error registering user: {Username}", model.NombreUsuario);
            return (false, "Error al registrar el usuario", null);
        }
    }
    
    public async Task<List<string>> GetUserRolesAsync(long userId)
    {
        return await _context.RolesUsuario
            .AsNoTracking()
            .Where(ru => ru.UsuarioId == userId)
            .Select(ru => ru.Rol.Codigo)
            .ToListAsync();
    }
    
    public async Task UpdateLastLoginAsync(long userId)
    {
        var usuario = await _context.Usuarios.FindAsync(userId);
        if (usuario != null)
        {
            usuario.UltimoLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasPermissionAsync(long userId, string permissionCode)
    {
        // ROOT has all permissions
        var roles = await GetUserRolesAsync(userId);
        if (roles.Contains("ROOT")) return true;

        return await _context.RolesUsuario
            .AsNoTracking()
            .Where(ru => ru.UsuarioId == userId)
            .Join(_context.RolesPermisos.AsNoTracking(), 
                ru => ru.RolId, 
                rp => rp.RolId, 
                (ru, rp) => rp.PermisoId)
            .Join(_context.Permisos.AsNoTracking(),
                permisoId => permisoId,
                p => p.Id,
                (permisoId, p) => p.Codigo)
            .AnyAsync(codigo => codigo == permissionCode);
    }
}
