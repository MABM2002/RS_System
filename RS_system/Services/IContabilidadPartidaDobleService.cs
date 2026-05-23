using Rs_system.Models;

namespace Rs_system.Services;

/// <summary>
/// Servicio de Contabilidad por Partida Doble.
/// Gestiona catálogo de cuentas, partidas contables, reportes financieros y cierre de períodos.
/// </summary>
public interface IContabilidadPartidaDobleService
{
    // ==================== Catálogo de Cuentas ====================

    /// <summary>Obtiene todas las cuentas contables activas.</summary>
    Task<List<CuentaContable>> GetAllCuentasAsync();

    /// <summary>Obtiene una cuenta por su ID.</summary>
    Task<CuentaContable?> GetCuentaByIdAsync(long id);

    /// <summary>Obtiene cuentas filtradas por tipo de cuenta (AccountType).</summary>
    Task<List<CuentaContable>> GetCuentasByTipoAsync(int accountTypeId);

    /// <summary>Obtiene el árbol jerárquico completo del catálogo de cuentas.</summary>
    Task<List<CuentaContable>> GetArbolCuentasAsync();

    /// <summary>Crea una nueva cuenta contable.</summary>
    Task<CuentaContable> CreateCuentaAsync(CuentaContable cuenta);

    /// <summary>Actualiza una cuenta contable existente.</summary>
    Task<CuentaContable> UpdateCuentaAsync(CuentaContable cuenta);

    /// <summary>Elimina una cuenta (solo si no tiene hijas ni movimientos asociados).</summary>
    Task<bool> DeleteCuentaAsync(long id);

    // ==================== Tipos de Cuenta (AccountType) — CRUD dinámico ====================

    /// <summary>Obtiene todos los tipos de cuenta activos.</summary>
    Task<List<AccountType>> GetAllAccountTypesAsync();

    /// <summary>Obtiene un tipo de cuenta por su ID.</summary>
    Task<AccountType?> GetAccountTypeByIdAsync(int id);

    /// <summary>Crea un nuevo tipo de cuenta.</summary>
    Task<AccountType> CreateAccountTypeAsync(AccountType accountType);

    /// <summary>Actualiza un tipo de cuenta existente.</summary>
    Task<AccountType> UpdateAccountTypeAsync(AccountType accountType);

    /// <summary>Elimina un tipo de cuenta (solo si no tiene cuentas asociadas).</summary>
    Task<bool> DeleteAccountTypeAsync(int id);

    // ==================== Partidas Contables (Asientos) ====================

    /// <summary>
    /// Crea una partida contable con sus líneas de detalle.
    /// Valida que la suma de débitos sea igual a la suma de créditos.
    /// Verifica que el período no esté cerrado.
    /// </summary>
    Task<PartidaContable> CreatePartidaAsync(PartidaContable partida, List<DetallePartidaContable> detalles);

    /// <summary>Obtiene una partida contable con todos sus detalles y cuentas.</summary>
    Task<PartidaContable?> GetPartidaByIdAsync(long id);

    /// <summary>Lista todas las partidas contables de un período específico.</summary>
    Task<List<PartidaContable>> GetPartidasByPeriodoAsync(long periodoId);

    // ==================== Reportes Financieros ====================

    /// <summary>
    /// Calcula el Balance General a una fecha de corte.
    /// Agrupa los saldos acumulados (Débito - Crédito) por cuenta y por tipo de cuenta.
    /// </summary>
    Task<BalanceGeneralResult> GetBalanceGeneralAsync(DateTime fechaCorte);

    /// <summary>
    /// Calcula el Estado de Resultados para un mes y año específicos.
    /// Retorna ingresos, gastos y resultado neto del período.
    /// </summary>
    Task<EstadoResultadosResult> GetEstadoResultadosAsync(int mes, int anio);

    // ==================== Períodos Contables ====================

