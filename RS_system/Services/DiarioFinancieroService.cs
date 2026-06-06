using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public class DiarioFinancieroService : IDiarioFinancieroService
{
    private readonly ApplicationDbContext _context;
    private readonly IAccountingIntegrationService _accountingIntegration;
    private readonly IConfiguracionService _configService;

    public DiarioFinancieroService(
        ApplicationDbContext context,
        IAccountingIntegrationService accountingIntegration,
        IConfiguracionService configService)
    {
        _context = context;
        _accountingIntegration = accountingIntegration;
        _configService = configService;
    }

    // ══════════════════════════════════════════════════
    //  CABECERAS
    // ══════════════════════════════════════════════════

    public async Task<List<DiarioFinancieroIndexViewModel>> ListarCabecerasAsync(
        DateTime? fechaInicio, DateTime? fechaFin, string? estado)
    {
        var query = _context.DiarioFinancieroCabeceras
            .Include(c => c.Detalles)
            .AsNoTracking()
            .AsQueryable();

        if (fechaInicio.HasValue)
            query = query.Where(c => c.Fecha >= fechaInicio.Value);

        if (fechaFin.HasValue)
            query = query.Where(c => c.Fecha <= fechaFin.Value);

        if (!string.IsNullOrEmpty(estado))
            query = query.Where(c => c.Estado == estado);

        return await query
            .OrderByDescending(c => c.Fecha)
            .Select(c => new DiarioFinancieroIndexViewModel
            {
                Id = c.Id,
                Fecha = c.Fecha,
                Estado = c.Estado,
                TotalIngresos = c.TotalIngresos,
                TotalEgresos = c.TotalEgresos,
                SaldoDia = c.SaldoDia,
                CantidadMovimientos = c.Detalles.Count,
                CreadoPor = c.CreadoPor
            })
            .ToListAsync();
    }

    public async Task<DiarioFinancieroCabecera?> ObtenerCabeceraAsync(long id)
    {
        return await _context.DiarioFinancieroCabeceras
            .Include(c => c.Detalles).ThenInclude(d => d.CategoriaIngreso)
            .Include(c => c.Detalles).ThenInclude(d => d.CategoriaEgreso)
            .Include(c => c.Detalles).ThenInclude(d => d.MetodoPago)
            .Include(c => c.Detalles).ThenInclude(d => d.Adjuntos)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<DiarioFinancieroCabecera> CrearCabeceraAsync(DateTime fecha, string? observaciones, string usuario)
    {
        // Check for duplicate date
        var existe = await _context.DiarioFinancieroCabeceras
            .AnyAsync(c => c.Fecha == fecha.Date);

        if (existe)
            throw new InvalidOperationException($"Ya existe un diario para la fecha {fecha:dd/MM/yyyy}.");

        var cabecera = new DiarioFinancieroCabecera
        {
            Fecha = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc),
            Estado = "Abierto",
            TotalIngresos = 0,
            TotalEgresos = 0,
            SaldoDia = 0,
            Observaciones = observaciones,
            CreadoPor = usuario,
            FechaCreacion = DateTime.UtcNow
        };

        _context.DiarioFinancieroCabeceras.Add(cabecera);
        await _context.SaveChangesAsync();
        return cabecera;
    }

    public async Task<bool> ActualizarObservacionesCabeceraAsync(long id, string? observaciones, string usuario)
    {
        var cabecera = await _context.DiarioFinancieroCabeceras.FindAsync(id);
        if (cabecera == null || cabecera.EstaCerrado) return false;

        _context.Entry(cabecera).Property(x => x.Observaciones).CurrentValue = observaciones;
        _context.Entry(cabecera).Property(x => x.Observaciones).IsModified = true;
        _context.Entry(cabecera).Property(x => x.ModificadoPor).CurrentValue = usuario;
        _context.Entry(cabecera).Property(x => x.ModificadoPor).IsModified = true;
        _context.Entry(cabecera).Property(x => x.FechaModificacion).CurrentValue = DateTime.UtcNow;
        _context.Entry(cabecera).Property(x => x.FechaModificacion).IsModified = true;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CerrarDiarioAsync(long id, string usuario)
    {
        var cabecera = await _context.DiarioFinancieroCabeceras.FindAsync(id);
        if (cabecera == null || cabecera.EstaCerrado) return false;

        try
        {
            _context.Entry(cabecera).Property(x => x.Estado).CurrentValue = "Cerrado";
            _context.Entry(cabecera).Property(x => x.Estado).IsModified = true;
            _context.Entry(cabecera).Property(x => x.ModificadoPor).CurrentValue = usuario;
            _context.Entry(cabecera).Property(x => x.ModificadoPor).IsModified = true;
            _context.Entry(cabecera).Property(x => x.FechaModificacion).CurrentValue = DateTime.UtcNow;
            _context.Entry(cabecera).Property(x => x.FechaModificacion).IsModified = true;
            _context.DiarioFinancieroCabeceras.Entry(cabecera).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            // Integración Contable Automática
            await _accountingIntegration.ProcesarCierreDiarioAsync(id, usuario);
            return true;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> ReabrirDiarioAsync(long id, string usuario)
    {
        var cabecera = await _context.DiarioFinancieroCabeceras.FindAsync(id);
        if (cabecera == null || cabecera.EstaAbierto) return false;

        _context.Entry(cabecera).Property(x => x.Estado).CurrentValue = "Abierto";
        _context.Entry(cabecera).Property(x => x.Estado).IsModified = true;
        _context.Entry(cabecera).Property(x => x.ModificadoPor).CurrentValue = usuario;
        _context.Entry(cabecera).Property(x => x.ModificadoPor).IsModified = true;
        _context.Entry(cabecera).Property(x => x.FechaModificacion).CurrentValue = DateTime.UtcNow;
        _context.Entry(cabecera).Property(x => x.FechaModificacion).IsModified = true;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarCabeceraAsync(long id)
    {
        var cabecera = await _context.DiarioFinancieroCabeceras.FindAsync(id);
        if (cabecera == null || cabecera.EstaCerrado) return false;

        _context.DiarioFinancieroCabeceras.Remove(cabecera);
        await _context.SaveChangesAsync();
        return true;
    }

    // ══════════════════════════════════════════════════
    //  DETALLES (MOVIMIENTOS)
    // ══════════════════════════════════════════════════

    public async Task<DiarioFinancieroDetalle?> ObtenerDetalleAsync(long id)
    {
        return await _context.DiarioFinancieroDetalles
            .Include(d => d.CategoriaIngreso)
            .Include(d => d.CategoriaEgreso)
            .Include(d => d.MetodoPago)
            .Include(d => d.Adjuntos)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DiarioFinancieroDetalle?> GuardarMovimientoAsync(DiarioMovimientoInput input, string usuario)
    {
        // Validate header exists and is open
        var cabecera = await _context.DiarioFinancieroCabeceras.FindAsync(input.CabeceraId);
        if (cabecera == null || cabecera.EstaCerrado) return null;

        DiarioFinancieroDetalle? detalle;

        if (input.Id > 0)
        {
            // Update existing
            detalle = await _context.DiarioFinancieroDetalles.FindAsync(input.Id);
            if (detalle == null || detalle.CabeceraId != input.CabeceraId) return null;

            _context.Entry(detalle).Property(x => x.FechaMovimiento).CurrentValue = DateTime.SpecifyKind(input.FechaMovimiento, DateTimeKind.Utc);
            _context.Entry(detalle).Property(x => x.FechaMovimiento).IsModified = true;
            _context.Entry(detalle).Property(x => x.Tipo).CurrentValue = input.Tipo;
            _context.Entry(detalle).Property(x => x.Tipo).IsModified = true;
            _context.Entry(detalle).Property(x => x.CategoriaIngresoId).CurrentValue = input.Tipo == 1 ? input.CategoriaIngresoId : null;
            _context.Entry(detalle).Property(x => x.CategoriaIngresoId).IsModified = true;
            _context.Entry(detalle).Property(x => x.CategoriaEgresoId).CurrentValue = input.Tipo == 2 ? input.CategoriaEgresoId : null;
            _context.Entry(detalle).Property(x => x.CategoriaEgresoId).IsModified = true;
            _context.Entry(detalle).Property(x => x.NumeroComprobante).CurrentValue = input.NumeroComprobante;
            _context.Entry(detalle).Property(x => x.NumeroComprobante).IsModified = true;
            _context.Entry(detalle).Property(x => x.Descripcion).CurrentValue = input.Descripcion;
            _context.Entry(detalle).Property(x => x.Descripcion).IsModified = true;
            _context.Entry(detalle).Property(x => x.Monto).CurrentValue = input.Monto;
            _context.Entry(detalle).Property(x => x.Monto).IsModified = true;
            _context.Entry(detalle).Property(x => x.MetodoPagoId).CurrentValue = input.MetodoPagoId;
            _context.Entry(detalle).Property(x => x.MetodoPagoId).IsModified = true;
            _context.Entry(detalle).Property(x => x.Tercero).CurrentValue = input.Tercero;
            _context.Entry(detalle).Property(x => x.Tercero).IsModified = true;
            _context.Entry(detalle).Property(x => x.Observaciones).CurrentValue = input.Observaciones;
            _context.Entry(detalle).Property(x => x.Observaciones).IsModified = true;
            _context.Entry(detalle).Property(x => x.ModificadoPor).CurrentValue = usuario;
            _context.Entry(detalle).Property(x => x.ModificadoPor).IsModified = true;
            _context.Entry(detalle).Property(x => x.FechaModificacion).CurrentValue = DateTime.UtcNow;
            _context.Entry(detalle).Property(x => x.FechaModificacion).IsModified = true;
        }
        else
        {
            // Create new
            detalle = new DiarioFinancieroDetalle
            {
                CabeceraId = input.CabeceraId,
                FechaMovimiento = DateTime.SpecifyKind(input.FechaMovimiento, DateTimeKind.Utc),
                Tipo = input.Tipo,
                CategoriaIngresoId = input.Tipo == 1 ? input.CategoriaIngresoId : null,
                CategoriaEgresoId = input.Tipo == 2 ? input.CategoriaEgresoId : null,
                NumeroComprobante = input.NumeroComprobante,
                Descripcion = input.Descripcion,
                Monto = input.Monto,
                MetodoPagoId = input.MetodoPagoId,
                Tercero = input.Tercero,
                Observaciones = input.Observaciones,
                CreadoPor = usuario,
                FechaCreacion = DateTime.UtcNow
            };
            _context.DiarioFinancieroDetalles.Add(detalle);
        }

        await _context.SaveChangesAsync();

        // Recalculate header totals
        await RecalcularTotalesAsync(input.CabeceraId);

        return detalle;
    }

    public async Task<bool> EliminarMovimientoAsync(long id, string usuario)
    {
        var detalle = await _context.DiarioFinancieroDetalles.FindAsync(id);
        if (detalle == null) return false;

        var cabeceraId = detalle.CabeceraId;

        // Verify header is open
        var cabecera = await _context.DiarioFinancieroCabeceras.FindAsync(cabeceraId);
        if (cabecera == null || cabecera.EstaCerrado) return false;

        _context.DiarioFinancieroDetalles.Remove(detalle);
        await _context.SaveChangesAsync();

        // Recalculate header totals
        await RecalcularTotalesAsync(cabeceraId);

        return true;
    }

    // ══════════════════════════════════════════════════
    //  RECALCULAR TOTALES
    // ══════════════════════════════════════════════════

    public async Task RecalcularTotalesAsync(long cabeceraId)
    {
        var totalIngresos = await _context.DiarioFinancieroDetalles
            .Where(d => d.CabeceraId == cabeceraId && d.Tipo == 1)
            .SumAsync(d => (decimal?)d.Monto) ?? 0;

        var totalEgresos = await _context.DiarioFinancieroDetalles
            .Where(d => d.CabeceraId == cabeceraId && d.Tipo == 2)
            .SumAsync(d => (decimal?)d.Monto) ?? 0;

        var cabecera = await _context.DiarioFinancieroCabeceras.FindAsync(cabeceraId);
        if (cabecera == null) return;

        _context.Entry(cabecera).Property(x => x.TotalIngresos).CurrentValue = totalIngresos;
        _context.Entry(cabecera).Property(x => x.TotalIngresos).IsModified = true;
        _context.Entry(cabecera).Property(x => x.TotalEgresos).CurrentValue = totalEgresos;
        _context.Entry(cabecera).Property(x => x.TotalEgresos).IsModified = true;
        _context.Entry(cabecera).Property(x => x.SaldoDia).CurrentValue = totalIngresos - totalEgresos;
        _context.Entry(cabecera).Property(x => x.SaldoDia).IsModified = true;

        await _context.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════
    //  ADJUNTOS
    // ══════════════════════════════════════════════════

    public async Task<List<DiarioFinancieroAdjunto>> ObtenerAdjuntosAsync(long detalleId)
    {
        return await _context.DiarioFinancieroAdjuntos
            .Where(a => a.DetalleId == detalleId)
            .OrderByDescending(a => a.FechaSubida)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DiarioFinancieroAdjunto?> CrearAdjuntoAsync(long detalleId, string nombreArchivo, string rutaArchivo, string? tipoContenido)
    {
        var detalle = await _context.DiarioFinancieroDetalles.FindAsync(detalleId);
        if (detalle == null) return null;

        var adjunto = new DiarioFinancieroAdjunto
        {
            DetalleId = detalleId,
            NombreArchivo = nombreArchivo,
            RutaArchivo = rutaArchivo,
            TipoContenido = tipoContenido,
            FechaSubida = DateTime.UtcNow
        };

        _context.DiarioFinancieroAdjuntos.Add(adjunto);
        await _context.SaveChangesAsync();
        return adjunto;
    }

    public async Task<bool> EliminarAdjuntoAsync(long adjuntoId)
    {
        var adjunto = await _context.DiarioFinancieroAdjuntos.FindAsync(adjuntoId);
        if (adjunto == null) return false;

        _context.DiarioFinancieroAdjuntos.Remove(adjunto);
        await _context.SaveChangesAsync();
        return true;
    }

    // ══════════════════════════════════════════════════
    //  CATÁLOGOS
    // ══════════════════════════════════════════════════

    public async Task<List<MetodoPago>> ObtenerMetodosPagoAsync()
    {
        return await _context.MetodosPago
            .Where(m => m.Activo)
            .OrderBy(m => m.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<CategoriaIngreso>> ObtenerCategoriasIngresoAsync()
    {
        return await _context.CategoriasIngreso
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<CategoriaEgreso>> ObtenerCategoriasEgresoAsync()
    {
        return await _context.CategoriasEgreso
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    // ══════════════════════════════════════════════════
    //  REPORTE
    // ══════════════════════════════════════════════════

    public async Task<DiarioFinancieroReporteViewModel> GenerarReporteAsync(DiarioFinancieroFiltroViewModel filtro)
    {
        var query = _context.DiarioFinancieroDetalles
            .Include(d => d.CategoriaIngreso)
            .Include(d => d.CategoriaEgreso)
            .Include(d => d.MetodoPago)
            .Include(d => d.Cabecera)
            .AsNoTracking()
            .AsQueryable();

        if (filtro.FechaInicio.HasValue)
            query = query.Where(d => d.FechaMovimiento >= DateTime.SpecifyKind(filtro.FechaInicio.Value, DateTimeKind.Utc));

        if (filtro.FechaFin.HasValue)
        {
            var fechaFinUtc = DateTime.SpecifyKind(filtro.FechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(d => d.FechaMovimiento <= fechaFinUtc);
        }

        if (filtro.Tipo.HasValue)
            query = query.Where(d => d.Tipo == filtro.Tipo.Value);

        if (filtro.CategoriaIngresoId.HasValue)
            query = query.Where(d => d.CategoriaIngresoId == filtro.CategoriaIngresoId.Value);

        if (filtro.CategoriaEgresoId.HasValue)
            query = query.Where(d => d.CategoriaEgresoId == filtro.CategoriaEgresoId.Value);

        if (filtro.MetodoPagoId.HasValue)
            query = query.Where(d => d.MetodoPagoId == filtro.MetodoPagoId.Value);

        var movimientos = await query
            .OrderByDescending(d => d.FechaMovimiento)
            .ToListAsync();

        var totalIngresos = movimientos.Where(m => m.Tipo == 1).Sum(m => m.Monto);
        var totalEgresos = movimientos.Where(m => m.Tipo == 2).Sum(m => m.Monto);

        return new DiarioFinancieroReporteViewModel
        {
            Filtro = filtro,
            Movimientos = movimientos,
            TotalIngresos = totalIngresos,
            TotalEgresos = totalEgresos,
            Saldo = totalIngresos - totalEgresos,
            CategoriasIngreso = await ObtenerCategoriasIngresoAsync(),
            CategoriasEgreso = await ObtenerCategoriasEgresoAsync(),
            MetodosPago = await ObtenerMetodosPagoAsync(),
            NombreIglesia = await _configService.GetValorOrDefaultAsync("NAME_CHURCH", "Iglesia"),
            DireccionIglesia = await _configService.GetValorAsync("church_address"),
            TelefonoIglesia = await _configService.GetValorAsync("church_phone"),
            EmailIglesia = await _configService.GetValorAsync("church_email")
        };
    }
}
