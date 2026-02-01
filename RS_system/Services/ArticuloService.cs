using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public class ArticuloService : IArticuloService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public ArticuloService(ApplicationDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<IEnumerable<ArticuloViewModel>> GetAllAsync(string? search = null, int? categoriaId = null, int? ubicacionId = null, int? estadoId = null)
    {
        var query = _context.Articulos
            .Include(a => a.Categoria)
            .Include(a => a.Estado)
            .Include(a => a.Ubicacion)
            .Where(a => !a.Eliminado)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(a => 
                a.Nombre.ToLower().Contains(term) || 
                a.Codigo.ToLower().Contains(term) ||
                a.Modelo.ToLower().Contains(term) ||
                a.Marca.ToLower().Contains(term) ||
                a.NumeroSerie.ToLower().Contains(term));
        }

        if (categoriaId.HasValue)
            query = query.Where(a => a.CategoriaId == categoriaId);

        if (ubicacionId.HasValue)
            query = query.Where(a => a.UbicacionId == ubicacionId);

        if (estadoId.HasValue)
            query = query.Where(a => a.EstadoId == estadoId);

        return await query
            .OrderByDescending(a => a.CreadoEn)
            .Select(a => new ArticuloViewModel
            {
                Id = a.Id,
                Codigo = a.Codigo,
                Nombre = a.Nombre,
                Descripcion = a.Descripcion,
                Marca = a.Marca,
                Modelo = a.Modelo,
                NumeroSerie = a.NumeroSerie,
                Precio = a.Precio,
                FechaAdquisicion = a.FechaAdquisicion,
                ImagenUrl = a.ImagenUrl,
                CategoriaId = a.CategoriaId,
                CategoriaNombre = a.Categoria.Nombre,
                EstadoId = a.EstadoId,
                EstadoNombre = a.Estado.Nombre,
                EstadoColor = a.Estado.Color,
                UbicacionId = a.UbicacionId,
                UbicacionNombre = a.Ubicacion.Nombre,
                Activo = a.Activo
            })
            .ToListAsync();
    }

    public async Task<ArticuloViewModel?> GetByIdAsync(int id)
    {
        var a = await _context.Articulos
            .Include(a => a.Categoria)
            .Include(a => a.Estado)
            .Include(a => a.Ubicacion)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Eliminado);

        if (a == null) return null;

        return new ArticuloViewModel
        {
            Id = a.Id,
            Codigo = a.Codigo,
            Nombre = a.Nombre,
            Descripcion = a.Descripcion,
            Marca = a.Marca,
            Modelo = a.Modelo,
            NumeroSerie = a.NumeroSerie,
            Precio = a.Precio,
            FechaAdquisicion = a.FechaAdquisicion,
            ImagenUrl = a.ImagenUrl,
            CategoriaId = a.CategoriaId,
            CategoriaNombre = a.Categoria.Nombre,
            EstadoId = a.EstadoId,
            EstadoNombre = a.Estado.Nombre,
            EstadoColor = a.Estado.Color,
            UbicacionId = a.UbicacionId,
            UbicacionNombre = a.Ubicacion.Nombre,
            Activo = a.Activo,
            CantidadGlobal = a.CantidadGlobal,
            // New Fields
            TipoControl = a.TipoControl,
            CantidadInicial = a.CantidadGlobal // Map Global Qty to CantidadInicial for Display
        };
    }

    public async Task<bool> CreateAsync(ArticuloViewModel viewModel, string createdBy)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                string? imagenUrl = null;
                if (viewModel.ImagenFile != null)
                {
                    imagenUrl = await _fileStorageService.SaveFileAsync(viewModel.ImagenFile, "articulos");
                }

                var articulo = new Articulo
                {
                    Codigo = viewModel.Codigo,
                    Nombre = viewModel.Nombre,
                    Descripcion = viewModel.Descripcion,
                    Marca = viewModel.Marca,
                    Modelo = viewModel.Modelo,
                    NumeroSerie = viewModel.NumeroSerie,
                    Precio = viewModel.Precio,
                    FechaAdquisicion = viewModel.FechaAdquisicion,
                    ImagenUrl = imagenUrl,
                    CategoriaId = viewModel.CategoriaId,
                    EstadoId = viewModel.EstadoId,
                    UbicacionId = viewModel.UbicacionId,
                    Activo = viewModel.Activo,
                    Eliminado = false,
                    CreadoPor = createdBy,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow,
                    // New Fields
                    TipoControl = viewModel.TipoControl ?? nameof(Articulo.TipoControlInventario.UNITARIO),
                    CantidadGlobal = (viewModel.TipoControl == nameof(Articulo.TipoControlInventario.LOTE)) ? viewModel.CantidadInicial : 1
                };

                _context.Articulos.Add(articulo);
                await _context.SaveChangesAsync();

                // If LOTE, initialize Existencia
                if (articulo.TipoControl == nameof(Articulo.TipoControlInventario.LOTE))
                {
                    var existencia = new Existencia
                    {
                        ArticuloId = articulo.Id,
                        UbicacionId = articulo.UbicacionId,
                        Cantidad = articulo.CantidadGlobal,
                        ActualizadoEn = DateTime.UtcNow
                    };
                    _context.Existencias.Add(existencia);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(ArticuloViewModel viewModel)
    {
        try
        {
            var articulo = await _context.Articulos.FindAsync(viewModel.Id);
            if (articulo == null || articulo.Eliminado) return false;

            if (viewModel.ImagenFile != null)
            {
                if (!string.IsNullOrEmpty(articulo.ImagenUrl))
                {
                    await _fileStorageService.DeleteFileAsync(articulo.ImagenUrl);
                }
                articulo.ImagenUrl = await _fileStorageService.SaveFileAsync(viewModel.ImagenFile, "articulos");
            }

            articulo.Codigo = viewModel.Codigo;
            articulo.Nombre = viewModel.Nombre;
            articulo.Descripcion = viewModel.Descripcion;
            articulo.Marca = viewModel.Marca;
            articulo.Modelo = viewModel.Modelo;
            articulo.NumeroSerie = viewModel.NumeroSerie;
            articulo.Precio = viewModel.Precio;
            articulo.FechaAdquisicion = viewModel.FechaAdquisicion;
            articulo.CategoriaId = viewModel.CategoriaId;
            articulo.EstadoId = viewModel.EstadoId;
            articulo.UbicacionId = viewModel.UbicacionId;
            articulo.Activo = viewModel.Activo;
            articulo.ActualizadoEn = DateTime.UtcNow;

            _context.Articulos.Update(articulo);
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
            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null || articulo.Eliminado) return false;

            articulo.Eliminado = true;
            articulo.ActualizadoEn = DateTime.UtcNow;

            _context.Articulos.Update(articulo);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null)
    {
        var query = _context.Articulos.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }
        return await query.AnyAsync(a => a.Codigo.ToLower() == codigo.ToLower() && !a.Eliminado);
    }

    public async Task<IEnumerable<(int Id, string Nombre)>> GetCategoriasAsync()
    {
         return await _context.Categorias
            .Where(x => x.Activo && !x.Eliminado)
            .OrderBy(x => x.Nombre)
            .Select(x => new ValueTuple<int, string>(x.Id, x.Nombre))
            .ToListAsync();
    }

    public async Task<IEnumerable<(int Id, string Nombre, string Color)>> GetEstadosAsync()
    {
        return await _context.EstadosArticulos
            .Where(x => x.Activo && !x.Eliminado)
            .OrderBy(x => x.Nombre)
            .Select(x => new ValueTuple<int, string, string>(x.Id, x.Nombre, x.Color ?? "secondary"))
            .ToListAsync();
    }

    public async Task<IEnumerable<(int Id, string Nombre)>> GetUbicacionesAsync()
    {
        return await _context.Ubicaciones
            .Where(x => x.Activo && !x.Eliminado)
            .OrderBy(x => x.Nombre)
            .Select(x => new ValueTuple<int, string>(x.Id, x.Nombre))
            .ToListAsync();
    }
}
