using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class MovimientoService : IMovimientoService
{
    private readonly ApplicationDbContext _context;

    public MovimientoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MovimientoInventario>> GetHistorialGeneralAsync(int limit = 100)
    {
        return await _context.MovimientosInventario
            .Include(m => m.Articulo)
            .Include(m => m.UbicacionOrigen)
            .Include(m => m.UbicacionDestino)
            .Include(m => m.EstadoAnterior)
            .Include(m => m.EstadoNuevo)
            .OrderByDescending(m => m.Fecha)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<MovimientoInventario>> GetHistorialPorArticuloAsync(int articuloId)
    {
        return await _context.MovimientosInventario
            .Include(m => m.UbicacionOrigen)
            .Include(m => m.UbicacionDestino)
            .Include(m => m.EstadoAnterior)
            .Include(m => m.EstadoNuevo)
            .Where(m => m.ArticuloId == articuloId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();
    }

    public async Task<bool> RegistrarTrasladoAsync(int articuloId, int nuevaUbicacionId, string observacion, string usuario)
    {
        return await RegistrarTrasladoCantidadAsync(articuloId, nuevaUbicacionId, 1, observacion, usuario);
    }

    public async Task<bool> RegistrarTrasladoCantidadAsync(
    int articuloId,
    int nuevaUbicacionId,
    int cantidad,
    string observacion,
    string usuario)
{
    var strategy = _context.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync(async () =>
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var articulo = await _context.Articulos.FindAsync(articuloId);
            if (articulo == null) return false;

            var fecha = DateTime.UtcNow;

            if (articulo.TipoControl == nameof(Articulo.TipoControlInventario.LOTE))
            {
                // ===== LOTE =====
                var origenExistencia = await _context.Existencias
                    .FirstOrDefaultAsync(e =>
                        e.ArticuloId == articuloId &&
                        e.UbicacionId == articulo.UbicacionId);

                if (origenExistencia == null || origenExistencia.Cantidad < cantidad)
                    return false;
        
                origenExistencia.Cantidad -= cantidad;
                origenExistencia.ActualizadoEn = fecha;
                _context.Existencias.Update(origenExistencia);

                
                var destinoExistencia = await _context.Existencias
                    .FirstOrDefaultAsync(e =>
                        e.ArticuloId == articuloId &&
                        e.UbicacionId == nuevaUbicacionId);

                if (destinoExistencia == null)
                {
                    destinoExistencia = new Existencia
                    {
                        ArticuloId = articuloId,
                        UbicacionId = nuevaUbicacionId,
                        Cantidad = 0,
                        ActualizadoEn = fecha
                    };
                    _context.Existencias.Add(destinoExistencia);
                }

                destinoExistencia.Cantidad += cantidad;
                destinoExistencia.ActualizadoEn = fecha;

                // 📉 MOVIMIENTO SALIDA
                var movSalida = new MovimientoInventario
                {
                    ArticuloId = articuloId,
                    TipoMovimiento = nameof(TipoMovimiento.TRASLADO),
                    Fecha = fecha,
                    UbicacionOrigenId = articulo.UbicacionId,
                    Cantidad = cantidad,
                    TipMov = 2,
                    Observacion = observacion,
                    UsuarioId = usuario,
                    EstadoAnteriorId = articulo.EstadoId,
                    EstadoNuevoId = articulo.EstadoId
                };

                // 📈 MOVIMIENTO ENTRADA
                var movEntrada = new MovimientoInventario
                {
                    ArticuloId = articuloId,
                    TipoMovimiento = nameof(TipoMovimiento.TRASLADO),
                    Fecha = fecha,
                    UbicacionDestinoId = nuevaUbicacionId,
                    Cantidad = cantidad,
                    TipMov = 1,
                    Observacion = observacion,
                    UsuarioId = usuario,
                    EstadoAnteriorId = articulo.EstadoId,
                    EstadoNuevoId = articulo.EstadoId
                };

                _context.MovimientosInventario.AddRange(movSalida, movEntrada);
            }
            else
            {
                // ===== UNITARIO =====
                if (articulo.UbicacionId == nuevaUbicacionId)
                    return false;

                // 📉 SALIDA
                var movSalida = new MovimientoInventario
                {
                    ArticuloId = articuloId,
                    TipoMovimiento = nameof(TipoMovimiento.TRASLADO),
                    Fecha = fecha,
                    UbicacionOrigenId = articulo.UbicacionId,
                    Cantidad = 1,
                    TipMov = 2,
                    Observacion = observacion,
                    UsuarioId = usuario,
                    EstadoAnteriorId = articulo.EstadoId,
                    EstadoNuevoId = articulo.EstadoId
                };

                // 📈 ENTRADA
                var movEntrada = new MovimientoInventario
                {
                    ArticuloId = articuloId,
                    TipoMovimiento = nameof(TipoMovimiento.TRASLADO),
                    Fecha = fecha,
                    UbicacionDestinoId = nuevaUbicacionId,
                    Cantidad = 1,
                    TipMov = 1,
                    Observacion = observacion,
                    UsuarioId = usuario,
                    EstadoAnteriorId = articulo.EstadoId,
                    EstadoNuevoId = articulo.EstadoId
                };

                articulo.UbicacionId = nuevaUbicacionId;
                articulo.ActualizadoEn = fecha;

                _context.Articulos.Update(articulo);
                _context.MovimientosInventario.AddRange(movSalida, movEntrada);
            }

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


    public async Task<bool> RegistrarCambioEstadoAsync(int articuloId, int nuevoEstadoId, string observacion, string usuario)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var articulo = await _context.Articulos.FindAsync(articuloId);
                if (articulo == null) return false;

                if (articulo.EstadoId == nuevoEstadoId) return false;

                var movimiento = new MovimientoInventario
                {
                    ArticuloId = articuloId,
                    TipoMovimiento = nameof(TipoMovimiento.CAMBIO_ESTADO),
                    Fecha = DateTime.UtcNow,
                    UbicacionOrigenId = articulo.UbicacionId,
                    UbicacionDestinoId = articulo.UbicacionId,
                    EstadoAnteriorId = articulo.EstadoId,
                    EstadoNuevoId = nuevoEstadoId,
                    Cantidad = (articulo.TipoControl == nameof(Articulo.TipoControlInventario.LOTE)) ? articulo.CantidadGlobal : 1,
                    Observacion = observacion,
                    UsuarioId = usuario
                };

                articulo.EstadoId = nuevoEstadoId;
                articulo.ActualizadoEn = DateTime.UtcNow;

                _context.MovimientosInventario.Add(movimiento);
                _context.Articulos.Update(articulo);
                
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

    public async Task<bool> RegistrarPrestamoAsync(int articuloId, int cantidad, string personaNombre, string? personaIdentificacion,
        DateTime? fechaDevolucionEstimada, string observacion, string usuario)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var articulo = await _context.Articulos.FindAsync(articuloId);
                if (articulo == null) return false;

                // Validar stock disponible
                if (articulo.TipoControl == nameof(Articulo.TipoControlInventario.LOTE))
                {
                    var existencia = await _context.Existencias
                        .FirstOrDefaultAsync(e => e.ArticuloId == articuloId && e.UbicacionId == articulo.UbicacionId);
                        
                    if (existencia == null || existencia.Cantidad < cantidad) return false;
                    
                    // Reducir stock existente
                    existencia.Cantidad -= cantidad;
                    articulo.CantidadGlobal -= cantidad;
                    
                    if (articulo.CantidadGlobal < 0) articulo.CantidadGlobal = 0;
                    if (existencia.Cantidad < 0) existencia.Cantidad = 0;
                    
                    _context.Existencias.Update(existencia);
                    _context.Articulos.Update(articulo);
                }
                else
                {
                    // UNITARIO - Solo se puede prestar 1
                    if (cantidad != 1) return false;
                    if (!articulo.Activo) return false;
                }

                // Crear movimiento de inventario
                var movimiento = new MovimientoInventario
                {
                    ArticuloId = articuloId,
                    TipoMovimiento = nameof(TipoMovimiento.PRESTAMO),
                    TipMov = 2,
                    Fecha = DateTime.UtcNow,
                    UbicacionOrigenId = articulo.UbicacionId,
                    UbicacionDestinoId = articulo.UbicacionId, // Mismo lugar, solo está prestado
                    EstadoAnteriorId = articulo.EstadoId,
                    EstadoNuevoId = articulo.EstadoId,
                    Cantidad = cantidad,
                    Observacion = $"Préstamo a {personaNombre}. {observacion}",
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

    public async Task<bool> RegistrarBajaAsync(int articuloId, string motivo, string usuario)
    {
        return await RegistrarBajaCantidadAsync(articuloId, 1, motivo, usuario);
    }

    public async Task<bool> RegistrarBajaCantidadAsync(int articuloId, int cantidad, string motivo, string usuario)
    {
         var strategy = _context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var articulo = await _context.Articulos.FindAsync(articuloId);
                if (articulo == null) return false;

                if (articulo.TipoControl == nameof(Articulo.TipoControlInventario.LOTE))
                {
                    var existencia = await _context.Existencias
                        .FirstOrDefaultAsync(e => e.ArticuloId == articuloId && e.UbicacionId == articulo.UbicacionId);
                        
                    if (existencia == null || existencia.Cantidad < cantidad) return false;

                    existencia.Cantidad -= cantidad;
                    articulo.CantidadGlobal -= cantidad;
                    
                    if (articulo.CantidadGlobal <= 0) articulo.Activo = false;

                     var movimiento = new MovimientoInventario
                    {
                        ArticuloId = articuloId,
                        TipoMovimiento = nameof(TipoMovimiento.BAJA),
                        TipMov = 2,
                        Fecha = DateTime.UtcNow,
                        UbicacionOrigenId = articulo.UbicacionId,
                        EstadoAnteriorId = articulo.EstadoId,
                        Cantidad = cantidad,
                        Observacion = motivo,
                        UsuarioId = usuario
                    };
                    
                    _context.Existencias.Update(existencia);
                    _context.MovimientosInventario.Add(movimiento);
                    _context.Articulos.Update(articulo);
                }
                else
                {
                    var movimiento = new MovimientoInventario
                    {
                        ArticuloId = articuloId,
                        TipoMovimiento = nameof(TipoMovimiento.BAJA),
                        TipMov = 2,
                        Fecha = DateTime.UtcNow,
                        UbicacionOrigenId = articulo.UbicacionId,
                        EstadoAnteriorId = articulo.EstadoId,
                        Cantidad = 1,
                        Observacion = motivo,
                        UsuarioId = usuario
                    };

                    articulo.Activo = false;
                    _context.MovimientosInventario.Add(movimiento);
                    _context.Articulos.Update(articulo);
                }
                
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

    public async Task<bool> RegistrarEntradaCantidadAsync(int articuloId, int cantidad, string observacion, string usuario)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                var articulo = await _context.Articulos.FindAsync(articuloId);
                if (articulo == null) return false;

                if (articulo.TipoControl == nameof(Articulo.TipoControlInventario.LOTE))
                {
                    var existencia = await _context.Existencias
                        .FirstOrDefaultAsync(e => e.ArticuloId == articuloId && e.UbicacionId == articulo.UbicacionId);
                        
                    if (existencia == null)
                    {
                        existencia = new Existencia
                        {
                            ArticuloId = articuloId,
                            UbicacionId = articulo.UbicacionId,
                            Cantidad = 0,
                            ActualizadoEn = DateTime.UtcNow
                        };
                        _context.Existencias.Add(existencia);
                    }

                    existencia.Cantidad += cantidad;
                    articulo.CantidadGlobal += cantidad;
                    articulo.ActualizadoEn = DateTime.UtcNow;
                    
                    _context.Existencias.Update(existencia);
                    _context.Articulos.Update(articulo);
                }
                else
                {
                    // UNITARIO - Si está inactivo por baja, lo reactivamos? 
                    // En unitario, una entrada suele ser que el objeto "vuelve" a estar disponible.
                    articulo.Activo = true;
                    articulo.ActualizadoEn = DateTime.UtcNow;
                    _context.Articulos.Update(articulo);
                }

                var movimiento = new MovimientoInventario
                {
                    ArticuloId = articuloId,
                    TipoMovimiento = nameof(TipoMovimiento.ENTRADA),
                    TipMov = 1,
                    Fecha = DateTime.UtcNow,
                    UbicacionDestinoId = articulo.UbicacionId,
                    EstadoAnteriorId = articulo.EstadoId,
                    EstadoNuevoId = articulo.EstadoId,
                    Cantidad = cantidad,
                    Observacion = observacion,
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
}
