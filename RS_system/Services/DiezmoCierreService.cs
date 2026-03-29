using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public class DiezmoCierreService : IDiezmoCierreService
{
    private readonly ApplicationDbContext  _context;
    private readonly IDiezmoCalculoService _calculo;

    public DiezmoCierreService(ApplicationDbContext context, IDiezmoCalculoService calculo)
    {
        _context = context;
        _calculo = calculo;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Catálogos
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<List<DiezmoTipoSalida>> GetTiposSalidaActivosAsync()
        => await _context.DiezmoTiposSalida
            .Where(t => t.Activo && !t.Eliminado)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

    public async Task<List<DiezmoBeneficiario>> GetBeneficiariosActivosAsync()
        => await _context.DiezmoBeneficiarios
            .Where(b => b.Activo && !b.Eliminado)
            .OrderBy(b => b.Nombre)
            .ToListAsync();

    // ──────────────────────────────────────────────────────────────────────────
    // Cierres
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<List<DiezmoCierre>> GetCierresAsync(int? anio = null)
    {
        var query = _context.DiezmoCierres
            .Where(c => !c.Eliminado);

        if (anio.HasValue)
            query = query.Where(c => c.Fecha.Year == anio.Value);

        return await query
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();
    }

    public async Task<DiezmoCierre?> GetCierreByIdAsync(long id)
        => await _context.DiezmoCierres
            .Include(c => c.Detalles.Where(d => !d.Eliminado))
                .ThenInclude(d => d.Miembro)
                    .ThenInclude(m => m.Persona)
            .Include(c => c.Salidas.Where(s => !s.Eliminado))
                .ThenInclude(s => s.TipoSalida)
            .Include(c => c.Salidas.Where(s => !s.Eliminado))
                .ThenInclude(s => s.Beneficiario)
            .FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);

    public async Task<DiezmoCierre> CrearCierreAsync(DateOnly fecha, string? observaciones, string creadoPor)
    {
        // Verificar que no exista ya un cierre para esa fecha
        var yaExiste = await _context.DiezmoCierres
            .AnyAsync(c => c.Fecha == fecha && !c.Eliminado);

        if (yaExiste)
            throw new InvalidOperationException($"Ya existe un cierre para la fecha {fecha:dd/MM/yyyy}.");

        var cierre = new DiezmoCierre
        {
            Fecha        = fecha,
            Observaciones = observaciones,
            CreadoPor    = creadoPor,
            CreadoEn     = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        _context.DiezmoCierres.Add(cierre);
        await _context.SaveChangesAsync();
        return cierre;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Detalles
    // ──────────────────────────────────────────────────────────────────────────

    public async Task AgregarDetalleAsync(long cierreId, DiezmoDetalleFormViewModel vm, string usuario)
    {
        var cierre = await GetCierreOrThrowAsync(cierreId);
        GuardarSiAbierto(cierre);
        
        var neto = vm.MontoNeto;
        var cambio = vm.MontoEntregado - neto;
        if (cambio < 0) cambio = 0;

        cierre.TotalCambio += cambio;
        cierre.TotalNeto += neto;
        cierre.TotalRecibido += vm.MontoEntregado;
        cierre.SaldoFinal = cierre.TotalNeto - cierre.TotalSalidas;
        
        var detalle = new DiezmoDetalle
        {
            DiezmoCierreId   = cierreId,
            MiembroId        = vm.MiembroId,
            MontoEntregado   = vm.MontoEntregado,
            CambioEntregado  = cambio,
            MontoNeto        = neto,
            Observaciones    = vm.Observaciones,
            Fecha            = DateTime.UtcNow,
            CreadoPor        = usuario,
            CreadoEn         = DateTime.UtcNow,
            ActualizadoEn    = DateTime.UtcNow
        };
        
        _context.DiezmoDetalles.Add(detalle);
        await _context.SaveChangesAsync();
        
        _context.Entry(cierre).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task EliminarDetalleAsync(long detalleId, string usuario)
    {
        var detalle = await _context.DiezmoDetalles
            .FirstOrDefaultAsync(d => d.Id == detalleId && !d.Eliminado)
            ?? throw new InvalidOperationException("Detalle no encontrado.");

        var cierre = await GetCierreOrThrowAsync(detalle.DiezmoCierreId);
        GuardarSiAbierto(cierre);

        detalle.Eliminado      = true;
        detalle.ActualizadoEn  = DateTime.UtcNow;
        detalle.ActualizadoPor = usuario;
        _context.Entry(detalle).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await RegistrarBitacoraAsync(detalle.DiezmoCierreId, "ELIMINAR_DETALLE",
            $"Detalle #{detalleId} eliminado", usuario);
        //await RecalcularTotalesAsync(detalle.DiezmoCierreId);
    }

    public async Task ActualizarDetalleAsync(long detalleId, DiezmoDetalleFormViewModel vm, string usuario)
    {
        var detalle = await _context.DiezmoDetalles
            .FirstOrDefaultAsync(d => d.Id == detalleId && !d.Eliminado)
            ?? throw new InvalidOperationException("Detalle no encontrado.");

        var cierre = await GetCierreOrThrowAsync(detalle.DiezmoCierreId);
        GuardarSiAbierto(cierre);

        detalle.MiembroId       = vm.MiembroId;
        detalle.MontoEntregado  = vm.MontoEntregado;
        detalle.MontoNeto       = vm.MontoNeto;
        var cambio = vm.MontoEntregado - vm.MontoNeto;
        detalle.CambioEntregado = cambio < 0 ? 0 : cambio;
        detalle.Observaciones   = vm.Observaciones;
        detalle.ActualizadoEn   = DateTime.UtcNow;
        detalle.ActualizadoPor  = usuario;

        _context.Entry(detalle).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await RegistrarBitacoraAsync(detalle.DiezmoCierreId, "ACTUALIZAR_DETALLE",
            $"Detalle #{detalleId} actualizado. Nuevo neto: {vm.MontoNeto:C}", usuario);
        await RecalcularTotalesAsync(detalle.DiezmoCierreId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Salidas
    // ──────────────────────────────────────────────────────────────────────────

    public async Task AgregarSalidaAsync(long cierreId, DiezmoSalidaFormViewModel vm, string usuario)
    {
        var cierre = await GetCierreOrThrowAsync(cierreId);
        GuardarSiAbierto(cierre);

        var salida = new DiezmoSalida
        {
            DiezmoCierreId  = cierreId,
            TipoSalidaId    = vm.TipoSalidaId,
            BeneficiarioId  = vm.BeneficiarioId,
            Monto           = vm.Monto,
            Concepto        = vm.Concepto,
            Fecha           = DateTime.UtcNow,
            CreadoPor       = usuario,
            CreadoEn        = DateTime.UtcNow,
            ActualizadoEn   = DateTime.UtcNow
        };

        _context.DiezmoSalidas.Add(salida);
        await _context.SaveChangesAsync();
        await RecalcularTotalesAsync(cierreId);
    }

    public async Task EliminarSalidaAsync(long salidaId, string usuario)
    {
        var salida = await _context.DiezmoSalidas
            .FirstOrDefaultAsync(s => s.Id == salidaId && !s.Eliminado)
            ?? throw new InvalidOperationException("Salida no encontrada.");

        var cierre = await GetCierreOrThrowAsync(salida.DiezmoCierreId);
        GuardarSiAbierto(cierre);

        salida.Eliminado    = true;
        salida.ActualizadoEn = DateTime.UtcNow;
        
        _context.Entry(salida).State = EntityState.Modified;
            
        await _context.SaveChangesAsync();

        await RegistrarBitacoraAsync(salida.DiezmoCierreId, "ELIMINAR_SALIDA",
            $"Salida #{salidaId} eliminada", usuario);
        await RecalcularTotalesAsync(salida.DiezmoCierreId);
    }

    public async Task ActualizarSalidaAsync(long salidaId, DiezmoSalidaFormViewModel vm, string usuario)
    {
        var salida = await _context.DiezmoSalidas
            .FirstOrDefaultAsync(s => s.Id == salidaId && !s.Eliminado)
            ?? throw new InvalidOperationException("Salida no encontrada.");

        var cierre = await GetCierreOrThrowAsync(salida.DiezmoCierreId);
        GuardarSiAbierto(cierre);

        salida.TipoSalidaId   = vm.TipoSalidaId;
        salida.BeneficiarioId = vm.BeneficiarioId;
        salida.Monto          = vm.Monto;
        salida.Concepto       = vm.Concepto;
        salida.ActualizadoEn  = DateTime.UtcNow;

        _context.Entry(salida).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await RegistrarBitacoraAsync(salida.DiezmoCierreId, "ACTUALIZAR_SALIDA",
            $"Salida #{salidaId} actualizada. Nuevo monto: {vm.Monto:C}", usuario);
        await RecalcularTotalesAsync(salida.DiezmoCierreId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Flujo de cierre / reapertura
    // ──────────────────────────────────────────────────────────────────────────

    public async Task CerrarCierreAsync(long cierreId, string usuario)
    {
        var cierre = await GetCierreByIdAsync(cierreId)
                     ?? throw new InvalidOperationException("Cierre no encontrado.");

        if (cierre.Cerrado)
            throw new InvalidOperationException("El cierre ya se encuentra cerrado.");

        cierre = _calculo.RecalcularTotales(cierre);

        cierre.Cerrado       = true;
        cierre.FechaCierre   = DateTime.UtcNow;
        cierre.CerradoPor    = usuario;
        cierre.ActualizadoEn = DateTime.UtcNow;
        cierre.ActualizadoPor = usuario;
        _context.Entry(cierre).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    
        await RegistrarBitacoraAsync(cierreId, "CIERRE", $"Cierre sellado. Saldo final: {cierre.SaldoFinal:C}", usuario);
        
    }
    
    
    public async Task RecalcularCierreAsync(long cierreId, string usuario)
    {
        var cierre = await GetCierreByIdAsync(cierreId)
                     ?? throw new InvalidOperationException("Cierre no encontrado.");

        if (cierre.Cerrado)
            throw new InvalidOperationException("El cierre ya se encuentra cerrado.");

        cierre = _calculo.RecalcularTotales(cierre);

        cierre.Cerrado       = false;
        //cierre.FechaCierre   = DateTime.UtcNow;
        //cierre.CerradoPor    = usuario;
        cierre.ActualizadoEn = DateTime.UtcNow;
        cierre.ActualizadoPor = usuario;
        _context.Entry(cierre).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    
        await RegistrarBitacoraAsync(cierreId, "RECALCULO_CIERRE", $"Recalculo Realizado. Saldo final: {cierre.SaldoFinal:C}", usuario);
        
    }

    public async Task ReabrirCierreAsync(long cierreId, string usuario)
    {
        var cierre = await GetCierreOrThrowAsync(cierreId);

        if (!cierre.Cerrado)
            throw new InvalidOperationException("El cierre ya se encuentra abierto.");

        cierre.Cerrado        = false;
        cierre.FechaCierre    = null;
        cierre.CerradoPor     = null;
        cierre.ActualizadoEn  = DateTime.UtcNow;
        cierre.ActualizadoPor = usuario;
        
        _context.Entry(cierre).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        await RegistrarBitacoraAsync(cierreId, "REAPERTURA", "Cierre reabierto", usuario);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Totales
    // ──────────────────────────────────────────────────────────────────────────

    public async Task RecalcularTotalesAsync(long cierreId)
    {
        var cierre = await GetCierreByIdAsync(cierreId)
            ?? throw new InvalidOperationException("Cierre no encontrado.");

        _calculo.RecalcularTotales(cierre);
        cierre.ActualizadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<DiezmoCierre> GetCierreOrThrowAsync(long id)
        => await _context.DiezmoCierres.FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado)
           ?? throw new InvalidOperationException("Cierre no encontrado.");

    private static void GuardarSiAbierto(DiezmoCierre cierre)
    {
        if (cierre.Cerrado)
            throw new InvalidOperationException("No se puede modificar un cierre que ya está cerrado.");
    }

    private async Task RegistrarBitacoraAsync(long cierreId, string accion, string detalle, string usuario)
    {
        await _context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.diezmo_bitacora (diezmo_cierre_id, accion, detalle, realizado_por, realizado_en)
            VALUES ({0}, {1}, {2}, {3}, {4})
            """,
            cierreId, accion, detalle, usuario, DateTime.UtcNow);
    }
}