    /// <summary>Obtiene todos los períodos contables ordenados por año y mes descendente.</summary>
    Task<List<PeriodoContable>> GetAllPeriodosAsync();

    /// <summary>Obtiene o crea un período contable para el mes y año dados.</summary>
    Task<PeriodoContable> GetOrCreatePeriodoAsync(int mes, int anio);

    /// <summary>
    /// Cierra un período contable. Bloquea todas las partidas del período.
    /// Calcula el saldo inicial para el período siguiente.
    /// </summary>
    Task<PeriodoContable> CerrarPeriodoAsync(long periodoId, string cerradoPor);

    /// <summary>Reabre un período contable previamente cerrado (solo admin).</summary>
    Task<PeriodoContable> ReabrirPeriodoAsync(long periodoId);

    // ==================== Integración Automática ====================

    /// <summary>
    /// Genera automáticamente una partida contable (doble entrada) a partir de un MovimientoGeneral.
    /// Ingreso → Débito Caja, Crédito cuenta de la categoría.
    /// Egreso  → Débito cuenta de la categoría, Crédito Caja.
    /// </summary>
    Task<PartidaContable> GenerarPartidaDesdeMovimientoAsync(MovimientoGeneral movimiento);

    /// <summary>
    /// Genera automáticamente una partida contable (doble entrada) a partir de un ContabilidadRegistro.
    /// Usa las cuentas genéricas "4.1.99 - Otros Ingresos" / "5.1.99 - Otros Gastos" como contrapartida.
    /// </summary>
    Task<PartidaContable> GenerarPartidaDesdeRegistroAsync(ContabilidadRegistro registro);

    // ==================== Helpers ====================

    /// <summary>Obtiene la cuenta de caja/bancos configurada como contrapartida por defecto.</summary>
    Task<CuentaContable> GetCuentaCajaDefaultAsync();
}

/// <summary>Resultado del Balance General — 100% dinámico basado en AccountType.</summary>
public class BalanceGeneralResult
{
    public DateTime FechaCorte { get; set; }
    public List<SeccionReporte> Secciones { get; set; } = new();

    /// <summary>Verifica la ecuación contable: Σ Deudoras = Σ Acreedoras.</summary>
    public bool EstaBalanceado =>
        Math.Abs(Secciones.Where(s => s.Naturaleza == NaturalezaCuenta.Deudora).Sum(s => s.Total)
               - Secciones.Where(s => s.Naturaleza == NaturalezaCuenta.Acreedora).Sum(s => s.Total)) < 0.01m;

    public decimal TotalDeudoras => Secciones.Where(s => s.Naturaleza == NaturalezaCuenta.Deudora).Sum(s => s.Total);
    public decimal TotalAcreedoras => Secciones.Where(s => s.Naturaleza == NaturalezaCuenta.Acreedora).Sum(s => s.Total);
}

/// <summary>Resultado del Estado de Resultados — 100% dinámico basado en AccountType.</summary>
public class EstadoResultadosResult
{
    public int Mes { get; set; }
    public int Anio { get; set; }
    public List<SeccionReporte> Secciones { get; set; } = new();
    public decimal ResultadoNeto => Secciones.Where(s => s.Naturaleza == NaturalezaCuenta.Acreedora).Sum(s => s.Total)
                                 - Secciones.Where(s => s.Naturaleza == NaturalezaCuenta.Deudora).Sum(s => s.Total);
    public bool EsUtilidad => ResultadoNeto >= 0;
}

/// <summary>Sección dinámica de un reporte financiero (Agrupa cuentas por tipo).</summary>
public class SeccionReporte
{
    public string Nombre { get; set; } = string.Empty;
    public NaturalezaCuenta Naturaleza { get; set; }
    public List<CuentaSaldo> Cuentas { get; set; } = new();
    public decimal Total { get; set; }
    public int Orden { get; set; }
}

/// <summary>Saldo de una cuenta para reportes.</summary>
public class CuentaSaldo
{
    public long CuentaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
}
