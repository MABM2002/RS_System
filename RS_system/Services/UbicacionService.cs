using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class UbicacionService : IUbicacionService
{
    private readonly ApplicationDbContext _context;

    public UbicacionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Ubicacion>> GetAllAsync()
    {
        return await _context.Ubicaciones
            .Where(u => !u.Eliminado)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
    }

    public async Task<Ubicacion?> GetByIdAsync(int id)
    {
        return await _context.Ubicaciones
            .FirstOrDefaultAsync(u => u.Id == id && !u.Eliminado);
    }

    public async Task<bool> CreateAsync(Ubicacion ubicacion)
    {
        try
        {
            ubicacion.CreadoEn = DateTime.UtcNow;
            ubicacion.ActualizadoEn = DateTime.UtcNow;
            ubicacion.Eliminado = false;
            
            _context.Ubicaciones.Add(ubicacion);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Ubicacion ubicacion)
    {
        try
        {
            var existing = await _context.Ubicaciones.FindAsync(ubicacion.Id);
            if (existing == null || existing.Eliminado) return false;

            existing.Nombre = ubicacion.Nombre;
            existing.Descripcion = ubicacion.Descripcion;
            existing.Responsable = ubicacion.Responsable;
            existing.Activo = ubicacion.Activo;
            existing.ActualizadoEn = DateTime.UtcNow;

            _context.Ubicaciones.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var ubicacion = await _context.Ubicaciones.FindAsync(id);
            if (ubicacion == null || ubicacion.Eliminado) return false;

            ubicacion.Eliminado = true;
            ubicacion.ActualizadoEn = DateTime.UtcNow;
            
            _context.Ubicaciones.Update(ubicacion);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string nombre, int? excludeId = null)
    {
        var query = _context.Ubicaciones.AsQueryable();
        
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }

        return await query.AnyAsync(u => u.Nombre.ToLower() == nombre.ToLower() && !u.Eliminado);
    }
}
