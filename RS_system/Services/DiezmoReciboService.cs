using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class DiezmoReciboService : IDiezmoReciboService
{
    private readonly ApplicationDbContext _context;

    public DiezmoReciboService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<string> GenerarNumeroReciboAsync(long salidaId)
    {
        var salida = await _context.DiezmoSalidas
            .FirstOrDefaultAsync(s => s.Id == salidaId && !s.Eliminado)
            ?? throw new InvalidOperationException("Salida no encontrada.");

        // Si ya tiene correlativo, devolverlo
        if (!string.IsNullOrEmpty(salida.NumeroRecibo))
            return salida.NumeroRecibo;

        var anio = salida.CreadoEn.Year;
        var correlativo = $"RECDZ-{anio}-{salidaId:D6}";

        salida.NumeroRecibo  = correlativo;
        salida.ActualizadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return correlativo;
    }

    /// <inheritdoc/>
    public async Task<DiezmoSalida?> GetSalidaParaReciboAsync(long salidaId)
        => await _context.DiezmoSalidas
            .Include(s => s.DiezmoCierre)
            .Include(s => s.TipoSalida)
            .Include(s => s.Beneficiario)
            .FirstOrDefaultAsync(s => s.Id == salidaId && !s.Eliminado);
}
