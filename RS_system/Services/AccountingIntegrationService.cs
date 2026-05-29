using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

public class AccountingIntegrationService : IAccountingIntegrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IContabilidadPartidaDobleService _contabilidadService;
    private readonly ILogger<AccountingIntegrationService> _logger;

    public AccountingIntegrationService(
        ApplicationDbContext context,
        IContabilidadPartidaDobleService contabilidadService,
        ILogger<AccountingIntegrationService> logger)
    {
        _context = context;
        _contabilidadService = contabilidadService;
        _logger = logger;
    }

    public async Task<List<PartidaContable>> ProcesarCierreDiarioAsync(long cabeceraId, string usuario)
    {
        var cabecera = await _context.DiarioFinancieroCabeceras
            .Include(c => c.Detalles).ThenInclude(d => d.CategoriaIngreso)
            .Include(c => c.Detalles).ThenInclude(d => d.CategoriaEgreso)
            .FirstOrDefaultAsync(c => c.Id == cabeceraId);

        if (cabecera == null)
            throw new ArgumentException($"Cabecera de diario {cabeceraId} no encontrada.");

        if (cabecera.Detalles.Count == 0)
        {
            _logger.LogWarning("El diario {DiarioId} no tiene movimientos para procesar.", cabeceraId);
            return new List<PartidaContable>();
        }

        var cuentaCaja = await _contabilidadService.GetCuentaCajaDefaultAsync();
        var periodo = await _contabilidadService.GetOrCreatePeriodoAsync(cabecera.Fecha.Month, cabecera.Fecha.Year);
        
        var partidasGeneradas = new List<PartidaContable>();

        try
        {
            // 1. Procesar INGRESOS (Agrupados)
            var ingresos = cabecera.Detalles.Where(d => d.Tipo == 1 && d.Monto > 0).ToList();
            if (ingresos.Any())
            {
                var partidaIngreso = new PartidaContable
                {
                    Fecha = cabecera.Fecha,
                    Referencia = $"CIERRE-ING-{cabecera.Fecha:yyyyMMdd}",
                    Descripcion = $"Cierre de Ingresos Diarios - {cabecera.Fecha:dd/MM/yyyy}",
                    PeriodoContableId = periodo.Id
                };

                var detallesIngreso = new List<DetallePartidaContable>();
                
                // Debe: Total a Caja
                detallesIngreso.Add(new DetallePartidaContable
                {
                    CuentaContableId = cuentaCaja.Id,
                    Debito = ingresos.Sum(i => i.Monto),
                    Credito = 0,
                    Descripcion = "Total Ingresos del Día"
                });

                // Haber: Desglose por Cuentas de Ingreso
                var ingresosPorCuenta = ingresos
                    .GroupBy(i => i.CategoriaIngreso?.CuentaContableId)
                    .ToList();

                foreach (var grupo in ingresosPorCuenta)
                {
                    if (!grupo.Key.HasValue)
                        throw new InvalidOperationException($"Existen categorías de ingreso sin cuenta contable configurada.");

                    var primerItem = grupo.First();
                    var nombreCategoria = primerItem.CategoriaIngreso?.Nombre ?? "Sin categoría";

                    detallesIngreso.Add(new DetallePartidaContable
                    {
                        CuentaContableId = grupo.Key.Value,
                        Debito = 0,
                        Credito = grupo.Sum(i => i.Monto),
                        Descripcion = $"Ingresos por {nombreCategoria}"
                    });
                }

                var pIng = await _contabilidadService.CreatePartidaAsync(partidaIngreso, detallesIngreso);
                partidasGeneradas.Add(pIng);
            }

            // 2. Procesar EGRESOS (Agrupados)
            var egresos = cabecera.Detalles.Where(d => d.Tipo == 2 && d.Monto > 0).ToList();
            if (egresos.Any())
            {
                var partidaEgreso = new PartidaContable
                {
                    Fecha = cabecera.Fecha,
                    Referencia = $"CIERRE-EGR-{cabecera.Fecha:yyyyMMdd}",
                    Descripcion = $"Cierre de Egresos Diarios - {cabecera.Fecha:dd/MM/yyyy}",
                    PeriodoContableId = periodo.Id
                };

                var detallesEgreso = new List<DetallePartidaContable>();

                // Debe: Desglose por Cuentas de Gasto
                var egresosPorCuenta = egresos
                    .GroupBy(e => e.CategoriaEgreso?.CuentaContableId)
                    .ToList();

                foreach (var grupo in egresosPorCuenta)
                {
                    if (!grupo.Key.HasValue)
                        throw new InvalidOperationException($"Existen categorías de egreso sin cuenta contable configurada.");

                    var primerItem = grupo.First();
                    var nombreCategoria = primerItem.CategoriaEgreso?.Nombre ?? "Sin categoría";

                    detallesEgreso.Add(new DetallePartidaContable
                    {
                        CuentaContableId = grupo.Key.Value,
                        Debito = grupo.Sum(e => e.Monto),
                        Credito = 0,
                        Descripcion = $"Gastos por {nombreCategoria}"
                    });
                }

                // Haber: Total a Caja
                detallesEgreso.Add(new DetallePartidaContable
                {
                    CuentaContableId = cuentaCaja.Id,
                    Debito = 0,
                    Credito = egresos.Sum(e => e.Monto),
                    Descripcion = "Total Egresos del Día"
                });

                var pEgr = await _contabilidadService.CreatePartidaAsync(partidaEgreso, detallesEgreso);
                partidasGeneradas.Add(pEgr);
            }

            _logger.LogInformation("Integración contable completada para diario {DiarioId}. Partidas generadas: {Count}", cabeceraId, partidasGeneradas.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la integración contable del diario {DiarioId}", cabeceraId);
            throw;
        }

        return partidasGeneradas;
    }
}
