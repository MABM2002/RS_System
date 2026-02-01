using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class EstadoArticuloService : IEstadoArticuloService
{
    private readonly ApplicationDbContext _context;

    public EstadoArticuloService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EstadoArticulo>> GetAllAsync()
    {
        return await _context.EstadosArticulos
            .Where(e => !e.Eliminado)
            .OrderBy(e => e.Nombre)
            .ToListAsync();
    }

    public async Task<EstadoArticulo?> GetByIdAsync(int id)
    {
        return await _context.EstadosArticulos
            .FirstOrDefaultAsync(e => e.Id == id && !e.Eliminado);
    }

    public async Task<bool> CreateAsync(EstadoArticulo estado)
    {
        try
        {
            estado.CreadoEn = DateTime.UtcNow;
            estado.ActualizadoEn = DateTime.UtcNow;
            estado.Eliminado = false;
            
            _context.EstadosArticulos.Add(estado);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(EstadoArticulo estado)
    {
        try
        {
            var existing = await _context.EstadosArticulos.FindAsync(estado.Id);
            if (existing == null || existing.Eliminado) return false;

            existing.Nombre = estado.Nombre;
            existing.Descripcion = estado.Descripcion;
            existing.Color = estado.Color;
            existing.Activo = estado.Activo;
            existing.ActualizadoEn = DateTime.UtcNow;

            _context.EstadosArticulos.Update(existing);
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
            var estado = await _context.EstadosArticulos.FindAsync(id);
            if (estado == null || estado.Eliminado) return false;

            estado.Eliminado = true;
            estado.ActualizadoEn = DateTime.UtcNow;
            
            _context.EstadosArticulos.Update(estado);
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
        var query = _context.EstadosArticulos.AsQueryable();
        
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync(e => e.Nombre.ToLower() == nombre.ToLower() && !e.Eliminado);
    }
}
