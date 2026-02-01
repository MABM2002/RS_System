using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ApplicationDbContext _context;

    public CategoriaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Categoria>> GetAllAsync()
    {
        return await _context.Categorias
            .Where(c => !c.Eliminado)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Categoria?> GetByIdAsync(int id)
    {
        return await _context.Categorias
            .FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);
    }

    public async Task<bool> CreateAsync(Categoria categoria)
    {
        try
        {
            categoria.CreadoEn = DateTime.UtcNow;
            categoria.ActualizadoEn = DateTime.UtcNow;
            // Eliminado and Activo defaults are set in the model/DB, ensuring here just in case
            categoria.Eliminado = false; 
            
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Categoria categoria)
    {
        try
        {
            var existing = await _context.Categorias.FindAsync(categoria.Id);
            if (existing == null || existing.Eliminado) return false;

            existing.Nombre = categoria.Nombre;
            existing.Descripcion = categoria.Descripcion;
            existing.Activo = categoria.Activo;
            existing.ActualizadoEn = DateTime.UtcNow;
            // CreadoPor and CreadoEn should not change

            _context.Categorias.Update(existing);
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
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null || categoria.Eliminado) return false;

            categoria.Eliminado = true;
            categoria.ActualizadoEn = DateTime.UtcNow;
            
            _context.Categorias.Update(categoria);
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
        var query = _context.Categorias.AsQueryable();
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower() && !c.Eliminado);
    }
}
