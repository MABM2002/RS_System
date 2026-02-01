using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class PrestamoService : IPrestamoService
{
    private readonly ApplicationDbContext _context;

    public PrestamoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Prestamo>> GetHistorialPrestamosAsync(int limit = 100)
    {
        return await _context.Prestamos
            .Include(p => p.Articulo)
            .OrderByDescending(p => p.FechaPrestamo)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Prestamo>> GetPrestamosActivosAsync()
    {
        return await _context.Prestamos
            .Include(p => p.Articulo)
            .Where(p => p.Estado == "ACTIVO" || p.Estado == "ATRASADO")
            .OrderByDescending(p => p.FechaPrestamo)
            .ToListAsync();
    }

    public async Task<Prestamo?> GetPrestamoByIdAsync(long id)
    {
        return await _context.Prestamos
            .Include(p => p.Articulo)
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> RegistrarPrestamoAsync(int articuloId, int cantidad, string personaNombre, string? personaIdentificacion, DateTime? fechaDevolucionEstimada, string observacion, string usuario)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var articulo = await _context.Articulos.FindAsync(articuloId);
                if (articulo == null) return false;

                // 1. Validar y actualizar stock
                if (articulo.TipoControl == nameof(Articulo.TipoControlInventario.LOTE))
                {
                    var existencia = await _context.Existencias
                        .FirstOrDefaultAsync(e => e.ArticuloId == articuloId && e.UbicacionId == articulo.UbicacionId);

                    if (existencia == null || existencia.Cantidad < cantidad) return false;

                    existencia.Cantidad -= cantidad;
                    articulo.CantidadGlobal -= cantidad;
                    _context.Existencias.Update(existencia);
                    _context.Articulos.Update(articulo);
                }
                else
                {
                    // Unitario
                    if (cantidad != 1 || !articulo.Activo) return false;
                    // En unitario, podrías marcar como inactivo o simplemente registrar el préstamo
                    // Para este sistema, asumiremos que prestado sigue siendo "Activo" pero en una ubicación de préstamo (vía movimiento)
                }

                // 2. Crear el registro de préstamo
                var prestamo = new Prestamo
                {
                    ArticuloId = articuloId,
                    Cantidad = cantidad,
                    PersonaNombre = personaNombre,
                    PersonaIdentificacion = personaIdentificacion,
                    FechaPrestamo = DateTime.UtcNow,
                    FechaDevolucionEstimada = fechaDevolucionEstimada,
                    Estado = "ACTIVO",
                    Observacion = observacion,
                    UsuarioId = usuario
                };

                _context.Prestamos.Add(prestamo);
                await _context.SaveChangesAsync(); // Guardamos para tener el ID del préstamo

                // 3. Crear movimiento de inventario (auditoría)
                var movimiento = new MovimientoInventario
                {
                    ArticuloId = articuloId,
                    TipoMovimiento = nameof(TipoMovimiento.PRESTAMO),
                    TipMov = 2,
                    Fecha = DateTime.UtcNow,
                    UbicacionOrigenId = articulo.UbicacionId,
                    UbicacionDestinoId = articulo.UbicacionId,
                    EstadoAnteriorId = articulo.EstadoId,
                    EstadoNuevoId = articulo.EstadoId,
                    Cantidad = cantidad,
                    Observacion = $"Préstamo #{prestamo.Id} a {personaNombre}. {observacion}",
                    UsuarioId = usuario
                };

                _context.MovimientosInventario.Add(movimiento);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        });
    }

    public async Task<bool> RegistrarDevolucionAsync(
    long prestamoId,
    string observacion,
    string usuario)
{
    var strategy = _context.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync(async () =>
    {
        try
        {
            var prestamo = await _context.Prestamos.FindAsync(prestamoId);
            if (prestamo == null)
                return false;

            if (prestamo.Estado == "DEVUELTO")
                return false;

            var articulo = await _context.Articulos.FindAsync(prestamo.ArticuloId);
            if (articulo == null)
                return false;

            var fechaActual = DateTime.UtcNow;

            if (articulo.TipoControl == nameof(Articulo.TipoControlInventario.LOTE))
            {
                // --- Buscar existencia ---
                var existencia = await _context.Existencias
                    .FirstOrDefaultAsync(e =>
                        e.ArticuloId == articulo.Id &&
                        e.UbicacionId == articulo.UbicacionId);

                // --- Crear existencia si no existe ---
                if (existencia == null)
                {
                    existencia = new Existencia
                    {
                        ArticuloId = articulo.Id,
                        UbicacionId = articulo.UbicacionId,
                        Cantidad = 0,
                        ActualizadoEn = fechaActual
                    };
                    _context.Existencias.Add(existencia);
                }

                // --- Actualizar cantidades ---
                existencia.Cantidad += prestamo.Cantidad;
                existencia.ActualizadoEn = fechaActual;

                articulo.CantidadGlobal += prestamo.Cantidad;
                articulo.ActualizadoEn = fechaActual;

                _context.Existencias.Update(existencia);
                _context.Articulos.Update(articulo);
            }
            else
            {
                articulo.Activo = true;
                articulo.ActualizadoEn = fechaActual;
                _context.Articulos.Update(articulo);
            }

            prestamo.Estado = "DEVUELTO";
            prestamo.FechaDevolucionReal = fechaActual;
            prestamo.Observacion =
                $"{prestamo.Observacion}\nDevolución: {observacion}";

            _context.Prestamos.Update(prestamo);

            var movimiento = new MovimientoInventario
            {
                ArticuloId = articulo.Id,
                TipoMovimiento = nameof(TipoMovimiento.DEVOLUCION),
                TipMov = 1, // ENTRADA
                Fecha = fechaActual,
                UbicacionOrigenId = articulo.UbicacionId,
                UbicacionDestinoId = articulo.UbicacionId,
                EstadoAnteriorId = articulo.EstadoId,
                EstadoNuevoId = articulo.EstadoId,
                Cantidad = prestamo.Cantidad,
                Observacion = $"Devolución de préstamo #{prestamo.Id}. {observacion}",
                UsuarioId = usuario
            };

            _context.MovimientosInventario.Add(movimiento);

            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    });
}


    public async Task<bool> RegistrarDevolucionParcialAsync(long prestamoId, List<string> codigosDevolucion, string observacion, string usuario)
    {
        // Implementación básica para seguir la interfaz, aunque el controlador actual no la usa directamente
        // Esta lógica sería más para artículos unitarios con códigos específicos (PrestamoDetalle)
        return await RegistrarDevolucionAsync(prestamoId, "Devolución parcial - " + observacion, usuario);
    }
}
