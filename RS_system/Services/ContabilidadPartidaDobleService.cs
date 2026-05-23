using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;

namespace Rs_system.Services;

/// <summary>
/// Implementación del servicio de contabilidad por partida doble.
/// Gestiona catálogo de cuentas, asientos contables, reportes financieros y cierre periódico.
/// </summary>
public class ContabilidadPartidaDobleService : IContabilidadPartidaDobleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContabilidadPartidaDobleService> _logger;

    public ContabilidadPartidaDobleService(
        ApplicationDbContext context,
        ILogger<ContabilidadPartidaDobleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== Catálogo de Cuentas ====================

    public async Task<List<CuentaContable>> GetAllCuentasAsync()
    {
        return await _context.CuentasContables
            .Include(c => c.AccountType)
            .Include(c => c.Padre)
            .OrderBy(c => c.Codigo)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<CuentaContable?> GetCuentaByIdAsync(long id)
    {
        return await _context.CuentasContables
            .Include(c => c.AccountType)
            .Include(c => c.Padre)
            .Include(c => c.Hijas)
            .ThenInclude(h => h.AccountType)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<CuentaContable>> GetCuentasByTipoAsync(int accountTypeId)
    {
        return await _context.CuentasContables
            .Where(c => c.AccountTypeId == accountTypeId && c.Activa)
            .OrderBy(c => c.Codigo)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<CuentaContable>> GetArbolCuentasAsync()
    {
        var todas = await _context.CuentasContables
            .OrderBy(c => c.Codigo)
            .AsNoTracking()
            .ToListAsync();

        // Build tree in memory (only roots + their children)
        return todas.Where(c => c.PadreId == null).ToList();
    }

    public async Task<CuentaContable> CreateCuentaAsync(CuentaContable cuenta)
    {
        // Validar código único
        var existe = await _context.CuentasContables.AnyAsync(c => c.Codigo == cuenta.Codigo);
        if (existe)
            throw new InvalidOperationException($"Ya existe una cuenta con el código '{cuenta.Codigo}'.");

        // Si tiene padre, verificar que exista
        if (cuenta.PadreId.HasValue)
        {
            var padreExiste = await _context.CuentasContables.AnyAsync(c => c.Id == cuenta.PadreId.Value);
            if (!padreExiste)
                throw new ArgumentException($"La cuenta padre con ID {cuenta.PadreId} no existe.");
        }

        cuenta.FechaCreacion = DateTime.UtcNow;
        _context.CuentasContables.Add(cuenta);
        await _context.SaveChangesAsync();
        return cuenta;
    }

    public async Task<CuentaContable> UpdateCuentaAsync(CuentaContable cuenta)
    {
        var existente = await _context.CuentasContables.FindAsync(cuenta.Id);
        if (existente == null)
            throw new KeyNotFoundException($"Cuenta contable con ID {cuenta.Id} no encontrada.");

        // Validar código único (excluyéndose a sí misma)
        var conflicto = await _context.CuentasContables
            .AnyAsync(c => c.Codigo == cuenta.Codigo && c.Id != cuenta.Id);
        if (conflicto)
            throw new InvalidOperationException($"Ya existe otra cuenta con el código '{cuenta.Codigo}'.");

        // Validar que no se asigne a sí misma como padre
        if (cuenta.PadreId.HasValue && cuenta.PadreId.Value == cuenta.Id)
            throw new InvalidOperationException("Una cuenta no puede ser su propio padre.");

        existente.Codigo = cuenta.Codigo;
        existente.Nombre = cuenta.Nombre;
        existente.PadreId = cuenta.PadreId;
        existente.AccountTypeId = cuenta.AccountTypeId;
        existente.Activa = cuenta.Activa;

        _context.CuentasContables.Update(existente);
        await _context.SaveChangesAsync();
        return existente;
    }

    public async Task<bool> DeleteCuentaAsync(long id)
    {
        var cuenta = await _context.CuentasContables
            .Include(c => c.Hijas)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cuenta == null) return false;

        // No permitir eliminar cuentas con hijas
        if (cuenta.Hijas.Any())
            throw new InvalidOperationException(
                $"No se puede eliminar la cuenta '{cuenta.Nombre}' porque tiene cuentas hijas. Elimine o reasigne las hijas primero.");

        // No permitir eliminar cuentas con movimientos
        var tieneMovimientos = await _context.DetallesPartidaContable
            .AnyAsync(d => d.CuentaContableId == id);
        if (tieneMovimientos)
            throw new InvalidOperationException(
                $"No se puede eliminar la cuenta '{cuenta.Nombre}' porque tiene movimientos contables asociados. Desactívela en su lugar.");

        _context.CuentasContables.Remove(cuenta);
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== Tipos de Cuenta (AccountType) — CRUD Dinámico ====================

    public async Task<List<AccountType>> GetAllAccountTypesAsync()
    {
        return await _context.AccountTypes
            .OrderBy(at => at.Orden)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<AccountType?> GetAccountTypeByIdAsync(int id)
    {
        return await _context.AccountTypes.FindAsync(id);
    }

    public async Task<AccountType> CreateAccountTypeAsync(AccountType accountType)
    {
        var existe = await _context.AccountTypes.AnyAsync(at => at.Nombre == accountType.Nombre);
        if (existe)
            throw new InvalidOperationException($"Ya existe un tipo de cuenta con el nombre '{accountType.Nombre}'.");

        _context.AccountTypes.Add(accountType);
        await _context.SaveChangesAsync();
        return accountType;
    }

    public async Task<AccountType> UpdateAccountTypeAsync(AccountType accountType)
    {
        var existente = await _context.AccountTypes.FindAsync(accountType.Id)
            ?? throw new KeyNotFoundException($"Tipo de cuenta con ID {accountType.Id} no encontrado.");

        var conflicto = await _context.AccountTypes
            .AnyAsync(at => at.Nombre == accountType.Nombre && at.Id != accountType.Id);
        if (conflicto)
            throw new InvalidOperationException($"Ya existe otro tipo de cuenta con el nombre '{accountType.Nombre}'.");

        existente.Nombre = accountType.Nombre;
        existente.Naturaleza = accountType.Naturaleza;
        existente.CategoriaReporte = accountType.CategoriaReporte;
        existente.Orden = accountType.Orden;
        existente.Activo = accountType.Activo;

        await _context.SaveChangesAsync();
        return existente;
    }

    public async Task<bool> DeleteAccountTypeAsync(int id)
    {
        var tipo = await _context.AccountTypes
            .Include(at => at.Cuentas)
            .FirstOrDefaultAsync(at => at.Id == id);

        if (tipo == null) return false;

        if (tipo.Cuentas.Any())
            throw new InvalidOperationException(
                $"No se puede eliminar el tipo '{tipo.Nombre}' porque tiene {tipo.Cuentas.Count} cuentas asociadas. " +
                "Desactive el tipo o reasigne las cuentas a otro tipo primero.");

        _context.AccountTypes.Remove(tipo);
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== Partidas Contables (Asientos) ====================

    public async Task<PartidaContable> CreatePartidaAsync(
        PartidaContable partida, List<DetallePartidaContable> detalles)
    {
        if (detalles == null || detalles.Count < 2)
            throw new InvalidOperationException("Una partida contable debe tener al menos 2 líneas de detalle (débito y crédito).");

        // Validar que cada línea tenga solo débito o crédito (no ambos)
        foreach (var d in detalles)
        {
            if (d.Debito < 0 || d.Credito < 0)
                throw new InvalidOperationException("Los montos de débito y crédito no pueden ser negativos.");
            if (d.Debito > 0 && d.Credito > 0)
                throw new InvalidOperationException("Una línea de detalle no puede tener débito y crédito simultáneamente.");
            if (d.Debito == 0 && d.Credito == 0)
                throw new InvalidOperationException("Cada línea de detalle debe tener un monto mayor a cero.");
        }

        // Validar que la suma de débitos = suma de créditos
        var totalDebito = detalles.Sum(d => d.Debito);
        var totalCredito = detalles.Sum(d => d.Credito);
        if (Math.Abs(totalDebito - totalCredito) > 0.01m)
            throw new InvalidOperationException(
                $"La partida no está balanceada. Débitos: {totalDebito:N2}, Créditos: {totalCredito:N2}. " +
                "La suma de débitos debe ser igual a la suma de créditos.");

        // Validar que todas las cuentas existan y estén activas
        var cuentaIds = detalles.Select(d => d.CuentaContableId).Distinct().ToList();
        var cuentasValidas = await _context.CuentasContables
            .Where(c => cuentaIds.Contains(c.Id))
            .ToListAsync();
        var cuentasInvalidas = cuentaIds.Except(cuentasValidas.Select(c => c.Id)).ToList();
        if (cuentasInvalidas.Any())
            throw new ArgumentException($"Las siguientes cuentas no existen: {string.Join(", ", cuentasInvalidas)}");

        var cuentasInactivas = cuentasValidas.Where(c => !c.Activa).ToList();
        if (cuentasInactivas.Any())
            throw new InvalidOperationException(
                $"Las siguientes cuentas están inactivas: {string.Join(", ", cuentasInactivas.Select(c => $"{c.Codigo} - {c.Nombre}"))}");

        // Validar que el período no esté cerrado
        if (partida.PeriodoContableId.HasValue)
        {
            var periodo = await _context.PeriodosContables.FindAsync(partida.PeriodoContableId.Value);
            if (periodo == null)
                throw new ArgumentException($"El período contable con ID {partida.PeriodoContableId} no existe.");
            if (periodo.Cerrado)
                throw new InvalidOperationException(
                    $"No se pueden registrar partidas en un período cerrado ({periodo.Mes}/{periodo.Anio}).");
        }

        partida.FechaCreacion = DateTime.UtcNow;
        partida.Cerrada = false;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.PartidasContables.Add(partida);
            await _context.SaveChangesAsync();

            foreach (var detalle in detalles)
            {
                detalle.PartidaContableId = partida.Id;
                _context.DetallesPartidaContable.Add(detalle);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return partida;
    }

    public async Task<PartidaContable?> GetPartidaByIdAsync(long id)
    {
        return await _context.PartidasContables
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Cuenta)
            .Include(p => p.Periodo)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<PartidaContable>> GetPartidasByPeriodoAsync(long periodoId)
    {
        return await _context.PartidasContables
            .Where(p => p.PeriodoContableId == periodoId)
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Cuenta)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    // ==================== Reportes Financieros ====================

    public async Task<BalanceGeneralResult> GetBalanceGeneralAsync(DateTime fechaCorte)
    {
        // 1. Cargar todos los tipos de cuenta que pertenecen al Balance General
        var tiposBalance = await _context.AccountTypes
            .Where(at => at.Activo && at.CategoriaReporte == CategoriaReporte.Balance)
            .OrderBy(at => at.Orden)
            .AsNoTracking()
            .ToListAsync();

        var tipoIds = tiposBalance.Select(t => t.Id).ToHashSet();

        // 2. Obtener saldos acumulados por cuenta hasta la fecha de corte
        var saldos = await _context.DetallesPartidaContable
            .Where(d => d.Partida.Fecha.Date <= fechaCorte.Date
                        && tipoIds.Contains(d.Cuenta.AccountTypeId))
            .GroupBy(d => new { d.CuentaContableId, d.Cuenta.Codigo, d.Cuenta.Nombre, d.Cuenta.AccountTypeId })
            .Select(g => new
            {
                g.Key.CuentaContableId,
                g.Key.Codigo,
                g.Key.Nombre,
                g.Key.AccountTypeId,
                SaldoDebito = g.Sum(d => d.Debito),
                SaldoCredito = g.Sum(d => d.Credito)
            })
            .ToListAsync();

        // 3. Obtener cuentas sin movimiento (saldo cero) para tipos Balance
        var idsConMovimiento = saldos.Select(s => s.CuentaContableId).ToHashSet();
        var cuentasSinMovimiento = await _context.CuentasContables
            .Where(c => c.Activa && tipoIds.Contains(c.AccountTypeId) && !idsConMovimiento.Contains(c.Id))
            .Select(c => new { CuentaContableId = c.Id, c.Codigo, c.Nombre, c.AccountTypeId })
            .ToListAsync();

        var resultado = new BalanceGeneralResult { FechaCorte = fechaCorte };

        // 4. Agrupar dinámicamente por AccountType
        foreach (var tipo in tiposBalance)
        {
            var seccion = new SeccionReporte
            {
                Nombre = tipo.Nombre,
                Naturaleza = tipo.Naturaleza,
                Orden = tipo.Orden
            };

            var cuentasTipo = saldos.Where(s => s.AccountTypeId == tipo.Id).ToList();
            foreach (var csm in cuentasSinMovimiento.Where(c => c.AccountTypeId == tipo.Id))
            {
                cuentasTipo.Add(new { csm.CuentaContableId, csm.Codigo, csm.Nombre, csm.AccountTypeId, SaldoDebito = 0m, SaldoCredito = 0m });
            }

            foreach (var cuenta in cuentasTipo.OrderBy(c => c.Codigo))
            {
                var saldoNeto = cuenta.SaldoDebito - cuenta.SaldoCredito;
                if (Math.Abs(saldoNeto) < 0.001m && saldoNeto == 0) continue;

                var item = new CuentaSaldo
                {
                    CuentaId = cuenta.CuentaContableId,
                    Codigo = cuenta.Codigo,
                    Nombre = cuenta.Nombre,
                    Saldo = tipo.Naturaleza == NaturalezaCuenta.Acreedora ? -saldoNeto : saldoNeto
                };
                seccion.Cuentas.Add(item);
                seccion.Total += item.Saldo;
            }

            resultado.Secciones.Add(seccion);
        }

        return resultado;
    }

    public async Task<EstadoResultadosResult> GetEstadoResultadosAsync(int mes, int anio)
    {
        var fechaInicio = new DateTime(anio, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

        // 1. Cargar tipos de cuenta que pertenecen al Estado de Resultados
        var tiposResultado = await _context.AccountTypes
            .Where(at => at.Activo && at.CategoriaReporte == CategoriaReporte.Resultado)
            .OrderBy(at => at.Orden)
            .AsNoTracking()
            .ToListAsync();

        var tipoIds = tiposResultado.Select(t => t.Id).ToHashSet();

        // 2. Obtener saldos del período para cuentas de tipo Resultado
        var saldos = await _context.DetallesPartidaContable
            .Where(d => d.Partida.Fecha.Date >= fechaInicio.Date
                        && d.Partida.Fecha.Date <= fechaFin.Date
                        && tipoIds.Contains(d.Cuenta.AccountTypeId))
            .GroupBy(d => new { d.CuentaContableId, d.Cuenta.Codigo, d.Cuenta.Nombre, d.Cuenta.AccountTypeId })
            .Select(g => new
            {
                g.Key.CuentaContableId,
                g.Key.Codigo,
                g.Key.Nombre,
                g.Key.AccountTypeId,
                SaldoDebito = g.Sum(d => d.Debito),
                SaldoCredito = g.Sum(d => d.Credito)
            })
            .ToListAsync();

        var resultado = new EstadoResultadosResult { Mes = mes, Anio = anio };

        // 3. Agrupar dinámicamente por AccountType
        foreach (var tipo in tiposResultado)
        {
            var seccion = new SeccionReporte
            {
                Nombre = tipo.Nombre,
                Naturaleza = tipo.Naturaleza,
                Orden = tipo.Orden
            };

            var cuentasTipo = saldos.Where(s => s.AccountTypeId == tipo.Id).ToList();

            foreach (var cuenta in cuentasTipo.OrderBy(c => c.Codigo))
            {
                var saldoNeto = cuenta.SaldoDebito - cuenta.SaldoCredito;
                if (Math.Abs(saldoNeto) < 0.001m) continue;

                var item = new CuentaSaldo
                {
                    CuentaId = cuenta.CuentaContableId,
                    Codigo = cuenta.Codigo,
                    Nombre = cuenta.Nombre,
                    Saldo = tipo.Naturaleza == NaturalezaCuenta.Acreedora ? -saldoNeto : saldoNeto
                };
                seccion.Cuentas.Add(item);
                seccion.Total += item.Saldo;
            }

            if (seccion.Cuentas.Any())
                resultado.Secciones.Add(seccion);
        }

        return resultado;
    }

    // ==================== Períodos Contables ====================

    public async Task<List<PeriodoContable>> GetAllPeriodosAsync()
    {
        return await _context.PeriodosContables
            .OrderByDescending(p => p.Anio)
            .ThenByDescending(p => p.Mes)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PeriodoContable> GetOrCreatePeriodoAsync(int mes, int anio)
    {
        var periodo = await _context.PeriodosContables
            .FirstOrDefaultAsync(p => p.Mes == mes && p.Anio == anio);

        if (periodo != null)
            return periodo;

        var fechaInicio = new DateTime(anio, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

        // Calcular saldo inicial del período anterior
        decimal saldoInicial = 0;
        var periodoAnterior = await _context.PeriodosContables
            .Where(p => (p.Anio < anio) || (p.Anio == anio && p.Mes < mes))
            .OrderByDescending(p => p.Anio)
            .ThenByDescending(p => p.Mes)
            .FirstOrDefaultAsync();

        if (periodoAnterior != null && periodoAnterior.Cerrado)
        {
            // El saldo inicial es el último disponible del período anterior
            saldoInicial = periodoAnterior.SaldoInicial;

            // Sumar todos los débitos/créditos del período anterior para calcular el saldo final
            var partidasAnteriores = await _context.DetallesPartidaContable
                .Where(d => d.Partida.PeriodoContableId == periodoAnterior.Id)
                .SumAsync(d => d.Debito - d.Credito);

            saldoInicial += partidasAnteriores;
        }

        periodo = new PeriodoContable
        {
            Mes = mes,
            Anio = anio,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            SaldoInicial = saldoInicial,
            FechaCreacion = DateTime.UtcNow,
            Cerrado = false
        };

        _context.PeriodosContables.Add(periodo);
        await _context.SaveChangesAsync();
        return periodo;
    }

    public async Task<PeriodoContable> CerrarPeriodoAsync(long periodoId, string cerradoPor)
    {
        var periodo = await _context.PeriodosContables
            .Include(p => p.Partidas)
            .FirstOrDefaultAsync(p => p.Id == periodoId);

        if (periodo == null)
            throw new KeyNotFoundException($"Período contable con ID {periodoId} no encontrado.");

        if (periodo.Cerrado)
            throw new InvalidOperationException($"El período {periodo.Mes}/{periodo.Anio} ya está cerrado.");

        // Actualizar todas las partidas del período como cerradas
        foreach (var partida in periodo.Partidas)
        {
            partida.Cerrada = true;
        }

        periodo.Cerrado = true;
        periodo.FechaCierre = DateTime.UtcNow;
        periodo.CerradoPor = cerradoPor;

        await _context.SaveChangesAsync();
        return periodo;
    }

    public async Task<PeriodoContable> ReabrirPeriodoAsync(long periodoId)
    {
        var periodo = await _context.PeriodosContables
            .FirstOrDefaultAsync(p => p.Id == periodoId);

        if (periodo == null)
            throw new KeyNotFoundException($"Período contable con ID {periodoId} no encontrado.");

        if (!periodo.Cerrado)
            throw new InvalidOperationException($"El período {periodo.Mes}/{periodo.Anio} ya está abierto.");

        // Reabrir partidas del período
        var partidas = await _context.PartidasContables
            .Where(p => p.PeriodoContableId == periodoId && p.Cerrada)
            .ToListAsync();

        foreach (var partida in partidas)
        {
            partida.Cerrada = false;
        }

        periodo.Cerrado = false;
        periodo.FechaCierre = null;
        periodo.CerradoPor = null;

        await _context.SaveChangesAsync();
        return periodo;
    }

    // ==================== Integración Automática ====================

    public async Task<PartidaContable> GenerarPartidaDesdeMovimientoAsync(MovimientoGeneral movimiento)
    {
        var cuentaCaja = await GetCuentaCajaDefaultAsync();
        var periodo = await GetOrCreatePeriodoAsync(movimiento.Fecha.Month, movimiento.Fecha.Year);

        if (movimiento.Tipo == (int)TipoMovimientoGeneral.Ingreso)
        {
            // Ingreso → Débito a Caja, Crédito a la cuenta mapeada en la categoría
            if (!movimiento.CategoriaIngresoId.HasValue)
                throw new InvalidOperationException("El movimiento de ingreso no tiene una categoría de ingreso asignada.");

            var categoria = await _context.CategoriasIngreso
                .FirstOrDefaultAsync(c => c.Id == movimiento.CategoriaIngresoId.Value);
            if (categoria == null)
                throw new InvalidOperationException($"Categoría de ingreso con ID {movimiento.CategoriaIngresoId} no encontrada.");
            if (!categoria.CuentaContableId.HasValue)
                throw new InvalidOperationException(
                    $"La categoría de ingreso '{categoria.Nombre}' no tiene una cuenta contable asignada. " +
                    "Configure la cuenta contable en el catálogo de cuentas antes de generar asientos automáticos.");

            var partida = new PartidaContable
            {
                Fecha = movimiento.Fecha,
                Referencia = movimiento.NumeroComprobante ?? $"MOV-{movimiento.Id}",
                Descripcion = movimiento.Descripcion,
                PeriodoContableId = periodo.Id,
                MovimientoGeneralId = movimiento.Id
            };

            var detalles = new List<DetallePartidaContable>
            {
                new() { CuentaContableId = cuentaCaja.Id, Debito = movimiento.Monto, Credito = 0 },
                new() { CuentaContableId = categoria.CuentaContableId.Value, Debito = 0, Credito = movimiento.Monto }
            };

            return await CreatePartidaAsync(partida, detalles);
        }
        else
        {
            // Egreso → Débito a la cuenta de la categoría, Crédito a Caja
            if (!movimiento.CategoriaEgresoId.HasValue)
                throw new InvalidOperationException("El movimiento de egreso no tiene una categoría de egreso asignada.");

            var categoria = await _context.CategoriasEgreso
                .FirstOrDefaultAsync(c => c.Id == movimiento.CategoriaEgresoId.Value);
            if (categoria == null)
                throw new InvalidOperationException($"Categoría de egreso con ID {movimiento.CategoriaEgresoId} no encontrada.");
            if (!categoria.CuentaContableId.HasValue)
                throw new InvalidOperationException(
                    $"La categoría de egreso '{categoria.Nombre}' no tiene una cuenta contable asignada. " +
                    "Configure la cuenta contable en el catálogo de cuentas antes de generar asientos automáticos.");

            var partida = new PartidaContable
            {
                Fecha = movimiento.Fecha,
                Referencia = movimiento.NumeroComprobante ?? $"MOV-{movimiento.Id}",
                Descripcion = movimiento.Descripcion,
                PeriodoContableId = periodo.Id,
                MovimientoGeneralId = movimiento.Id
            };

            var detalles = new List<DetallePartidaContable>
            {
                new() { CuentaContableId = categoria.CuentaContableId.Value, Debito = movimiento.Monto, Credito = 0 },
                new() { CuentaContableId = cuentaCaja.Id, Debito = 0, Credito = movimiento.Monto }
            };

            return await CreatePartidaAsync(partida, detalles);
        }
    }

    public async Task<PartidaContable> GenerarPartidaDesdeRegistroAsync(ContabilidadRegistro registro)
    {
        var cuentaCaja = await GetCuentaCajaDefaultAsync();
        var periodo = await GetOrCreatePeriodoAsync(registro.Fecha.Month, registro.Fecha.Year);

        // Buscar cuentas genéricas según el tipo de movimiento
        var esIngreso = registro.Tipo == TipoMovimientoContable.Ingreso;
        var codigoCuentaGenerica = esIngreso ? "4.1.99" : "5.1.99";

        var cuentaGenerica = await _context.CuentasContables
            .FirstOrDefaultAsync(c => c.Codigo == codigoCuentaGenerica && c.Activa);

        if (cuentaGenerica == null)
            throw new InvalidOperationException(
                $"No se encontró la cuenta genérica '{codigoCuentaGenerica}'. " +
                "Asegúrese de que el catálogo de cuentas incluya las cuentas para migración de datos legacy.");

        var partida = new PartidaContable
        {
            Fecha = registro.Fecha,
            Referencia = $"LEGACY-{registro.Id}",
            Descripcion = registro.Descripcion,
            PeriodoContableId = periodo.Id,
            ContabilidadRegistroId = registro.Id
        };

        List<DetallePartidaContable> detalles;

        if (esIngreso)
        {
            // Ingreso → Débito Caja, Crédito cuenta genérica de ingresos
            detalles = new List<DetallePartidaContable>
            {
                new() { CuentaContableId = cuentaCaja.Id, Debito = registro.Monto, Credito = 0 },
                new() { CuentaContableId = cuentaGenerica.Id, Debito = 0, Credito = registro.Monto }
            };
        }
        else
        {
            // Egreso → Débito cuenta genérica de gastos, Crédito Caja
            detalles = new List<DetallePartidaContable>
            {
                new() { CuentaContableId = cuentaGenerica.Id, Debito = registro.Monto, Credito = 0 },
                new() { CuentaContableId = cuentaCaja.Id, Debito = 0, Credito = registro.Monto }
            };
        }

        return await CreatePartidaAsync(partida, detalles);
    }

    // ==================== Helpers ====================

    public async Task<CuentaContable> GetCuentaCajaDefaultAsync()
    {
        // Buscar la cuenta configurada como "Caja/Banco Default"
        var config = await _context.Configuraciones
            .FirstOrDefaultAsync(c => c.Clave == "CUENTA_CAJA_DEFAULT_ID");

        if (config != null && long.TryParse(config.Valor, out var cuentaId))
        {
            var cuenta = await _context.CuentasContables.FindAsync(cuentaId);
            if (cuenta != null && cuenta.Activa)
                return cuenta;
        }

        // Fallback: buscar la primera cuenta de un tipo con naturaleza Deudora (Activo) y CategoriaReporte Balance
        var tiposActivo = await _context.AccountTypes
            .Where(at => at.Activo && at.Naturaleza == NaturalezaCuenta.Deudora && at.CategoriaReporte == CategoriaReporte.Balance)
            .OrderBy(at => at.Orden)
            .Select(at => at.Id)
            .FirstOrDefaultAsync();

        var cajaDefault = await _context.CuentasContables
            .Where(c => c.AccountTypeId == tiposActivo && c.Activa)
            .OrderBy(c => c.Codigo)
            .FirstOrDefaultAsync();

        if (cajaDefault == null)
            throw new InvalidOperationException(
                "No hay una cuenta de caja/activo configurada. " +
                "Configure al menos un tipo de cuenta con naturaleza Deudora y categoría Balance en AccountTypes, " +
                "y registre la clave 'CUENTA_CAJA_DEFAULT_ID' en ConfiguracionSistema con el ID de la cuenta.");

        return cajaDefault;
    }
}
